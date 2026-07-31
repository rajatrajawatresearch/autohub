using AutoHub.SharedKernel.Results;
using AutoHub.SharedKernel.Time;
using Microsoft.Extensions.Logging;

namespace AutoHub.Platform.Integrations;

/// <summary>
/// Walks the resolved chain, calling adapters until one answers or failing over is no longer safe.
/// </summary>
public sealed class IntegrationInvoker : IIntegrationInvoker
{
    private readonly IIntegrationRouter _router;
    private readonly IReadOnlyList<IIntegrationAdapter> _adapters;
    private readonly IClock _clock;
    private readonly ILogger<IntegrationInvoker> _logger;

    public IntegrationInvoker(
        IIntegrationRouter router,
        IEnumerable<IIntegrationAdapter> adapters,
        IClock clock,
        ILogger<IntegrationInvoker> logger)
    {
        _router = router;
        _adapters = adapters.ToList();
        _clock = clock;
        _logger = logger;
    }

    public async Task<Result<TResponse>> InvokeAsync<TRequest, TResponse>(
        string capabilityCode,
        TRequest request,
        IntegrationContext context,
        string idempotencyKey,
        CancellationToken cancellationToken)
    {
        var chain = await _router.ResolveAsync(capabilityCode, context, cancellationToken);
        if (chain.IsFailure)
        {
            return Result.Failure<TResponse>(chain.Error);
        }

        Error? lastError = null;

        foreach (var provider in chain.Value)
        {
            var adapter = FindAdapter<TRequest, TResponse>(capabilityCode, provider.ProviderCode);

            if (adapter is null)
            {
                // Configured but not deployed: someone added a provider row for a vendor whose
                // adapter is not in this build. Skipping keeps the fallback working, and the log is
                // what tells an operator why their new provider is receiving no traffic.
                _logger.LogError(
                    "No {Capability} adapter is registered for provider {Provider}",
                    capabilityCode, provider.ProviderCode);

                continue;
            }

            for (var attempt = 0; attempt < Math.Max(provider.MaxAttempts, 1); attempt++)
            {
                var outcome = await ExecuteAsync(
                    adapter, request, provider, idempotencyKey, attempt, cancellationToken);

                if (outcome.IsSuccess)
                {
                    await _router.RecordOutcomeAsync(
                        provider.ProviderCode, capabilityCode, succeeded: true, cancellationToken);

                    if (provider.Order > 0)
                    {
                        _logger.LogWarning(
                            "{Capability} served by fallback {Provider} at position {Order}",
                            capabilityCode, provider.ProviderCode, provider.Order);
                    }

                    return outcome;
                }

                lastError = outcome.Error;

                await _router.RecordOutcomeAsync(
                    provider.ProviderCode, capabilityCode, succeeded: false, cancellationToken);

                if (IntegrationErrors.IsIndeterminate(outcome.Error))
                {
                    // The vendor may already have acted. Trying anyone else — including this same
                    // provider again — risks doing the thing twice, and for payment.collect that is
                    // a customer's money.
                    _logger.LogError(
                        "{Capability} on {Provider} returned an indeterminate result ({Code}); chain stopped for reconciliation",
                        capabilityCode, provider.ProviderCode, outcome.Error.Code);

                    return Result.Failure<TResponse>(outcome.Error);
                }

                if (outcome.Error.Kind is not ErrorKind.Upstream)
                {
                    // The vendor rejected our request rather than failing to serve it — a malformed
                    // amount, an unsupported method, a validation error. Every other provider will
                    // reject it identically, so failing over just multiplies the latency.
                    return Result.Failure<TResponse>(outcome.Error);
                }
            }
        }

        return Result.Failure<TResponse>(lastError ?? Error.Upstream(
            "integration.all_providers_failed",
            $"No provider could serve '{capabilityCode}'.")
            .With("capability", capabilityCode));
    }

    /// <summary>
    /// Applies the provider's timeout and converts anything thrown into a typed failure.
    /// </summary>
    /// <remarks>
    /// A timeout is deliberately reported as <b>indeterminate</b>. We sent the request and never
    /// heard back, so the only honest statement is that we do not know whether it took effect —
    /// which is precisely the case where failing over would double-charge.
    /// </remarks>
    private async Task<Result<TResponse>> ExecuteAsync<TRequest, TResponse>(
        IIntegrationAdapter<TRequest, TResponse> adapter,
        TRequest request,
        ResolvedProvider provider,
        string idempotencyKey,
        int attempt,
        CancellationToken cancellationToken)
    {
        using var timeout = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        timeout.CancelAfter(provider.Timeout);

        var invocation = new IntegrationInvocation
        {
            Provider = provider,
            IdempotencyKey = idempotencyKey,
            RequestedAt = _clock.UtcNow,
            Attempt = attempt,
        };

        try
        {
            return await adapter.ExecuteAsync(request, invocation, timeout.Token);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            // The caller hung up, not the vendor. Propagate rather than recording a failure the
            // provider did not cause — a wave of abandoned requests must not open every circuit.
            throw;
        }
        catch (OperationCanceledException)
        {
            return IntegrationErrors.Indeterminate(
                "integration.timeout",
                $"{provider.ProviderCode} did not respond within {provider.Timeout.TotalSeconds:0}s.");
        }
        catch (HttpRequestException ex)
        {
            // A transport error before a response: the request did not complete at the vendor.
            _logger.LogWarning(
                ex, "{Capability} transport failure against {Provider}",
                provider.CapabilityCode, provider.ProviderCode);

            return IntegrationErrors.Retryable(
                "integration.transport_failed",
                $"Could not reach {provider.ProviderCode}.");
        }
    }

    private IIntegrationAdapter<TRequest, TResponse>? FindAdapter<TRequest, TResponse>(
        string capabilityCode,
        string providerCode)
        => _adapters
            .OfType<IIntegrationAdapter<TRequest, TResponse>>()
            .FirstOrDefault(a =>
                string.Equals(a.CapabilityCode, capabilityCode, StringComparison.Ordinal) &&
                string.Equals(a.ProviderCode, providerCode, StringComparison.Ordinal));
}
