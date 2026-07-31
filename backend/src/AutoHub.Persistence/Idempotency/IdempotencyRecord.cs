using System.Security.Cryptography;
using System.Text;
using System.Text.Json.Nodes;

namespace AutoHub.Persistence.Idempotency;

/// <summary>
/// One <c>Idempotency-Key</c> and the response it produced.
/// </summary>
/// <remarks>
/// The quality floor requires every write endpoint to be idempotent, and the reason is concrete: a
/// dealer on a train taps "Place bid", the connection drops before the response arrives, and the app
/// retries. Without this row that is two bids, two holds on the dealer's limit, and a lot won at a
/// price nobody meant to offer. With it, the retry returns the first response byte for byte.
/// <para>
/// <b>The key is the primary key.</b> A replay check that costs a primary-key lookup can sit in
/// front of every write endpoint without anyone arguing about the overhead, and it is the same index
/// that enforces the uniqueness the mechanism depends on. A surrogate id plus a unique index would
/// be a second index for no benefit.
/// </para>
/// <para>
/// <b>A key replayed with a different body is a failure, loudly.</b> Returning the original response
/// would be worse than either alternative: the client asked to do something, got a success, and the
/// something never happened. That is a client bug — a key generated once and reused across two
/// different requests — and the only useful response is one that says so.
/// </para>
/// </remarks>
public sealed class IdempotencyRecord
{
    private IdempotencyRecord(
        string key,
        string endpoint,
        Guid? userId,
        string requestHash,
        DateTimeOffset createdAt,
        DateTimeOffset expiresAt)
    {
        Key = key;
        Endpoint = endpoint;
        UserId = userId;
        RequestHash = requestHash;
        CreatedAt = createdAt;
        ExpiresAt = expiresAt;
    }

    private IdempotencyRecord() { }

    /// <summary>The client's <c>Idempotency-Key</c> header, verbatim.</summary>
    public string Key { get; private set; } = string.Empty;

    /// <summary>
    /// Method and route template, e.g. <c>POST /api/v1/auction/lots/{id}/bids</c>.
    /// </summary>
    /// <remarks>
    /// Recorded so a key reused against a different endpoint is caught as the mistake it is, rather
    /// than replaying a bid's response to a payment request. It is not part of the key: a client
    /// that reuses one key across two endpoints has a bug we want to surface, not accommodate.
    /// </remarks>
    public string Endpoint { get; private set; } = string.Empty;

    /// <summary>
    /// The caller. Null for anonymous writes — an OTP request has no user yet.
    /// </summary>
    /// <remarks>
    /// Checked on replay. Keys are client-generated and therefore guessable in principle; without
    /// this check, a replay by a different user would hand them somebody else's response body.
    /// </remarks>
    public Guid? UserId { get; private set; }

    /// <summary>SHA-256 of the canonical request body. See <see cref="ComputeHash"/>.</summary>
    public string RequestHash { get; private set; } = string.Empty;

    /// <summary>Null until the original request finished. See the remarks on <see cref="IsInFlight"/>.</summary>
    public int? ResponseStatus { get; private set; }

    public JsonNode? ResponseBody { get; private set; }

    public DateTimeOffset CreatedAt { get; private set; }

    /// <summary>
    /// When the row may be swept.
    /// </summary>
    /// <remarks>
    /// Idempotency is a promise with a horizon, and the horizon has to be stated. Twenty-four hours
    /// covers every retry a client library, a mobile app resuming from background, or a person
    /// re-tapping a button will ever perform; keeping them forever turns a hot write table into an
    /// unbounded one and makes the primary-key index grow without ever being read.
    /// </remarks>
    public DateTimeOffset ExpiresAt { get; private set; }

    /// <summary>
    /// A record claimed but not yet completed — the original request is still running.
    /// </summary>
    /// <remarks>
    /// This is the double-submit case, and it is the interesting one: the retry arrives while the
    /// first request is mid-flight, so there is no response to replay yet. Answering <c>409</c> is
    /// correct and honest — the operation is in progress — and is very different from answering
    /// "already done", which would be a lie the client acts on.
    /// </remarks>
    public bool IsInFlight => ResponseStatus is null;

    public static IdempotencyRecord Begin(
        string key,
        string endpoint,
        Guid? userId,
        string requestHash,
        DateTimeOffset now,
        TimeSpan retention)
        => new(key, endpoint, userId, requestHash, now, now.Add(retention));

    public void Complete(int responseStatus, JsonNode? responseBody)
    {
        ResponseStatus = responseStatus;
        ResponseBody = responseBody;
    }

    /// <summary>
    /// Canonical hash of a request body.
    /// </summary>
    /// <remarks>
    /// Hashed rather than stored because the body can be large and can contain personal data, and a
    /// table of retained request bodies is a data-protection liability that buys nothing — the only
    /// question ever asked of it is "is this the same request?", which a digest answers.
    /// <para>
    /// UTF-8 bytes of the raw body, not of a re-serialised object: the comparison must be about what
    /// the client actually sent. Two byte-identical requests hash identically; anything else is a
    /// different request and must be refused.
    /// </para>
    /// </remarks>
    public static string ComputeHash(string? body)
    {
        var bytes = Encoding.UTF8.GetBytes(body ?? string.Empty);
        return Convert.ToHexStringLower(SHA256.HashData(bytes));
    }
}
