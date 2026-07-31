using System.Text.Json.Nodes;
using AutoHub.SharedKernel.Domain;
using AutoHub.SharedKernel.Ids;
using AutoHub.SharedKernel.Results;

namespace AutoHub.Platform.Integrations;

/// <summary>
/// One vendor configured to serve one capability.
/// </summary>
/// <remarks>
/// A provider row is everything needed to talk to a vendor <b>except the credentials</b> — see
/// <see cref="CredentialRef"/>, where that separation is argued.
/// </remarks>
public sealed class IntegrationProvider : AggregateRoot, IAuditable
{
    private IntegrationProvider(
        Guid id,
        string code,
        string capabilityCode,
        string displayName,
        IntegrationEnvironment environment,
        string credentialRef)
        : base(id)
    {
        Code = code;
        CapabilityCode = capabilityCode;
        DisplayName = displayName;
        Environment = environment;
        CredentialRef = credentialRef;
    }

    private IntegrationProvider() { }

    /// <summary>Vendor code, e.g. <c>razorpay</c>, <c>cashfree</c>, <c>karza</c>, <c>msg91</c>.</summary>
    public string Code { get; private set; } = string.Empty;

    public string CapabilityCode { get; private set; } = string.Empty;

    public string DisplayName { get; private set; } = string.Empty;

    /// <summary>
    /// Sandbox and live are separate rows, never a flag flipped in place.
    /// </summary>
    /// <remarks>
    /// Two rows means the sandbox configuration keeps existing after go-live, so a support engineer
    /// reproducing a failed payment does it against sandbox without editing the row production is
    /// reading. One row with a toggle is how a test key reaches production at 4pm on a Friday.
    /// </remarks>
    public IntegrationEnvironment Environment { get; private set; }

    /// <summary>
    /// Non-secret settings: base URL, merchant id, webhook path, supported payment methods, feature
    /// toggles. Anything here is visible to anyone who can read the admin console.
    /// </summary>
    public string? ConfigJson { get; private set; }

    /// <summary>
    /// A <b>pointer</b> to the credential in a secret store. Never the credential.
    /// </summary>
    /// <remarks>
    /// <b>Why the database must not hold the secret.</b> The rule is not "secrets are sensitive" —
    /// everyone agrees with that and stores them anyway. It is that a database row has a much larger
    /// blast radius than it appears to:
    /// <list type="bullet">
    /// <item>Nightly backups copy it, and backups live longer and are guarded less than the database.</item>
    /// <item>Read replicas and point-in-time snapshots copy it again, to more machines.</item>
    /// <item>Anyone with the admin console's provider-edit screen — a support lead, an ops contractor
    /// — can read a live payment key, and revoking their access does not un-read it.</item>
    /// <item>A single SQL-injection or an over-broad analytics query exports every key at once, and
    /// the audit trail shows a legitimate SELECT.</item>
    /// <item>Rotating a leaked key means editing rows in every environment rather than one entry in
    /// one vault, so rotation gets deferred, and deferred rotation is how a leak becomes an incident.</item>
    /// </list>
    /// Holding a reference instead — <c>vault://autohub/payments/razorpay/live</c> — means the
    /// database, its backups, its replicas and its admin console all contain a string that is worth
    /// nothing on its own. The secret is fetched at call time by
    /// <see cref="ISecretResolver"/> from a store whose access is separately granted, separately
    /// audited, and separately rotatable. It also keeps card-adjacent material out of the database's
    /// compliance scope entirely, which is far cheaper than proving the database is safe enough.
    /// </remarks>
    public string CredentialRef { get; private set; } = string.Empty;

    public bool IsActive { get; private set; } = true;

    /// <summary>
    /// Per-call deadline. Set per provider because vendors differ by an order of magnitude — a UPI
    /// collect request is sub-second, a bureau score can take fifteen.
    /// </summary>
    public int TimeoutSeconds { get; private set; } = 30;

    /// <summary>
    /// Attempts against <i>this</i> provider before the router moves to the next one.
    /// </summary>
    /// <remarks>
    /// Defaults to one. Retrying the same dead gateway is what the circuit breaker exists to stop,
    /// and for anything that moves money a blind retry risks a double charge — so a value above one
    /// is a deliberate statement that this provider's failures are known to be safe to repeat.
    /// </remarks>
    public int MaxAttempts { get; private set; } = 1;

    public DateTimeOffset CreatedAt { get; set; }

    public Guid? CreatedBy { get; set; }

    public DateTimeOffset? UpdatedAt { get; set; }

    public Guid? UpdatedBy { get; set; }

    public static Result<IntegrationProvider> Create(
        string code,
        string capabilityCode,
        string displayName,
        IntegrationEnvironment environment,
        string credentialRef)
    {
        if (LooksLikeASecret(credentialRef))
        {
            // Refusing at the door rather than trusting a naming convention. The one thing this
            // column must never contain is the thing it is most tempting to paste into it.
            return Error.Validation(
                "integration.credential_ref_looks_like_secret",
                "CredentialRef must reference a secret store entry, not contain the secret itself.");
        }

        return new IntegrationProvider(UuidV7.New(), code, capabilityCode, displayName, environment, credentialRef);
    }

    public void Configure(JsonNode? config) => ConfigJson = config?.ToJsonString();

    public void SetLimits(int timeoutSeconds, int maxAttempts)
    {
        TimeoutSeconds = timeoutSeconds;
        MaxAttempts = maxAttempts;
    }

    public void Deactivate() => IsActive = false;

    public void Activate() => IsActive = true;

    public JsonNode? Config()
        => string.IsNullOrWhiteSpace(ConfigJson) ? null : JsonNode.Parse(ConfigJson);

    /// <summary>
    /// Catches the obvious paste. A reference is short, scheme-prefixed, and has no entropy;
    /// gateway keys are long and random.
    /// </summary>
    private static bool LooksLikeASecret(string credentialRef)
        => string.IsNullOrWhiteSpace(credentialRef)
           || credentialRef.Length > 128
           || !credentialRef.Contains(':', StringComparison.Ordinal);
}

public enum IntegrationEnvironment
{
    Sandbox = 1,
    Live = 2,
}
