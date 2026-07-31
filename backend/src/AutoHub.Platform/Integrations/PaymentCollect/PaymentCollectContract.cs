using AutoHub.SharedKernel.Money;

namespace AutoHub.Platform.Integrations.PaymentCollect;

/// <summary>
/// Our request to take money from a customer. Vendor-neutral by construction.
/// </summary>
/// <remarks>
/// This is the worked example the whole integration registry exists to make possible. Read it
/// alongside <see cref="RazorpayPaymentCollectAdapter"/> and
/// <see cref="CashfreePaymentCollectAdapter"/>: two vendors, two wire formats, one type that the
/// deals module and the payments module both speak — and a routing table that decides between them
/// per city, per amount, per method, without either module knowing a decision was made.
/// </remarks>
public sealed record PaymentCollectRequest
{
    public required Money Amount { get; init; }

    /// <summary>Our own reference — the deal or invoice id. Appears on the vendor's settlement report.</summary>
    public required string OrderReference { get; init; }

    /// <summary>E.164 phone. India is phone-first, so this is the one contact detail always present.</summary>
    public required string CustomerContact { get; init; }

    public string? CustomerEmail { get; init; }

    /// <summary>
    /// The instrument, as a taxonomy code (<c>upi</c>, <c>card</c>, <c>netbanking</c>,
    /// <c>wallet</c>). Null lets the vendor show its own picker.
    /// </summary>
    /// <remarks>
    /// A code from the taxonomy registry rather than an enum, because the list of Indian payment
    /// instruments is not stable — UPI AutoPay, credit-line-on-UPI and RuPay credit each arrived
    /// after any enum anyone would have written.
    /// </remarks>
    public string? Method { get; init; }

    /// <summary>Where the vendor sends the customer back to. Null for server-to-server collection.</summary>
    public string? ReturnUrl { get; init; }
}

/// <summary>The vendor's answer, normalised.</summary>
public sealed record PaymentCollectResponse
{
    public required string ProviderCode { get; init; }

    /// <summary>The vendor's id for this attempt. Kept for reconciliation and for refunds.</summary>
    public required string ProviderReference { get; init; }

    public required PaymentCollectStatus Status { get; init; }

    public required Money Amount { get; init; }

    /// <summary>Where to send the customer to complete payment, if the vendor needs a page.</summary>
    public string? RedirectUrl { get; init; }

    /// <summary>A <c>upi://</c> intent for apps that can open GPay or PhonePe directly.</summary>
    public string? UpiIntentUrl { get; init; }
}

/// <summary>
/// The normalised outcome of a collection attempt at the gateway.
/// </summary>
/// <remarks>
/// This is <b>not</b> a lifecycle status and does not belong in the workflow registry. It is the
/// translation target for a dozen incompatible vendor vocabularies — Razorpay's <c>created</c>,
/// Cashfree's <c>ACTIVE</c>, another's <c>AWAITING_CUSTOMER</c> — so that the payments module has
/// one thing to reason about. What the <i>deal</i> does in response to each of these is workflow
/// registry data, and that is where the states an operator can add live.
/// </remarks>
public enum PaymentCollectStatus
{
    /// <summary>The vendor accepted the request; the customer has not acted yet.</summary>
    Created = 1,

    /// <summary>The customer has acted and the vendor is settling. Ends in success or failure later.</summary>
    Pending = 2,

    /// <summary>Money captured. The only status that may move a deal forward.</summary>
    Succeeded = 3,

    /// <summary>Definitively failed at the vendor. Safe to offer the customer another attempt.</summary>
    Failed = 4,
}
