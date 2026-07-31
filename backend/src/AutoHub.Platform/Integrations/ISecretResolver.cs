using AutoHub.SharedKernel.Results;

namespace AutoHub.Platform.Integrations;

/// <summary>
/// Turns a <see cref="IntegrationProvider.CredentialRef"/> into an actual credential, at call time.
/// </summary>
/// <remarks>
/// Deliberately asynchronous and deliberately fallible. A vault lookup is a network call that can be
/// denied, and a code path that assumed credentials were always available in memory would have no
/// honest way to express "this key was revoked twenty minutes ago".
/// <para>
/// Resolved values must not be logged, must not be put in <see cref="Error.Metadata"/>, and must not
/// be cached beyond the lifetime of the call — a cached credential outlives its own rotation, which
/// turns a routine key change into an outage nobody connects to the change.
/// </para>
/// </remarks>
public interface ISecretResolver
{
    /// <summary>
    /// Reads one field of a credential — a gateway has a key id and a key secret, and they are
    /// separate fields of one reference rather than two references to keep and rotate in step.
    /// </summary>
    Task<Result<string>> ResolveAsync(string credentialRef, string field, CancellationToken cancellationToken);
}

/// <summary>
/// Resolves credentials from environment variables.
/// </summary>
/// <remarks>
/// The working default, and genuinely correct for containerised deployments where the orchestrator
/// injects secrets from its own store — the variable is the vault's delivery mechanism, not a
/// replacement for it. Development uses a dotenv file that is not committed.
/// <para>
/// The reference <c>env:AUTOHUB_RAZORPAY_LIVE</c> with field <c>key_secret</c> reads
/// <c>AUTOHUB_RAZORPAY_LIVE__KEY_SECRET</c>. The double underscore matches the convention .NET
/// configuration already uses for nesting, so the same variable is readable by both.
/// </para>
/// </remarks>
public sealed class EnvironmentSecretResolver : ISecretResolver
{
    private const string EnvScheme = "env:";

    public Task<Result<string>> ResolveAsync(
        string credentialRef,
        string field,
        CancellationToken cancellationToken)
    {
        if (!credentialRef.StartsWith(EnvScheme, StringComparison.Ordinal))
        {
            return Task.FromResult(Result.Failure<string>(Error.Unexpected(
                "integration.credential_scheme_unsupported",
                "This deployment can only resolve env: credential references.")
                .With("scheme", credentialRef.Split(':', 2)[0])));
        }

        var variable = $"{credentialRef[EnvScheme.Length..]}__{field}".ToUpperInvariant();
        var value = System.Environment.GetEnvironmentVariable(variable);

        if (string.IsNullOrEmpty(value))
        {
            // The variable name is safe to report; its value is not, and this is exactly the kind of
            // error that gets pasted into a chat channel while someone asks for help.
            return Task.FromResult(Result.Failure<string>(Error.Unexpected(
                "integration.credential_missing",
                "The credential for this provider is not configured.")
                .With("variable", variable)));
        }

        return Task.FromResult(Result.Success(value));
    }
}
