using AutoHub.SharedKernel.Domain;
using AutoHub.SharedKernel.Ids;

namespace AutoHub.Platform.Integrations;

/// <summary>
/// A thing the platform needs done by somebody outside it — stated once, independent of who does it.
/// </summary>
/// <remarks>
/// The capability is the seam. Application code asks for <c>payment.collect</c> and never learns
/// whether Razorpay, Cashfree or a bank's own gateway answered; the routing registry decides, per
/// city, per amount, per segment, and changes its mind without a deployment.
/// <para>
/// This is what makes "swap Razorpay for Cashfree" a config change rather than a project. Written
/// the usual way — an <c>IRazorpayClient</c> injected into a handler — swapping providers means
/// touching every call site, re-testing every flow, and doing it all at once during a maintenance
/// window, because there is no way to run both.
/// </para>
/// </remarks>
public sealed class IntegrationCapability : AggregateRoot, IAuditable
{
    private IntegrationCapability(Guid id, string code, string name) : base(id)
    {
        Code = code;
        Name = name;
    }

    private IntegrationCapability() { }

    /// <summary>Dotted <c>domain.verb</c>, e.g. <c>payment.collect</c>, <c>kyc.pan_verify</c>.</summary>
    public string Code { get; private set; } = string.Empty;

    public string Name { get; private set; } = string.Empty;

    public string? Description { get; private set; }

    /// <summary>
    /// Whether a failure here should stop the user's flow.
    /// </summary>
    /// <remarks>
    /// Collecting a payment is essential — if no provider answers, the deal cannot proceed and the
    /// user must be told. Geocoding an address is not — a listing without map coordinates is a
    /// listing that still sells. Marking the difference here means the invoker knows whether to
    /// surface an error or degrade quietly, instead of every call site deciding for itself.
    /// </remarks>
    public bool IsEssential { get; private set; }

    public bool IsActive { get; private set; } = true;

    public DateTimeOffset CreatedAt { get; set; }

    public Guid? CreatedBy { get; set; }

    public DateTimeOffset? UpdatedAt { get; set; }

    public Guid? UpdatedBy { get; set; }

    public static IntegrationCapability Create(string code, string name, bool isEssential = true)
        => new(UuidV7.New(), code, name) { IsEssential = isEssential };

    public void Deactivate() => IsActive = false;
}

/// <summary>
/// The capability codes that have a typed adapter contract in this codebase.
/// </summary>
/// <remarks>
/// This list is code, and it is the same boundary <see cref="Workflow.WorkflowEffect"/> draws.
/// <b>Which provider</b> serves a capability, in which city, at which priority, is data an operator
/// edits. <b>What a capability's request and response look like</b> is a typed contract a developer
/// writes, reviews and tests — because a new capability is a new pair of C# types and a new adapter
/// interface, and no amount of JSON in a table produces those.
/// <para>
/// The rows in <see cref="IntegrationCapability"/> still exist: they carry the human name, the
/// description, and the active flag that the admin console lists and that lets operations disable a
/// whole capability during an incident. The constants here are the compile-time half of the same
/// fact, and they exist so a typo in a routing lookup is a build error rather than an empty
/// provider chain discovered at checkout.
/// </para>
/// </remarks>
public static class IntegrationCapabilities
{
    public const string PaymentCollect = "payment.collect";

    public const string PaymentPayout = "payment.payout";

    public const string SmsSend = "sms.send";

    public const string WhatsappSend = "whatsapp.send";

    public const string KycPanVerify = "kyc.pan_verify";

    public const string OcrRc = "ocr.rc";

    public const string ValuationPredict = "valuation.predict";

    public const string BureauScore = "bureau.score";

    public const string StorageObject = "storage.object";

    public const string MapsGeocode = "maps.geocode";

    public static IReadOnlyList<string> All { get; } =
    [
        PaymentCollect, PaymentPayout, SmsSend, WhatsappSend, KycPanVerify,
        OcrRc, ValuationPredict, BureauScore, StorageObject, MapsGeocode,
    ];
}
