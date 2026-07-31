using System.Globalization;
using System.Text.Json.Nodes;
using AutoHub.SharedKernel.Results;

namespace AutoHub.Platform.Integrations.PaymentCollect;

/// <summary>
/// The guard that keeps a simulating payment adapter out of production.
/// </summary>
/// <remarks>
/// An adapter that fabricates a successful payment is the most dangerous class in this codebase: it
/// makes a deal settle, a commission post, and a vehicle change hands, with no money anywhere. So it
/// refuses to run unless the provider row it was routed through explicitly declares
/// <c>"mode": "simulate"</c>, and the refusal is an <see cref="ErrorKind.Unexpected"/> rather than an
/// upstream one so the invoker does not treat it as a transient vendor problem and fail over into
/// another simulator.
/// <para>
/// Opt-in rather than opt-out on purpose. A flag that defaults to simulating is one missing
/// environment variable away from a production incident; a flag that defaults to refusing is one
/// missing environment variable away from a loud, immediate, obvious failure in staging.
/// </para>
/// </remarks>
public sealed record PaymentSimulation
{
    private PaymentSimulation(string checkoutBaseUrl, string virtualPaymentAddress)
    {
        CheckoutBaseUrl = checkoutBaseUrl;
        VirtualPaymentAddress = virtualPaymentAddress;
    }

    private const string ModeKey = "mode";
    private const string SimulateMode = "simulate";

    public string CheckoutBaseUrl { get; }

    public string VirtualPaymentAddress { get; }

    public static Result<PaymentSimulation> Require(ResolvedProvider provider, string providerCode)
    {
        var config = provider.Config as JsonObject;
        var mode = config?[ModeKey]?.GetValue<string>();

        if (!string.Equals(mode, SimulateMode, StringComparison.Ordinal))
        {
            return Error.Unexpected(
                "integration.adapter_not_installed",
                $"The {providerCode} payment adapter in this build only simulates. " +
                "Install the AutoHub.Integrations adapter or set the provider to simulate mode.")
                .With("provider", providerCode);
        }

        return new PaymentSimulation(
            config?["checkoutBaseUrl"]?.GetValue<string>() ?? $"https://simulate.invalid/{providerCode}",
            config?["vpa"]?.GetValue<string>() ?? "autohub@simulate");
    }

    /// <summary>
    /// A stable pseudo-reference derived from the idempotency key, so replaying a simulated call
    /// returns the same provider reference a real gateway would have returned.
    /// </summary>
    public static string Reference(string idempotencyKey)
        => Math.Abs(idempotencyKey.GetHashCode(StringComparison.Ordinal))
            .ToString("x8", CultureInfo.InvariantCulture);
}
