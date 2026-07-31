using System.Text.Json.Nodes;
using AutoHub.Api.Security;
using AutoHub.Platform.Auditing;
using AutoHub.Platform.Caching;
using AutoHub.SharedKernel.Security;
using AutoHub.SharedKernel.Time;
using Microsoft.Extensions.Logging;

namespace AutoHub.Api.Application.RegistryAdmin;

/// <summary>
/// The two things that must happen together on every registry write: an audit row and a version bump.
/// </summary>
/// <remarks>
/// They are one call because they are one obligation, and separating them produces two failures that
/// are individually silent. A bump without an audit row is a change to what dealers are charged that
/// nobody can trace. An audit row without a bump is a change an operator was told took effect and
/// which every node keeps serving the old version of — so they publish again, and again, and file a
/// bug about the admin panel.
/// <para>
/// The audit row joins the caller's transaction and is written by the transaction behaviour; the
/// version bump goes to Redis and is not transactional, so it is done last. A bump that lands after
/// a rollback costs one unnecessary cache miss, which is the cheapest of the available failures.
/// </para>
/// </remarks>
internal sealed class RegistryPublisher
{
    private readonly IAuditTrail _audit;
    private readonly IRegistryCache _cache;
    private readonly ICurrentUser _currentUser;
    private readonly IRequestContext _request;
    private readonly IClock _clock;
    private readonly ILogger<RegistryPublisher> _logger;

    public RegistryPublisher(
        IAuditTrail audit,
        IRegistryCache cache,
        ICurrentUser currentUser,
        IRequestContext request,
        IClock clock,
        ILogger<RegistryPublisher> logger)
    {
        _audit = audit;
        _cache = cache;
        _currentUser = currentUser;
        _request = request;
        _clock = clock;
        _logger = logger;
    }

    /// <summary>Records the change and retires every cached snapshot of the registry.</summary>
    public async Task<long> PublishAsync(
        string registry,
        string action,
        string entityType,
        string? entityKey,
        Guid? entityId,
        string summary,
        JsonNode? before,
        JsonNode? after,
        CancellationToken cancellationToken)
    {
        Record(action, entityType, entityKey, entityId, summary, before, after);

        var version = await _cache.InvalidateAsync(registry, cancellationToken);

        _logger.LogInformation(
            "Registry {Registry} published to v{Version} by {ActorId} ({Action})",
            registry, version.Value, _currentUser.UserId, action);

        return version.Value;
    }

    /// <summary>
    /// Records a change that is not yet visible — a draft edit.
    /// </summary>
    /// <remarks>
    /// Drafts are audited too. "Who wrote the rule that was published on Tuesday?" and "who published
    /// it?" are different questions with different answers, and an approval process that separates
    /// authoring from publishing is worth nothing if only the second half is recorded.
    /// </remarks>
    public void Record(
        string action,
        string entityType,
        string? entityKey,
        Guid? entityId,
        string summary,
        JsonNode? before = null,
        JsonNode? after = null)
        => _audit.Record(AuditEntry.Record(
            _clock.UtcNow,
            action,
            entityType,
            entityKey,
            entityId,
            _currentUser.UserId,
            _currentUser.IpAddress,
            _request.TraceId,
            summary,
            before,
            after));
}

/// <summary>What an admin write reports back.</summary>
/// <remarks>
/// The version is echoed because an operator who cannot tell whether their change took effect will
/// make it again — and "make it again" on a pricing rule set means two rule sets.
/// </remarks>
public sealed record RegistryWriteResponse(string Registry, long Version, string? Key, Guid? Id);
