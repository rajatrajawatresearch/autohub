using AutoHub.SharedKernel.Results;

namespace AutoHub.Persistence.Seed;

/// <summary>
/// Turns a failed <see cref="Result"/> from authored seed data into an exception.
/// </summary>
/// <remarks>
/// This is the one place in the codebase where throwing on a <c>Result</c> is the right answer.
/// Everywhere else a failure is a user's input being wrong — Tuesday, not exceptional. Here the
/// input is a constant a developer typed, so a failure means the seed table itself is malformed:
/// a category code with a space in it, a fee rate written as <c>18</c> instead of <c>0.18m</c>, a
/// workflow with two initial states. None of that is recoverable at runtime and all of it must stop
/// the process immediately, loudly, with the offending item named — because the alternative is a
/// half-seeded database that looks fine until the demo asks it a question.
/// </remarks>
internal static class SeedGuard
{
    public static T Required<T>(Result<T> result, string what)
        => result.IsSuccess
            ? result.Value
            : throw new InvalidOperationException($"Seed data for '{what}' is invalid — {result.Error}");

    public static void Required(Result result, string what)
    {
        if (result.IsFailure)
        {
            throw new InvalidOperationException($"Seed data for '{what}' is invalid — {result.Error}");
        }
    }
}
