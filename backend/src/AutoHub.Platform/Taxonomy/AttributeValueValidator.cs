using System.Globalization;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.RegularExpressions;
using AutoHub.SharedKernel.Money;
using AutoHub.SharedKernel.Results;

namespace AutoHub.Platform.Taxonomy;

/// <summary>
/// Checks a submitted value against the attribute it claims to be.
/// </summary>
/// <remarks>
/// One validator, two callers, and that is the point. The sell form validates a single field as the
/// seller types; the dealer bulk upload validates ten thousand rows at once. If those ran different
/// code, a CSV would eventually create listings the sell form would have rejected, and the fix would
/// arrive after the listings did.
/// <para>
/// It is deliberately <b>not</b> a duplicate of the form schema registry's rules. The form registry
/// decides which fields appear and what the copy says; this decides what the domain will accept. A
/// value that reaches the API through an integration, a migration or a partner feed never passes a
/// form, and it still must not be allowed to store 900,000 km on a two-year-old car.
/// </para>
/// </remarks>
public static class AttributeValueValidator
{
    /// <summary>
    /// Untrusted patterns get a hard ceiling on match time.
    /// </summary>
    /// <remarks>
    /// The pattern is authored in the admin panel, so it is user input to the regex engine. Something
    /// as ordinary-looking as <c>(a+)+$</c> backtracks exponentially, and without a timeout a single
    /// bulk upload row would pin a request thread until the process was recycled. A hundred
    /// milliseconds is several orders of magnitude more than any legitimate attribute pattern needs.
    /// </remarks>
    private static readonly TimeSpan PatternTimeout = TimeSpan.FromMilliseconds(100);

    /// <summary>
    /// Validates one value. <paramref name="raw"/> is null or <c>JsonNull</c> when the field was
    /// left blank, which is a failure only if the attribute is required for this category.
    /// </summary>
    public static Result<TypedAttributeValue?> Validate(EffectiveAttribute attribute, JsonNode? raw)
    {
        var definition = attribute.Definition;

        if (raw is null || raw.GetValueKind() == JsonValueKind.Null)
        {
            if (attribute.IsRequired)
            {
                return Result.Failure<TypedAttributeValue?>(Required(definition));
            }

            return Result.Success<TypedAttributeValue?>(null);
        }

        Result<TypedAttributeValue> parsed = definition.DataType switch
        {
            AttributeDataType.String => ValidateString(definition, raw),
            AttributeDataType.Number => ValidateNumber(definition, raw, wholeOnly: false),
            AttributeDataType.Integer => ValidateNumber(definition, raw, wholeOnly: true),
            AttributeDataType.Boolean => ValidateBoolean(definition, raw),
            AttributeDataType.Enum => ValidateEnum(definition, raw),
            AttributeDataType.MultiEnum => ValidateMultiEnum(definition, raw),
            AttributeDataType.Date => ValidateDate(definition, raw),
            AttributeDataType.Money => ValidateMoney(definition, raw),
            _ => Result.Failure<TypedAttributeValue>(Error.Unexpected(
                "attribute.data_type_unhandled",
                $"Attribute '{definition.Code}' has a data type this build cannot validate.")),
        };

        return parsed.IsSuccess
            ? Result.Success<TypedAttributeValue?>(parsed.Value)
            : Result.Failure<TypedAttributeValue?>(parsed.Error);
    }

    /// <summary>
    /// Validates a whole payload against a category's effective attributes, collecting every error.
    /// </summary>
    /// <remarks>
    /// Returning the first failure is what makes a dealer fix a four-hundred-row spreadsheet one
    /// round trip at a time. Every error is gathered instead, keyed by attribute code, matching the
    /// <c>metadata.fieldErrors</c> shape the API contract already publishes so the same response
    /// renders under a field on the web form and in a row of the bulk-upload error table.
    /// <para>
    /// Keys present in the payload but absent from the category are ignored rather than rejected. A
    /// dealer's export contains columns we do not model, and refusing the whole file over an extra
    /// column would be a support ticket rather than a data-quality win.
    /// </para>
    /// </remarks>
    public static Result<IReadOnlyDictionary<string, TypedAttributeValue>> ValidateAll(
        IReadOnlyList<EffectiveAttribute> attributes,
        JsonObject payload)
    {
        var values = new Dictionary<string, TypedAttributeValue>(StringComparer.Ordinal);
        var fieldErrors = new Dictionary<string, object?>(StringComparer.Ordinal);

        foreach (var attribute in attributes)
        {
            var raw = payload.TryGetPropertyValue(attribute.Code, out var node) ? node : null;

            var outcome = Validate(attribute, raw);

            if (outcome.IsFailure)
            {
                fieldErrors[attribute.Code] = new List<string> { outcome.Error.Code };
                continue;
            }

            if (outcome.Value is { } value)
            {
                values[attribute.Code] = value;
            }
        }

        return fieldErrors.Count == 0
            ? Result.Success<IReadOnlyDictionary<string, TypedAttributeValue>>(values)
            : Error.Validation("attribute.values_invalid", "Some values could not be accepted.")
                .With("fieldErrors", fieldErrors);
    }

