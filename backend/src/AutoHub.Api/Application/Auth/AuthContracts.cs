namespace AutoHub.Api.Application.Auth;

/// <summary>
/// The response shapes of §8 of the contract, spelled out.
/// </summary>
/// <remarks>
/// Written as records here rather than projected from the domain because the wire shape is a
/// contract with three shipped clients and the domain is not. A rename in <c>User</c> must be a
/// compile error in this file, not a silent change to what a Flutter app parses.
/// </remarks>
public sealed record OtpChallengeResponse(
    string ChallengeId,
    int ExpiresInSeconds,
    int ResendAfterSeconds,
    string MaskedTo,
    string DeliveryChannel,
    int AttemptsAllowed,
    string? DevCode);

public sealed record AuthUserResponse(
    string Id,
    string DisplayName,
    string? Phone,
    string? Email,
    IReadOnlyList<string> Roles,
    IReadOnlyList<string> Permissions,
    string? ActiveCity,
    int KycLevel);

public sealed record AuthSessionResponse(
    string AccessToken,
    string RefreshToken,
    int ExpiresIn,
    string TokenType,
    AuthUserResponse User,
    bool IsNewUser,
    string? NextStep);

public sealed record TokenPairResponse(
    string AccessToken,
    string RefreshToken,
    int ExpiresIn,
    string TokenType);
