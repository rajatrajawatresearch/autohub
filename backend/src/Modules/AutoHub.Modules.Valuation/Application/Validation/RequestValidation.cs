using AutoHub.SharedKernel.Results;
using FluentValidation.Results;

namespace AutoHub.Modules.Valuation.Application.Validation;

/// <summary>
/// Turns a FluentValidation result into the failure shape the API contract publishes.
/// </summary>
/// <remarks>
/// Handlers validate rather than a MediatR pipeline behaviour: a behaviour throws, and an expected
/// failure in this codebase travels as a <see cref="Result"/>. Declared per module rather than
/// shared, so the module stays extractable without dragging a common assembly with it.
/// </remarks>
internal static class RequestValidation
{
    public static Error ToError(ValidationResult validation)
    {
        var fieldErrors = new Dictionary<string, object?>(StringComparer.Ordinal);

        foreach (var failure in validation.Errors)
        {
            if (!fieldErrors.TryGetValue(failure.PropertyName, out var existing)
                || existing is not List<string> codes)
            {
                codes = [];
                fieldErrors[failure.PropertyName] = codes;
            }

            codes.Add(string.IsNullOrWhiteSpace(failure.ErrorCode) ? failure.PropertyName : failure.ErrorCode);
        }

        return Error.Validation(
            "valuation.request_invalid",
            "Some of the submitted values could not be accepted.")
            .With("fieldErrors", fieldErrors);
    }
}
