using AutoHub.SharedKernel.Results;

namespace AutoHub.Integrations.Storage;

/// <summary>
/// The object store, behind an interface the product owns.
/// </summary>
/// <remarks>
/// R2 in production, MinIO locally, and neither of them running in CI. Same reasoning as
/// <see cref="Search.ISearchIndex"/>: which implementation is registered is a host decision, and a
/// missing bucket must not be the reason the API cannot start.
/// <para>
/// <b>Uploads are presigned, never proxied.</b> Twenty inspection photos routed through the API is
/// twenty multi-megabyte requests occupying request threads for the sake of putting bytes somewhere
/// the client could have put them directly. <see cref="CreateUploadTicketAsync"/> hands the client a
/// short-lived URL and keeps the API out of the data path entirely.
/// </para>
/// </remarks>
public interface IObjectStore
{
    /// <summary>A short-lived, scoped credential for the client to upload straight to storage.</summary>
    Task<Result<UploadTicket>> CreateUploadTicketAsync(
        UploadRequest request,
        CancellationToken cancellationToken);

    /// <summary>A time-boxed read URL. Inspection photos and invoices are not public objects.</summary>
    Task<Result<Uri>> CreateReadUrlAsync(string key, TimeSpan lifetime, CancellationToken cancellationToken);

    Task<Result> DeleteAsync(string key, CancellationToken cancellationToken);

    Task<bool> IsAvailableAsync(CancellationToken cancellationToken);
}

public sealed record UploadRequest
{
    /// <summary>Logical bucket — <c>listing-media</c>, <c>inspection</c>, <c>documents</c>.</summary>
    public required string Bucket { get; init; }

    /// <summary>Storage key. Callers mint it; the store never invents one, so retries are idempotent.</summary>
    public required string Key { get; init; }

    public required string ContentType { get; init; }

    /// <summary>
    /// Declared size, enforced by the presigned policy.
    /// </summary>
    /// <remarks>
    /// Enforced at the storage layer rather than trusted from the client: a ticket with no size cap
    /// is an anonymous, unmetered write endpoint pointed at our bill.
    /// </remarks>
    public long? MaxBytes { get; init; }
}

public sealed record UploadTicket(
    string Key,
    Uri UploadUrl,
    IReadOnlyDictionary<string, string> Headers,
    DateTimeOffset ExpiresAt);
