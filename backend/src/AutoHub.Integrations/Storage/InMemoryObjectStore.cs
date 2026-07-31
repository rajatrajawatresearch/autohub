using System.Collections.Concurrent;
using AutoHub.SharedKernel.Results;
using AutoHub.SharedKernel.Time;

namespace AutoHub.Integrations.Storage;

/// <summary>
/// An object store that records what would have been uploaded, so the API boots without MinIO.
/// </summary>
/// <remarks>
/// It issues real, unique, expiring tickets against a loopback URL. A client that follows the flow
/// end to end gets a coherent sequence — ticket, key, read URL — and only discovers there are no
/// bytes when it tries to fetch them, which is the correct place for that discovery: a developer
/// working on the sell flow needs the ticket to be right, not the storage to be real.
/// <para>
/// It keeps keys and never content. Holding uploads in process memory would turn a bulk import into
/// an out-of-memory kill, and pretending to be durable is the one thing a fallback must not do.
/// </para>
/// </remarks>
public sealed class InMemoryObjectStore : IObjectStore
{
    private readonly ConcurrentDictionary<string, DateTimeOffset> _keys = new(StringComparer.Ordinal);
    private readonly IClock _clock;
    private readonly Uri _baseUri;

    public InMemoryObjectStore(IClock clock, Uri? baseUri = null)
    {
        _clock = clock;
        _baseUri = baseUri ?? new Uri("http://localhost:5180/_local-object-store/");
    }

    public Task<Result<UploadTicket>> CreateUploadTicketAsync(
        UploadRequest request,
        CancellationToken cancellationToken)
    {
        var now = _clock.UtcNow;
        var key = $"{request.Bucket}/{request.Key}";
        _keys[key] = now;

        var ticket = new UploadTicket(
            key,
            new Uri(_baseUri, Uri.EscapeDataString(key)),
            new Dictionary<string, string>(StringComparer.Ordinal) { ["Content-Type"] = request.ContentType },
            now.AddMinutes(15));

        return Task.FromResult(Result.Success(ticket));
    }

    public Task<Result<Uri>> CreateReadUrlAsync(
        string key,
        TimeSpan lifetime,
        CancellationToken cancellationToken)
        => Task.FromResult(Result.Success(new Uri(_baseUri, Uri.EscapeDataString(key))));

    public Task<Result> DeleteAsync(string key, CancellationToken cancellationToken)
    {
        _keys.TryRemove(key, out _);
        return Task.FromResult(Result.Success());
    }

    public Task<bool> IsAvailableAsync(CancellationToken cancellationToken) => Task.FromResult(true);
}
