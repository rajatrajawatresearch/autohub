using System.Text.Json.Nodes;
using AutoHub.Platform.Integrations;
using Microsoft.EntityFrameworkCore;

namespace AutoHub.Persistence.Seed;

/// <summary>
/// Registry #8 — which vendor serves which capability, in what order.
/// </summary>
/// <remarks>
/// Routes, not <c>if</c> statements. Every capability has a primary and at least one fallback, so
/// "swap Razorpay for Cashfree and keep Razorpay as the fallback" is two priority edits over two
/// weeks with a way back at every point.
/// <para>
/// <b>The development gateway.</b> The payment adapters in this build are simulators — they refuse
/// to run unless the provider row they were routed through says <c>"mode": "simulate"</c>, which is
/// why that flag is opt-in rather than opt-out: a simulator that defaults to on is one missing
/// environment variable away from a deal settling with no money anywhere. Both seeded payment
/// providers are <see cref="IntegrationEnvironment.Sandbox"/> rows in simulate mode, and the live
/// rows are created by whoever holds the real credentials — never by a seeder.
/// </para>
/// <para>
/// <b>No secret is seeded, or storable.</b> Every provider carries a <c>vault://</c> reference and
/// the database's own check constraint rejects anything that does not look like one. The point is
/// not that secrets are sensitive — everybody agrees and stores them anyway — it is that a row is
/// copied into every backup, every replica and every admin screen, and a reference is worth nothing
/// in all three places.
/// </para>
/// </remarks>
internal sealed class IntegrationSeeder
{
    private readonly AutoHubDbContext _context;

    public IntegrationSeeder(AutoHubDbContext context) => _context = context;

    public async Task<SeedStepResult> SeedAsync(CancellationToken cancellationToken)
    {
        var created = await SeedCapabilitiesAsync(cancellationToken);
        created += await SeedProvidersAsync(cancellationToken);
        created += await SeedRoutesAsync(cancellationToken);

        var total = await _context.IntegrationCapabilities.CountAsync(cancellationToken)
                    + await _context.IntegrationProviders.CountAsync(cancellationToken)
                    + await _context.ProviderRoutes.CountAsync(cancellationToken);

        return new SeedStepResult("integrations", created, total - created);
    }

    // ── Capabilities ─────────────────────────────────────────────────────────────────────────────

    private static readonly CapabilityRow[] Capabilities =
    [
        new(IntegrationCapabilities.PaymentCollect, "Collect a payment", true),
        new(IntegrationCapabilities.PaymentPayout, "Pay a seller or dealer", true),
        new(IntegrationCapabilities.SmsSend, "Send an SMS", true),
        new(IntegrationCapabilities.WhatsappSend, "Send a WhatsApp message", false),
        new(IntegrationCapabilities.KycPanVerify, "Verify a PAN", true),
        new(IntegrationCapabilities.OcrRc, "Read an RC from a photo", false),
        new(IntegrationCapabilities.ValuationPredict, "Predict a vehicle's value", true),
        new(IntegrationCapabilities.BureauScore, "Fetch a credit bureau score", false),
        new(IntegrationCapabilities.StorageObject, "Store an object", true),
        new(IntegrationCapabilities.MapsGeocode, "Geocode an address", false),
    ];

    private async Task<int> SeedCapabilitiesAsync(CancellationToken cancellationToken)
    {
        var known = (await _context.IntegrationCapabilities
                .Select(capability => capability.Code)
                .ToListAsync(cancellationToken))
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        var created = 0;

        foreach (var row in Capabilities.Where(row => !known.Contains(row.Code)))
        {
            _context.IntegrationCapabilities.Add(
                IntegrationCapability.Create(row.Code, row.Name, row.IsEssential));
            created++;
        }

        await _context.SaveChangesAsync(cancellationToken);
        return created;
    }

    // ── Providers ────────────────────────────────────────────────────────────────────────────────

