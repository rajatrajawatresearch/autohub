using System.Globalization;
using System.Text.Json.Nodes;
using AutoHub.SharedKernel.Money;

namespace AutoHub.Platform.Taxonomy;

/// <summary>
/// A validated attribute value, carried in the shape its type actually has.
/// </summary>
/// <remarks>
/// This is the handoff between validation and storage, and it exists so the coercion happens exactly
/// once. Without it, <see cref="AttributeValueValidator"/> would prove that "38400" is a valid
/// integer and then hand the caller the string back, leaving every consumer to re-parse it — and one
/// of them would parse it under a culture where the decimal separator is a comma.
/// <para>
/// Money is a <see cref="SharedKernel.Money.Money"/>, never a number. An attribute called
/// <c>insured_declared_value</c> holds paise, and the only way to guarantee nobody reads it as rupees
/// is to never let it exist as a bare number in the first place.
/// </para>
/// </remarks>
public sealed record TypedAttributeValue
{
    private TypedAttributeValue(AttributeDataType dataType) => DataType = dataType;

    public AttributeDataType DataType { get; }

    /// <summary>Set for <see cref="AttributeDataType.String"/> and <see cref="AttributeDataType.Enum"/>.</summary>
    public string? Text { get; private init; }

    /// <summary>Set for <see cref="AttributeDataType.Number"/> and <see cref="AttributeDataType.Integer"/>.</summary>
    public decimal? Number { get; private init; }

    public bool? Flag { get; private init; }

    public DateOnly? Date { get; private init; }

    public Money? Amount { get; private init; }

    /// <summary>Set for <see cref="AttributeDataType.MultiEnum"/>. Ordered as authored, deduplicated.</summary>
    public IReadOnlyList<string> Choices { get; private init; } = [];

    public static TypedAttributeValue OfText(string value)
        => new(AttributeDataType.String) { Text = value };

    public static TypedAttributeValue OfEnum(string value)
        => new(AttributeDataType.Enum) { Text = value };

    public static TypedAttributeValue OfNumber(decimal value)
        => new(AttributeDataType.Number) { Number = value };

    public static TypedAttributeValue OfInteger(long value)
        => new(AttributeDataType.Integer) { Number = value };

    public static TypedAttributeValue OfBoolean(bool value)
        => new(AttributeDataType.Boolean) { Flag = value };

    public static TypedAttributeValue OfDate(DateOnly value)
        => new(AttributeDataType.Date) { Date = value };

    public static TypedAttributeValue OfMoney(Money value)
        => new(AttributeDataType.Money) { Amount = value };

    public static TypedAttributeValue OfChoices(IReadOnlyList<string> values)
        => new(AttributeDataType.MultiEnum) { Choices = values };

    /// <summary>
    /// The JSON form written into the denormalised attribute bag and shipped to clients.
    /// </summary>
    /// <remarks>
    /// Money serialises as its minor units, not as a formatted string and not as rupees, matching
    /// <c>amountMinor</c> everywhere else in the API contract. Dates serialise as <c>yyyy-MM-dd</c>
    /// under the invariant culture, because a date rendered under the server's culture is a bug that
    /// only reproduces on the machine that has the wrong culture.
    /// </remarks>
    public JsonNode? ToJson() => DataType switch
    {
        AttributeDataType.String or AttributeDataType.Enum => Text is null ? null : JsonValue.Create(Text),
        AttributeDataType.Number => Number is null ? null : JsonValue.Create(Number.Value),
        AttributeDataType.Integer => Number is null ? null : JsonValue.Create((long)Number.Value),
        AttributeDataType.Boolean => Flag is null ? null : JsonValue.Create(Flag.Value),
        AttributeDataType.Date => Date is null
            ? null
            : JsonValue.Create(Date.Value.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture)),
        AttributeDataType.Money => Amount is null ? null : JsonValue.Create(Amount.Value.MinorUnits),
        AttributeDataType.MultiEnum => new JsonArray(Choices.Select(c => (JsonNode?)JsonValue.Create(c)).ToArray()),
        _ => null,
    };
}
