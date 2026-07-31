namespace AutoHub.SharedKernel.Security;

/// <summary>
/// Who is making the current request, injected rather than read from a static.
/// </summary>
/// <remarks>
/// The sibling of <see cref="Time.IClock"/>, and it exists for the same reason: the two facts every
/// audit row needs — who and when — must arrive the same way in a request, in a background sweep,
/// and in a test, or the rows written by one of those three will be wrong and nobody will notice
/// until an investigation needs them.
/// <para>
/// <see cref="UserId"/> is deliberately nullable. An OTP request, a public valuation quote, and the
/// outbox processor all write rows and none of them has a user. Modelling that as null — rather than
/// as a sentinel "system" guid — keeps "nobody was signed in" distinguishable from "a service
/// account did it", which is exactly the distinction a fraud review turns on.
/// </para>
/// <para>
/// This carries identity only. It carries no roles and no permission set, because a permission is
/// resolved from the RBAC registry per check and a copy of it cached on an ambient context is a copy
/// that goes stale the moment access is revoked.
/// </para>
/// </remarks>
public interface ICurrentUser
{
    Guid? UserId { get; }

    /// <summary>Stable per install, from the <c>X-Device-Id</c> header. Feeds fraud signals.</summary>
    string? DeviceId { get; }

    /// <summary>Caller IP, for the audit trail's "from where". Null when there is no request.</summary>
    string? IpAddress { get; }
}

/// <summary>
/// The current user when there is no request — background services, migrations, seeding.
/// </summary>
/// <remarks>
/// Registered as the fallback so that resolving <see cref="ICurrentUser"/> never fails outside a
/// request scope. A hosted service that could not construct its dependencies would take the whole
/// host down at boot, which is a spectacular way to fail at "nobody was signed in".
/// </remarks>
public sealed class UnauthenticatedUser : ICurrentUser
{
    public static UnauthenticatedUser Instance { get; } = new();

    public Guid? UserId => null;

    public string? DeviceId => null;

    public string? IpAddress => null;
}