    private static Result<TypedAttributeValue> ValidateString(AttributeDefinition definition, JsonNode raw)
    {
        if (raw is not JsonValue value || !value.TryGetValue<string>(out var text))
        {
            return TypeMismatch(definition, "text");
        }

        text = text.Trim();

        if (definition.MinLength is { } min && text.Length < min)
        {
            return Error.Validation(
                $"{definition.Code}.too_short",
                $"{definition.Label} must be at least {min} characters.")
                .With("minLength", min);
        }

        if (definition.MaxLength is { } max && text.Length > max)
        {
            return Error.Validation(
                $"{definition.Code}.too_long",
                $"{definition.Label} must be at most {max} characters.")
                .With("maxLength", max);
        }

        if (definition.Pattern is { } pattern && !IsPatternMatch(pattern, text))
        {
            return Error.Validation(
                $"{definition.Code}.malformed",
                $"{definition.Label} is not in the expected format.");
        }

        return TypedAttributeValue.OfText(text);
    }

    private static Result<TypedAttributeValue> ValidateNumber(
        AttributeDefinition definition,
        JsonNode raw,
        bool wholeOnly)
    {
        if (!TryReadDecimal(raw, out var number))
        {
            return TypeMismatch(definition, wholeOnly ? "a whole number" : "a number");
        }

        if (wholeOnly && decimal.Truncate(number) != number)
        {
            return Error.Validation(
                $"{definition.Code}.not_whole",
                $"{definition.Label} must be a whole number.");
        }

        var bounded = CheckBounds(definition, number);
        if (bounded.IsFailure)
        {
            return Result.Failure<TypedAttributeValue>(bounded.Error);
        }

        return wholeOnly
            ? TypedAttributeValue.OfInteger((long)number)
            : TypedAttributeValue.OfNumber(number);
    }

    private static Result<TypedAttributeValue> ValidateBoolean(AttributeDefinition definition, JsonNode raw)
    {
        if (raw is JsonValue value && value.TryGetValue<bool>(out var flag))
        {
            return TypedAttributeValue.OfBoolean(flag);
        }

        return TypeMismatch(definition, "true or false");
    }

    private static Result<TypedAttributeValue> ValidateEnum(AttributeDefinition definition, JsonNode raw)
    {
        if (raw is not JsonValue value || !value.TryGetValue<string>(out var choice))
        {
            return TypeMismatch(definition, "one of the listed options");
        }

        var option = definition.FindOption(choice.Trim());

        if (option is null || !option.IsActive)
        {
            return NotAnOption(definition, choice);
        }

        return TypedAttributeValue.OfEnum(option.Value);
    }

    private static Result<TypedAttributeValue> ValidateMultiEnum(AttributeDefinition definition, JsonNode raw)
    {
        if (raw is not JsonArray array)
        {
            return TypeMismatch(definition, "a list of options");
        }

        // Ordinal set semantics, and order preserved: the order a seller ticked features in is the
        // order they read back on the spec sheet, and re-sorting it would look like an edit.
        var chosen = new List<string>(array.Count);
        var seen = new HashSet<string>(StringComparer.Ordinal);

        foreach (var element in array)
        {
            if (element is not JsonValue value || !value.TryGetValue<string>(out var choice))
            {
                return TypeMismatch(definition, "a list of option values");
            }

            var option = definition.FindOption(choice.Trim());

            if (option is null || !option.IsActive)
            {
                return NotAnOption(definition, choice);
            }

            if (seen.Add(option.Value))
            {
                chosen.Add(option.Value);
            }
        }

        // For a multi-select the numeric bounds count selections — the only quantity it has.
        if (definition.MinNumber is { } min && chosen.Count < min)
        {
            return Error.Validation(
                $"{definition.Code}.too_few",
                $"Choose at least {min} for {definition.Label}.")
                .With("min", min);
        }

        if (definition.MaxNumber is { } max && chosen.Count > max)
        {
            return Error.Validation(
                $"{definition.Code}.too_many",
                $"Choose at most {max} for {definition.Label}.")
                .With("max", max);
        }

        return TypedAttributeValue.OfChoices(chosen);
    }

