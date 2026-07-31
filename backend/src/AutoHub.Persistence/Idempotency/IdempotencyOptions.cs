namespace AutoHub.Persistence.Idempotency;

public sealed class IdempotencyOptions
{
    public const string SectionName = "Idempotency";

    /// <summary>
    /// How long a key's response stays replayable.
    /// </summary>
    /// <remarks>
    /// Long enough to cover every retry that will actually happen — an HTTP client's own retry, a
    /// mobile app resuming from background, a person tapping the button again — and short enough
    /// that the table stays small enough for its primary-key index to sit in memory, which is the
    /// only reason the lookup in front of every write is free.
    /// </remarks>
    public TimeSpan Retention { get; set; } = TimeSpan.FromHours(24);
}
