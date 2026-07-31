using AutoHub.SharedKernel.Results;
using FluentValidation;
using MediatR;

namespace AutoHub.Api.Application.Behaviours;

/// <summary>
/// Runs every registered validator for a request and returns the failures as a
/// <see cref="Result"/>.
/// </summary>
/// <remarks>
/// Returns, never throws — see <see cref="ResultShape"/> for why that distinction is load-bearing
/// rather than stylistic.
/// <para>
/// The failure it produces carries <c>fieldErrors</c> in the same shape as the form registry's
/// <c>422</c>: property name to a list of machine codes. One shape for both means a client has one
/// piece of code that attaches errors to fields, whether the rejection came from a request DTO's
/// validator or from a registry-driven form schema — which is the difference between "the form
/// highlights the bad field" and "the form shows a toast".
/// </para>
/// <para>
/// FluentValidation's <c>ErrorCode</c> is used as the code when the validator sets one, and the
/// property name is the fallback. A validator that forgets to set a code produces a usable but
/// unlocalisable error, which is a nudge rather than a failure.
/// </para>
/// </remarks>
internal sealed class ValidationBehaviour<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
    where TRequest : notnull
{
    private readonly List<IValidator<TRequest>> _validators;

    public ValidationBehaviour(IEnumerable<IValidator<TRequest>> validators)
        => _validators = validators.ToList();

    public async Task<TResponse> Handle(
        TRequest request,
        RequestHandlerDelegate<TResponse> next,
        CancellationToken cancellationToken)
    {
        if (_validators.Count == 0)
        {
            return await next();
        }

        var context = new ValidationContext<TRequest>(request);

        var failures = new List<FluentValidation.Results.ValidationFailure>();

        foreach (var validator in _validators)
        {
            var outcome = await validator.ValidateAsync(context, cancellationToken);
            failures.AddRange(outcome.Errors);
        }

        if (failures.Count == 0)
        {
            return await next();
        }

        var fieldErrors = failures
            .GroupBy(failure => failure.PropertyName, StringComparer.Ordinal)
            .ToDictionary(
                group => group.Key,
                group => group
                    .Select(failure => string.IsNullOrWhiteSpace(failure.ErrorCode)
                        ? failure.PropertyName
                        : failure.ErrorCode)
                    .Distinct(StringComparer.Ordinal)
                    .ToList(),
                StringComparer.Ordinal);

        var error = Error.Validation("request.validation_failed", "Some values need attention.")
            .With("fieldErrors", fieldErrors);

        // A handler whose response is not a Result cannot be failed politely. Rather than swallow
        // the request, let it run: the endpoint will reject it, and the mismatch is a wiring bug a
        // test will catch rather than a request that vanishes in production.
        return ResultShape.TryFailure<TResponse>(error, out var failure)
            ? failure
            : await next();
    }
}
