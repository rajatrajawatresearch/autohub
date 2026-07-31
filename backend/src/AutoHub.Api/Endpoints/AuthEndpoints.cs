using AutoHub.Api.Application.Auth;
using AutoHub.Api.Http;
using MediatR;

namespace AutoHub.Api.Endpoints;

/// <summary>
/// §8 of the contract — OTP-first, because India is phone-first.
/// </summary>
/// <remarks>
/// Everything except <c>/auth/me</c> is anonymous, which is the point of an authentication endpoint.
/// <c>/auth/refresh</c> is anonymous too and authenticates on the refresh token itself: an expired
/// access token is exactly the situation it exists for, so requiring a valid one would make it
/// unreachable at the only moment it is needed.
/// </remarks>
internal static class AuthEndpoints
{
    public static IEndpointRouteBuilder MapAuthEndpoints(this IEndpointRouteBuilder app)
    {
        var auth = app.MapGroup("/auth").WithTags("Auth");

        auth.MapPost("/otp/request", async (
                OtpRequestBody body,
                ISender sender,
                HttpContext context,
                CancellationToken ct) =>
            {
                var challenge = await sender.Send(
                    new RequestOtpCommand(body.Phone, body.Purpose, body.Channel), ct);

                return challenge.IsSuccess
                    ? Results.Ok(challenge.Value)
                    : ApiProblem.Result(challenge.Error, context);
            })
            .AllowAnonymous()
            .WithName("RequestOtp")
            .WithSummary("Sends a one-time code to a phone number.");

        auth.MapPost("/otp/verify", async (
                OtpVerifyBody body,
                ISender sender,
                HttpContext context,
                CancellationToken ct) =>
            {
                var session = await sender.Send(
                    new VerifyOtpCommand(body.ChallengeId, body.Code, body.DeviceId), ct);

                return session.IsSuccess
                    ? Results.Ok(session.Value)
                    : ApiProblem.Result(session.Error, context);
            })
            .AllowAnonymous()
            .WithName("VerifyOtp")
            .WithSummary("Exchanges a verified code for a session. Signs up on first use.");

        auth.MapPost("/refresh", async (
                RefreshBody body,
                ISender sender,
                HttpContext context,
                CancellationToken ct) =>
            {
                var pair = await sender.Send(
                    new RefreshSessionCommand(body.RefreshToken, body.DeviceId), ct);

                return pair.IsSuccess
                    ? Results.Ok(pair.Value)
                    : ApiProblem.Result(pair.Error, context);
            })
            .AllowAnonymous()
            .WithName("RefreshSession")
            .WithSummary("Rotates the token pair. A replayed token revokes the whole family.");

        auth.MapPost("/logout", async (
                LogoutBody body,
                ISender sender,
                HttpContext context,
                CancellationToken ct) =>
            {
                var outcome = await sender.Send(new LogoutCommand(body.RefreshToken), ct);

                // 204, per the contract. A logout that reports failure is a logout a client retries,
                // and a client that cannot log out leaves a live session on a shared device.
                return outcome.IsSuccess
                    ? Results.NoContent()
                    : ApiProblem.Result(outcome.Error, context);
            })
            .AllowAnonymous()
            .WithName("Logout")
            .WithSummary("Ends the session the presented refresh token belongs to.");

        auth.MapGet("/me", async (ISender sender, HttpContext context, CancellationToken ct) =>
            {
                var user = await sender.Send(new GetCurrentUserQuery(), ct);

                return user.IsSuccess
                    ? Results.Ok(user.Value)
                    : ApiProblem.Result(user.Error, context);
            })
            .RequireAuthorization()
            .WithName("GetCurrentUser")
            .WithSummary("Resolved permissions for the caller. Never roles-as-authorisation.");

        return app;
    }
}

public sealed record OtpRequestBody(string Phone, string? Purpose, string? Channel);

public sealed record OtpVerifyBody(string ChallengeId, string Code, string? DeviceId);

public sealed record RefreshBody(string RefreshToken, string? DeviceId);

public sealed record LogoutBody(string? RefreshToken);