    private static Result<TypedAttributeValue> ValidateDate(AttributeDefinition definition, JsonNode raw)
    {
        if (raw is not JsonValue value || !value.TryGetValue<string>(out var text))
        {
            return TypeMismatch(definition, "a date as yyyy-MM-dd");
        }

        // Invariant culture and one exact format. Accepting "03/04/2026" would mean guessing between
        // the third of April and the fourth of March, and an insurance expiry guessed wrong is a
        // policy that lapses without warning anybody.
        if (!DateOnly.TryParseExact(text.Trim(), "yyyy-MM-dd", CultureInfo.InvariantCulture,
                DateTimeStyles.None, out var date))
        {
            return TypeMismatch(definition, "a date as yyyy-MM-dd");
        }

        return TypedAttributeValue.OfDate(date);
    }

    /// <summary>
    /// Accepts either a bare integer of <b>minor units</b> or <c>{ "minorUnits": n, "currency": "INR" }</c>.
    /// </summary>
    /// <remarks>
    /// A bare number is paise, never rupees. This is the one place in the registry where the wrong
    /// reading is off by a factor of a hundred and looks entirely plausible on the way past, so the
    /// rule is stated rather than inferred and there is no "amount in rupees" form to get it wrong.
    /// </remarks>
    private static Result<TypedAttributeValue> ValidateMoney(AttributeDefinition definition, JsonNode raw)
    {
        JsonNode? amountNode;

        // The platform's declared base currency. Stated rather than assumed, so the day a second
        // currency exists this is the one line that has to change.
        var currencyCode = Currency.Inr.Code;

        if (raw is JsonObject amountObject)
        {
            if (!amountObject.TryGetPropertyValue("minorUnits", out amountNode) || amountNode is null)
            {
                return TypeMismatch(definition, "an amount in minor units");
            }

            if (amountObject.TryGetPropertyValue("currency", out var currencyNode)
                && currencyNode is JsonValue currencyValue
                && currencyValue.TryGetValue<string>(out var code))
            {
                currencyCode = code;
            }
        }
        else
        {
            amountNode = raw;
        }

        if (!TryReadDecimal(amountNode, out var amount))
        {
            return TypeMismatch(definition, "an amount in minor units");
        }

        // Minor units are whole by definition. A fractional paisa is either a rupee figure somebody
        // forgot to scale or a rounding somebody skipped, and both are worth refusing loudly.
        if (decimal.Truncate(amount) != amount)
        {
            return Error.Validation(
                $"{definition.Code}.not_whole",
                $"{definition.Label} must be a whole number of minor units.");
        }

        if (amount is < long.MinValue or > long.MaxValue)
        {
            return Error.Validation(
                $"{definition.Code}.too_high",
                $"{definition.Label} is larger than any amount this platform can represent.");
        }

        var minorUnits = (long)amount;

        Currency currency;
        try
        {
            currency = Currency.FromCode(currencyCode);
        }
        catch (ArgumentOutOfRangeException)
        {
            return Error.Validation(
                $"{definition.Code}.currency_unsupported",
                $"'{currencyCode}' is not a currency this platform accepts.")
                .With("currency", currencyCode);
        }

        var bounded = CheckBounds(definition, minorUnits);
        if (bounded.IsFailure)
        {
            return Result.Failure<TypedAttributeValue>(bounded.Error);
        }

        return TypedAttributeValue.OfMoney(Money.FromMinorUnits(minorUnits, currency));
    }

