using AutoHub.SharedKernel.Results;
using FluentValidation.Results;

namespace AutoHub.Modules.Listings.Application.Validation;

/// <summary>
/// Turns a FluentValidation result into the failure shape the API contract publishes.
/// </summary>
/// <remarks>
/// Handlers validate rather than a MediatR pipeline behaviour, for two reasons. A behaviour throws,
/// and this codebase's rule is that an expected failure travels as a <see cref="Result"/> — a
/// validation failure on a sell form is the most expected failure in the product. And a behaviour
/// lives in whichever assembly registers the pipeline, which would put part of this module's request
/// handling outside the module and make extraction a rewrite.
/// <para>
/// Codes, never sentences: the sentence comes from the localisation registry and differs per locale.
/// </para>
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
            "listing.request_invalid",
            "Some of the submitted values could not be accepted.")
            .With("fieldErrors", fieldErrors);
    }
}