    private static readonly ProviderRow[] Providers =
    [
        // The development gateway: Razorpay's sandbox row, in simulate mode. The adapter checks that
        // flag on every call and refuses without it, which is why it is stated here explicitly.
        new("razorpay", IntegrationCapabilities.PaymentCollect, "Razorpay (development simulator)",
            IntegrationEnvironment.Sandbox, "vault://autohub/payments/razorpay/sandbox", 20, 1,
            """
            {"mode":"simulate","checkoutBaseUrl":"https://checkout.simulate.invalid/razorpay",
             "vpa":"autohub@simulate","methods":["upi","card","netbanking","wallet"]}
            """),

        new("cashfree", IntegrationCapabilities.PaymentCollect, "Cashfree (development simulator)",
            IntegrationEnvironment.Sandbox, "vault://autohub/payments/cashfree/sandbox", 20, 1,
            """
            {"mode":"simulate","checkoutBaseUrl":"https://checkout.simulate.invalid/cashfree",
             "vpa":"autohub@simulate","methods":["upi","card","netbanking"]}
            """),

        new("razorpayx", IntegrationCapabilities.PaymentPayout, "RazorpayX payouts",
            IntegrationEnvironment.Sandbox, "vault://autohub/payouts/razorpayx/sandbox", 30, 1,
            """{"mode":"simulate","settlementSchedule":"t_plus_1"}"""),

        new("msg91", IntegrationCapabilities.SmsSend, "MSG91",
            IntegrationEnvironment.Sandbox, "vault://autohub/sms/msg91/sandbox", 15, 2,
            """{"senderId":"AUTOHB","route":"transactional","dltEntityId":"1101100000000000000"}"""),

        new("kaleyra", IntegrationCapabilities.SmsSend, "Kaleyra",
            IntegrationEnvironment.Sandbox, "vault://autohub/sms/kaleyra/sandbox", 15, 2,
            """{"senderId":"AUTOHB","route":"transactional","dltEntityId":"1101100000000000000"}"""),

        new("gupshup", IntegrationCapabilities.WhatsappSend, "Gupshup WhatsApp Business",
            IntegrationEnvironment.Sandbox, "vault://autohub/whatsapp/gupshup/sandbox", 20, 1,
            """{"wabaId":"autohub-sandbox","namespace":"autohub_in"}"""),

        new("karza", IntegrationCapabilities.KycPanVerify, "Karza PAN verification",
            IntegrationEnvironment.Sandbox, "vault://autohub/kyc/karza/sandbox", 30, 1,
            """{"endpoint":"https://sandbox.karza.in/v3/pan-authentication","consentRequired":true}"""),

        new("signzy", IntegrationCapabilities.KycPanVerify, "Signzy PAN verification",
            IntegrationEnvironment.Sandbox, "vault://autohub/kyc/signzy/sandbox", 30, 1,
            """{"endpoint":"https://sandbox.signzy.in/api/v2/pan","consentRequired":true}"""),

        new("karza-ocr", IntegrationCapabilities.OcrRc, "Karza RC OCR",
            IntegrationEnvironment.Sandbox, "vault://autohub/ocr/karza/sandbox", 45, 1,
            """{"endpoint":"https://sandbox.karza.in/v2/rc-ocr","minConfidence":0.82}"""),

        new("autohub-ml", IntegrationCapabilities.ValuationPredict, "AutoHub valuation ensemble",
            IntegrationEnvironment.Sandbox, "vault://autohub/valuation/model/sandbox", 10, 2,
            """{"modelVersion":"valuation-ensemble@2026.07.3","fallbackToDepreciationCurve":true}"""),

        new("depreciation-curve", IntegrationCapabilities.ValuationPredict, "Depreciation curve fallback",
            IntegrationEnvironment.Sandbox, "vault://autohub/valuation/curve/sandbox", 5, 1,
            """{"mode":"deterministic","note":"Straight-line depreciation. Used when the model is unavailable."}"""),

        new("minio", IntegrationCapabilities.StorageObject, "MinIO (local S3)",
            IntegrationEnvironment.Sandbox, "vault://autohub/storage/minio/local", 30, 2,
            """{"endpoint":"http://localhost:9000","bucket":"autohub-media","forcePathStyle":true}"""),
    ];

