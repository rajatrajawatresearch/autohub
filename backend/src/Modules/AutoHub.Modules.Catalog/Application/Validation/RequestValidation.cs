using AutoHub.SharedKernel.Results;
using FluentValidation.Results;

namespace AutoHub.Modules.Catalog.Application.Validation;

/// <summary>
/// Turns a FluentValidation result into the failure shape the API contract publishes.
/// </summary>
/// <remarks>
/// <b>Why handlers validate rather than a MediatR pipeline behaviour.</b> A behaviour is the usual
/// answer and it is the wrong one here for two reasons. First, a behaviour throws — that is what
/// every published implementation does — and this codebase's rule is that an expected failure
/// travels as a <see cref="Result"/> so the compiler makes the caller acknowledge it; a validation
/// failure on a sell form is the most expected failure in the product. Second, a behaviour lives in
/// whichever assembly registers the pipeline, which would put a piece of every module's request
/// handling outside that module and make extraction a rewrite of the thing that was supposed to be
/// a build-file change.
/// <para>
/// The cost is three lines at the top of each handler. That is a cost worth paying to keep the
/// failure typed and the module self-contained.
/// </para>
/// <para>
/// Errors are grouped by field and carry <b>codes, never sentences</b>, matching
/// <c>422 metadata.fieldErrors</c> in §2 of the contract. The sentence is the localisation
/// registry's job and differs per locale; a message baked in here would be an English string a Tamil
/// user eventually reads.
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

            // ErrorCode falls back to FluentValidation's rule name when a validator forgot to set
            // one. Surfacing that rather than an empty string is what makes the omission findable.
            codes.Add(string.IsNullOrWhiteSpace(failure.ErrorCode) ? failure.PropertyName : failure.ErrorCode);
        }

        return Error.Validation(
            "catalog.request_invalid",
            "Some of the submitted values could not be accepted.")
            .With("fieldErrors", fieldErrors);
    }
}
