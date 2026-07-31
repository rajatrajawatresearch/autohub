using AutoHub.Platform.Access;
using AutoHub.SharedKernel.Security;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace AutoHub.Api.Security;

/// <summary>One permission code an endpoint requires.</summary>
public sealed class PermissionRequirement : IAuthorizationRequirement
{
    public PermissionRequirement(string permission) => Permission = permission;

    public string Permission { get; }
}

/// <summary>
/// Materialises one authorisation policy per permission code, on demand.
/// </summary>
/// <remarks>
/// The alternative is a startup loop that registers a policy for every code in
/// <see cref="PermissionCatalog"/>. That works, and it makes the policy set a snapshot taken at boot
/// — so a permission a module declares later, or one an endpoint names before the catalog is
/// updated, becomes a silent <c>403</c> with nothing in the logs saying why. Building on demand
/// means the policy always exists and the <i>resolver</i> decides, which is where the decision
/// belongs.
/// <para>
/// It also keeps the endpoint declaration honest: <c>RequireAuthorization(PlatformPermissions
/// .PricingPublish)</c> reads as the permission it enforces, with no indirection through a policy
/// name that could drift from it.
/// </para>
/// <para>
/// Names that are not permission codes fall through to the wrapped default provider, so
/// <c>[Authorize]</c> with no policy and any hand-registered policy still work.
/// </para>
/// </remarks>
internal sealed class PermissionPolicyProvider : IAuthorizationPolicyProvider
{
    private readonly DefaultAuthorizationPolicyProvider _fallback;

    public PermissionPolicyProvider(IOptions<AuthorizationOptions> options)
        => _fallback = new DefaultAuthorizationPolicyProvider(options);

    public Task<AuthorizationPolicy> GetDefaultPolicyAsync()
        => Task.FromResult(new AuthorizationPolicyBuilder(JwtBearerDefaults.AuthenticationScheme)
            .RequireAuthenticatedUser()
            .Build());

    public Task<AuthorizationPolicy?> GetFallbackPolicyAsync() => Task.FromResult<AuthorizationPolicy?>(null);

    public async Task<AuthorizationPolicy?> GetPolicyAsync(string policyName)
    {
        var code = PermissionCode.TryParse(policyName);

        if (code.IsFailure)
        {
            return await _fallback.GetPolicyAsync(policyName);
        }

        return new AuthorizationPolicyBuilder(JwtBearerDefaults.AuthenticationScheme)
            .RequireAuthenticatedUser()
            .AddRequirements(new PermissionRequirement(code.Value.Value))
            .Build();
    }
}

/// <summary>
/// Answers a permission requirement from the RBAC registry.
/// </summary>
/// <remarks>
/// Scope-blind on purpose. This checks "may this actor do this at all?", which is the right question
/// for admitting a request; the endpoint that owns the entity then asks the scoped question about
/// that entity's city or dealer, because only it knows which entity is involved. Two checks rather
/// than one, exactly as <see cref="ResolvedPermissionSet"/> documents.
/// </remarks>
internal sealed class PermissionAuthorizationHandler : AuthorizationHandler<PermissionRequirement>
{
    private readonly IPermissionResolver _resolver;
    private readonly ICurrentUser _currentUser;
    private readonly ILogger<PermissionAuthorizationHandler> _logger;

    public PermissionAuthorizationHandler(
        IPermissionResolver resolver,
        ICurrentUser currentUser,
        ILogger<PermissionAuthorizationHandler> logger)
    {
        _resolver = resolver;
        _currentUser = currentUser;
        _logger = logger;
    }

    protected override async Task HandleRequirementAsync(
        AuthorizationHandlerContext context,
        PermissionRequirement requirement)
    {
        if (_currentUser.UserId is not { } userId)
        {
            // Not a failure — leaving the requirement unmet lets the authenticated-user requirement
            // report the honest 401 rather than this reporting a misleading 403.
            return;
        }

        var permissions = await _resolver.ResolveAsync(userId, CancellationToken.None);

        if (permissions.Holds(requirement.Permission))
        {
            context.Succeed(requirement);
            return;
        }

        // Logged at Information, not Warning: a denied permission is a normal outcome of a client
        // that has not refreshed its menu, and alerting on it would train everyone to ignore it.
        // The line still exists, because "why did that 403?" is otherwise unanswerable.
        _logger.LogInformation(
            "User {UserId} does not hold {Permission}",
            userId, requirement.Permission);
    }
}
