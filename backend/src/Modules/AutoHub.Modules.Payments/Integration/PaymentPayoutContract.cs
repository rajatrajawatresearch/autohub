using AutoHub.SharedKernel.Money;

namespace AutoHub.Modules.Payments.Integration;

/// <summary>
/// Our request to send money out. Vendor-neutral, like its collection sibling.
/// </summary>
/// <remarks>
/// Declared here rather than in <c>AutoHub.Platform</c> because payouts are this module's business
/// and nothing else asks for one. The capability code it routes under —
/// <c>payment.payout</c> — is already in the platform's capability list, so the routing table, the
/// fallback chain and the circuit breaker all work without the platform knowing this type exists.
/// </remarks>
public sealed record PaymentPayoutRequest
{
    public required Money Amount { get; init; }

    /// <summary>Our reference — the payout instruction id. Appears on the bank's response file.</summary>
    public required string InstructionReference { get; init; }

    /// <summary>Opaque handle to the beneficiary held at the provider. Never an account number here.</summary>
    public required string BeneficiaryRef { get; init; }

    /// <summary>Taxonomy code for the rail: <c>imps</c>, <c>neft</c>, <c>upi</c>, <c>rtgs</c>.</summary>
    /// <remarks>
    /// A code rather than an enum for the reason the collection contract gives: the list of Indian
    /// payment rails is not stable, and each new one arrived after any enum anyone would have
    /// written. Null lets the provider choose by amount, which is what most of them do better than
    /// we would.
    /// </remarks>
    public string? RailCode { get; init; }

    /// <summary>What the beneficiary sees on their statement. Regulated in length by most rails.</summary>
    public string? Narration { get; init; }
}

public sealed record PaymentPayoutResponse
{
    public required string ProviderCode { get; init; }

    public required string ProviderReference { get; init; }

    public required PaymentPayoutStatus Status { get; init; }

    public required Money Amount { get; init; }

    /// <summary>The bank's own UTR once it exists. What a beneficiary quotes when chasing a transfer.</summary>
    public string? UtrNumber { get; init; }

    public string? FailureCode { get; init; }
}

/// <summary>
/// The normalised outcome of a payout attempt at the provider.
/// </summary>
/// <remarks>
/// <see cref="Accepted"/> is not <see cref="Settled"/>, and conflating them is the classic payout
/// bug. A bank accepting an instruction means it is queued; IMPS reverses, NEFT settles in half-hour
/// batches, and a beneficiary account can be frozen between the two. Only <see cref="Settled"/> may
/// close a payout instruction and only it may post the journal that says the money left.
/// </remarks>
public enum PaymentPayoutStatus
{
    Accepted = 1,

    Settled = 2,

    Failed = 3,

    /// <summary>
    /// The provider could not say. Treated as in-flight and reconciled, never retried.
    /// </summary>
    /// <remarks>
    /// The payout equivalent of an indeterminate collection failure. Retrying an unknown payout is
    /// how a dealer receives ₹5,49,000 twice, and getting it back is a conversation rather than an
    /// API call.
    /// </remarks>
    Unknown = 4,
}