    private static Result CheckBounds(AttributeDefinition definition, decimal number)
    {
        if (definition.MinNumber is { } min && number < min)
        {
            return Error.Validation(
                $"{definition.Code}.too_low",
                $"{definition.Label} cannot be below {min}.")
                .With("min", min);
        }

        if (definition.MaxNumber is { } max && number > max)
        {
            return Error.Validation(
                $"{definition.Code}.too_high",
                $"{definition.Label} cannot be above {max}.")
                .With("max", max);
        }

        if (definition.Step is { } step && step > 0)
        {
            // Steps are measured from the floor when there is one, so a range of 1,000–500,000 in
            // steps of 1,000 accepts 1,000 rather than demanding a multiple of 1,000 from zero.
            var offset = number - (definition.MinNumber ?? 0m);

            if (offset % step != 0m)
            {
                return Error.Validation(
                    $"{definition.Code}.step_mismatch",
                    $"{definition.Label} must change in steps of {step}.")
                    .With("step", step);
            }
        }

        return Result.Success();
    }

    private static bool IsPatternMatch(string pattern, string input)
    {
        try
        {
            // The static overload uses the framework's own compiled-pattern cache, so a bulk upload
            // of ten thousand rows compiles each pattern once rather than once per row.
            return Regex.IsMatch(input, pattern, RegexOptions.CultureInvariant, PatternTimeout);
        }
        catch (RegexMatchTimeoutException)
        {
            return false;
        }
        catch (ArgumentException)
        {
            // The pattern itself is not a valid regex. Refusing the value is the safe direction: the
            // alternative is accepting everything under a rule that never actually ran.
            return false;
        }
    }

    /// <summary>
    /// Reads a number out of a JSON node, whichever way that node was built.
    /// </summary>
    /// <remarks>
    /// A <see cref="JsonValue"/> is backed either by a <c>JsonElement</c> — the shape every parsed
    /// request body has — or by a boxed CLR number, which is what <c>JsonValue.Create(38400)</c>
    /// produces. On the second, <c>TryGetValue</c> is an exact type match and no more, so asking a
    /// <c>JsonValue&lt;int&gt;</c> for a <c>decimal</c> answers <c>false</c>.
    /// <para>
    /// That difference is invisible until it is not. The sell form posts JSON and works; a bulk
    /// importer, a seeder or a test that builds nodes in code hits the same validator and every
    /// number is rejected as the wrong type. Widening through the concrete numeric types is what
    /// makes a hand-built node and a parsed one behave identically, which they must.
    /// </para>
    /// </remarks>
    private static bool TryReadDecimal(JsonNode raw, out decimal number)
    {
        number = 0m;

        if (raw is not JsonValue value)
        {
            return false;
        }

        if (value.TryGetValue<decimal>(out number))
        {
            return true;
        }

        if (value.TryGetValue<long>(out var integral))
        {
            number = integral;
            return true;
        }

        if (value.TryGetValue<int>(out var small))
        {
            number = small;
            return true;
        }

        if (value.TryGetValue<double>(out var floating))
        {
            // A double outside decimal's range would throw on cast. An odometer reading that large
            // is not a rounding question, it is a rejection.
            if (double.IsNaN(floating) || double.IsInfinity(floating)
                || floating is < (double)decimal.MinValue or > (double)decimal.MaxValue)
            {
                return false;
            }

            number = (decimal)floating;
            return true;
        }

        // Numbers arrive as strings from CSV uploads and from clients that quote every field.
        // Invariant culture only: "1,50,000" is a valid Indian rendering and an invalid number.
        return value.TryGetValue<string>(out var text)
               && decimal.TryParse(text, NumberStyles.Number, CultureInfo.InvariantCulture, out number);
    }

    private static Error Required(AttributeDefinition definition)
        => Error.Validation($"{definition.Code}.required", $"{definition.Label} is required.");

    private static Error TypeMismatch(AttributeDefinition definition, string expected)
        => Error.Validation(
            $"{definition.Code}.invalid_type",
            $"{definition.Label} must be {expected}.")
            .With("dataType", definition.DataType.ToWireName());

    private static Error NotAnOption(AttributeDefinition definition, string submitted)
        => Error.Validation(
            $"{definition.Code}.not_an_option",
            $"'{submitted}' is not an available option for {definition.Label}.")
            .With("submitted", submitted);
}
