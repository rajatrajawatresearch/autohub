using AutoHub.SharedKernel.Results;

namespace AutoHub.Platform.Integrations;

/// <summary>Non-generic face of an adapter, so the container can enumerate every one of them.</summary>
public interface IIntegrationAdapter
{
    /// <summary>Matches <see cref="IntegrationProvider.Code"/>.</summary>
    string ProviderCode { get; }

    /// <summary>Matches <see cref="IntegrationCapability.Code"/>.</summary>
    string CapabilityCode { get; }
}

/// <summary>
/// One vendor's implementation of one capability.
/// </summary>
/// <remarks>
/// The request and response types are <b>ours</b>, not the vendor's. That is the entire point: the
/// adapter's job is to translate our vocabulary into Razorpay's and Razorpay's answer back into
/// ours, so that nothing above this interface ever learns what a <c>razorpay_order_id</c> is. An
/// interface that leaked the vendor's shapes would move the coupling rather than remove it.
/// <para>
/// Adapters are stateless and are registered as singletons. Anything per-call — credentials, the
/// resolved config, the timeout, the idempotency key — arrives in
/// <see cref="IntegrationInvocation"/>, because the same adapter instance serves sandbox and live,
/// and two merchant accounts, concurrently.
/// </para>
/// </remarks>
public interface IIntegrationAdapter<in TRequest, TResponse> : IIntegrationAdapter
{
    Task<Result<TResponse>> ExecuteAsync(
        TRequest request,
        IntegrationInvocation invocation,
        CancellationToken cancellationToken);
}

/// <summary>Everything an adapter needs to know about this particular call.</summary>
public sealed record IntegrationInvocation
{
    public required ResolvedProvider Provider { get; init; }

    /// <summary>
    /// Passed through to the vendor as their idempotency header where they support one.
    /// </summary>
    /// <remarks>
    /// Ours, not generated per attempt. A retry must carry the same key as the attempt it is
    /// retrying, or the gateway treats it as a new charge — which is the failure mode idempotency
    /// keys exist to prevent, reintroduced by the code that was supposed to prevent it.
    /// </remarks>
    public required string IdempotencyKey { get; init; }

    public required DateTimeOffset RequestedAt { get; init; }

    /// <summary>Zero on the first attempt against this provider. Adapters may log it; most ignore it.</summary>
    public int Attempt { get; init; }
}

/// <summary>
/// Runs a capability against its provider chain, failing over where that is safe.
/// </summary>
/// <remarks>
/// This is the only place that knows a chain exists. Handlers ask for a capability and receive an
/// answer; whether that answer came from the primary, from a fallback, or after a circuit had
/// opened is an operational detail they neither see nor should branch on.
/// </remarks>
public interface IIntegrationInvoker
{
    Task<Result<TResponse>> InvokeAsync<TRequest, TResponse>(
        string capabilityCode,
        TRequest request,
        IntegrationContext context,
        string idempotencyKey,
        CancellationToken cancellationToken);
}

/// <summary>
/// Marks a failure whose outcome at the vendor is unknown.
/// </summary>
/// <remarks>
/// The single most important distinction in this whole registry, and the one that separates a
/// failover that helps from a failover that charges a customer twice.
/// <list type="bullet">
/// <item><b>Retryable</b> — the request provably never reached the vendor, or reached it and was
/// rejected before anything happened. Connection refused, DNS failure, a 503 from the edge, an
/// authentication error. Moving to the next provider is safe.</item>
/// <item><b>Indeterminate</b> — the request was accepted and then the answer was lost. A read
/// timeout after the POST, a connection reset mid-response, a 500 from the payment core. The charge
/// may well exist. Failing over would create a second one, so the chain <b>stops</b> and the caller
/// is told to reconcile rather than retry.</item>
/// </list>
/// A vendor that cannot tell us which of the two happened must be treated as indeterminate, because
/// the cost of guessing wrong is asymmetric: one direction costs a slow support ticket, the other
/// costs a customer's money and a chargeback.
/// </remarks>
public static class IntegrationErrors
{
    /// <summary>Metadata flag the invoker reads to decide whether the chain may continue.</summary>
    public const string IndeterminateFlag = "indeterminate";

    /// <summary>The call definitely did not take effect. Failover is safe.</summary>
    public static Error Retryable(string code, string message)
        => Error.Upstream(code, message).With(IndeterminateFlag, false);

    /// <summary>The call may or may not have taken effect. Failover is not safe.</summary>
    public static Error Indeterminate(string code, string message)
        => Error.Upstream(code, message).With(IndeterminateFlag, true);

    public static bool IsIndeterminate(Error error)
        => error.Metadata.TryGetValue(IndeterminateFlag, out var flag) && flag is true;
}
