namespace AutoHub.Api.Middleware;

/// <summary>
/// Marks a write-shaped endpoint that changes nothing, so it is not asked for an idempotency key.
/// </summary>
/// <remarks>
/// A handful of endpoints are <c>POST</c> only because their input does not fit in a query string —
/// <c>/platform/forms/{key}/validate</c> and <c>/platform/pricing/quote</c> both take a JSON
/// document and both are pure functions of it. Running them through the idempotency ledger would
/// write a row to a hot table on every keystroke of a form the user is still filling in, to protect
/// against a duplicate that has no effect to duplicate.
/// <para>
/// It is an opt-out and not an opt-in, deliberately. The default has to be "protected", because the
/// failure mode of forgetting the attribute is a wasted database row, and the failure mode of
/// forgetting to opt in would be a duplicated bid.
/// </para>
/// </remarks>
[AttributeUsage(AttributeTargets.Method | AttributeTargets.Class)]
public sealed class IdempotencyExemptAttribute : Attribute;
