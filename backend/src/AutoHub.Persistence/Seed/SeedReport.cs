namespace AutoHub.Persistence.Seed;

/// <summary>
/// What one seeder did, so a developer can tell "already seeded" from "seeded nothing".
/// </summary>
/// <remarks>
/// The distinction matters more than it sounds. An idempotent seeder that silently does nothing
/// looks identical, in a log, to one whose natural-key lookup is broken and is skipping rows it
/// should have written. Counting both halves makes the second case visible on the first run after
/// it breaks rather than on the day the demo is given.
/// </remarks>
public sealed record SeedStepResult(string Registry, int Created, int Skipped)
{
    public static SeedStepResult Empty(string registry) => new(registry, 0, 0);

    public override string ToString() => $"{Registry}: +{Created} new, {Skipped} already present";
}

public sealed record SeedReport(IReadOnlyList<SeedStepResult> Steps)
{
    public int TotalCreated => Steps.Sum(step => step.Created);

    public bool WasAlreadySeeded => TotalCreated == 0;
}