    private async Task<int> SeedProvidersAsync(CancellationToken cancellationToken)
    {
        var existing = await _context.IntegrationProviders
            .Select(provider => new { provider.CapabilityCode, provider.Code, provider.Environment })
            .ToListAsync(cancellationToken);

        var known = existing
            .Select(provider => (
                provider.CapabilityCode.ToUpperInvariant(),
                provider.Code.ToUpperInvariant(),
                provider.Environment))
            .ToHashSet();

        var created = 0;

        foreach (var row in Providers)
        {
            var key = (row.CapabilityCode.ToUpperInvariant(), row.Code.ToUpperInvariant(), row.Environment);

            if (!known.Add(key))
            {
                continue;
            }

            var provider = SeedGuard.Required(
                IntegrationProvider.Create(
                    row.Code, row.CapabilityCode, row.DisplayName, row.Environment, row.CredentialRef),
                $"integration provider {row.Code}");

            provider.Configure(JsonNode.Parse(row.ConfigJson));
            provider.SetLimits(row.TimeoutSeconds, row.MaxAttempts);

            _context.IntegrationProviders.Add(provider);
            created++;
        }

        await _context.SaveChangesAsync(cancellationToken);
        return created;
    }

    // ── Routes ───────────────────────────────────────────────────────────────────────────────────
    //
    // Lower priority is tried first, and the gaps are deliberate: authoring at 10, 20, 30 leaves room
    // to slot a new vendor in at 15 without renumbering a chain somebody is watching a dashboard for.

    private static readonly RouteRow[] Routes =
    [
        new(IntegrationCapabilities.PaymentCollect, "razorpay", 10, null),
        new(IntegrationCapabilities.PaymentCollect, "cashfree", 20, null),
        new(IntegrationCapabilities.PaymentPayout, "razorpayx", 10, null),

        // Two SMS vendors. A provider appears in a capability's chain exactly once — the database
        // enforces it — because a chain listing the same vendor twice would fail over to the gateway
        // that had just failed. So the split is by priority, not by repetition: MSG91 leads because
        // OTP delivery latency is the biggest drop-off in a phone-first signup, and Kaleyra catches
        // everything MSG91 does not take.
        new(IntegrationCapabilities.SmsSend, "msg91", 10, null),
        new(IntegrationCapabilities.SmsSend, "kaleyra", 20, null),

        new(IntegrationCapabilities.WhatsappSend, "gupshup", 10, null),

        new(IntegrationCapabilities.KycPanVerify, "karza", 10, null),
        new(IntegrationCapabilities.KycPanVerify, "signzy", 20, null),

        new(IntegrationCapabilities.OcrRc, "karza-ocr", 10, null),

        new(IntegrationCapabilities.ValuationPredict, "autohub-ml", 10, null),
        new(IntegrationCapabilities.ValuationPredict, "depreciation-curve", 20, null),

        new(IntegrationCapabilities.StorageObject, "minio", 10, null),
    ];

    private async Task<int> SeedRoutesAsync(CancellationToken cancellationToken)
    {
        var existing = await _context.ProviderRoutes
            .Select(route => new { route.CapabilityCode, route.ProviderCode })
            .ToListAsync(cancellationToken);

        var known = existing
            .Select(route => (
                route.CapabilityCode.ToUpperInvariant(),
                route.ProviderCode.ToUpperInvariant()))
            .ToHashSet();

        var created = 0;

        foreach (var row in Routes)
        {
            var key = (row.CapabilityCode.ToUpperInvariant(), row.ProviderCode.ToUpperInvariant());

            if (!known.Add(key))
            {
                continue;
            }

            _context.ProviderRoutes.Add(ProviderRoute.Create(
                row.CapabilityCode, row.ProviderCode, row.Priority, row.ConditionJson));

            created++;
        }

        await _context.SaveChangesAsync(cancellationToken);
        return created;
    }
}

internal sealed record CapabilityRow(string Code, string Name, bool IsEssential);

internal sealed record ProviderRow(
    string Code,
    string CapabilityCode,
    string DisplayName,
    IntegrationEnvironment Environment,
    string CredentialRef,
    int TimeoutSeconds,
    int MaxAttempts,
    string ConfigJson);

internal sealed record RouteRow(
    string CapabilityCode,
    string ProviderCode,
    int Priority,
    string? ConditionJson);
