namespace AutoHub.Persistence.Outbox;

/// <summary>Knobs for the outbox processor. Every default here is a stated trade, not a guess.</summary>
public sealed class OutboxOptions
{
    public const string SectionName = "Outbox";

    /// <summary>
    /// How long the processor sleeps when it finds nothing.
    /// </summary>
    /// <remarks>
    /// This is the floor on end-to-end event latency in an idle system: a bid that triggers an
    /// outbid notification is delivered somewhere between zero and this. Two seconds keeps a
    /// notification feeling immediate while costing one trivial indexed query per node per two
    /// seconds — a rounding error next to the traffic that raised the event.
    /// </remarks>
    public TimeSpan PollInterval { get; set; } = TimeSpan.FromSeconds(2);

    /// <summary>
    /// Messages claimed per sweep.
    /// </summary>
    /// <remarks>
    /// Bounded because a claim holds row locks until the batch commits, and a batch large enough to
    /// take thirty seconds is a batch that blocks every other node for thirty seconds. Small
    /// batches, claimed often, keep the tail latency of one slow consumer off everybody else.
    /// </remarks>
    public int BatchSize { get; set; } = 100;

    /// <summary>First retry delay. Doubles per attempt up to <see cref="MaxRetryDelay"/>.</summary>
    public TimeSpan BaseRetryDelay { get; set; } = TimeSpan.FromSeconds(5);

    public TimeSpan MaxRetryDelay { get; set; } = TimeSpan.FromMinutes(30);

    /// <summary>
    /// Attempts before a message is left alone as poison.
    /// </summary>
    /// <remarks>
    /// A message that has failed this many times over the escalating backoff has been failing for
    /// hours, which means it is not a transient fault — it is a payload no consumer can process, and
    /// retrying it forever burns a worker and buries the healthy messages behind it in the log.
    /// Poison rows stay in the table, unprocessed and visible, because deleting the evidence of a bug
    /// is the one response that guarantees nobody fixes it.
    /// </remarks>
    public int MaxAttempts { get; set; } = 10;

    /// <summary>
    /// Turns the processor off without removing it. Set in tests and in one-off tooling, where a
    /// background service quietly draining the outbox makes assertions non-deterministic.
    /// </summary>
    public bool Enabled { get; set; } = true;
}
