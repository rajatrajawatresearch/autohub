using AutoHub.SharedKernel.Results;
using Microsoft.Extensions.Logging;

namespace AutoHub.Api.Application.Auth;

/// <summary>
/// Gets the code to the phone.
/// </summary>
/// <remarks>
/// An interface with one implementation today, because the real one is a composition of two
/// registries that are not wired up yet: the notification registry picks the template for the
/// channel and locale, and the integration registry routes it to an SMS provider with a fallback
/// chain. When those land, this becomes their entry point rather than being replaced — the auth
/// flow already treats delivery as something that can fail and be retried.
/// </remarks>
public interface IOtpDeliveryChannel
{
    /// <summary>The channel name reported to the client, e.g. <c>sms</c>.</summary>
    string Channel { get; }

    Task<Result> SendAsync(string phone, string code, string purpose, CancellationToken cancellationToken);
}

/// <summary>
/// Writes the code to the log instead of sending it.
/// </summary>
/// <remarks>
/// The development stand-in, and it is deliberately the <i>only</i> stand-in. The alternative
/// developers reach for when there is no SMS provider — a fixed code like <c>000000</c> that the
/// verifier always accepts — is a backdoor that has shipped to production more than once in this
/// industry. Logging a real, random, single-use code keeps the entire flow honest: the challenge is
/// real, the attempt counter is real, expiry is real, and the only thing that differs from
/// production is the transport.
/// <para>
/// It logs at Warning, not Information, so it is visible in any environment where it should not be
/// running.
/// </para>
/// </remarks>
internal sealed class LoggingOtpDeliveryChannel : IOtpDeliveryChannel
{
    private readonly ILogger<LoggingOtpDeliveryChannel> _logger;

    public LoggingOtpDeliveryChannel(ILogger<LoggingOtpDeliveryChannel> logger) => _logger = logger;

    public string Channel => "sms";

    public Task<Result> SendAsync(
        string phone,
        string code,
        string purpose,
        CancellationToken cancellationToken)
    {
        _logger.LogWarning(
            "OTP delivery is not configured — code for {Phone} ({Purpose}) is {Code}",
            phone, purpose, code);

        return Task.FromResult(Result.Success());
    }
}
