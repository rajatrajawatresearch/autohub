namespace AutoHub.SharedKernel.Money;

/// <summary>
/// An ISO 4217 currency and its minor-unit scale.
/// </summary>
/// <remarks>
/// AutoHub is INR-only today. This type exists anyway because the PRD's roadmap has an
/// export/import vehicle marketplace on it, and retrofitting currency into a ledger that assumed
/// one is a migration nobody survives cheerfully. Carrying the code costs one column now.
/// </remarks>
public readonly record struct Currency
{
    private Currency(string code, int decimals)
    {
        Code = code;
        Decimals = decimals;
    }

    public string Code { get; }

    public int Decimals { get; }

    public int MinorUnitsPerMajor => (int)Math.Pow(10, Decimals);

    public static Currency Inr { get; } = new("INR", 2);

    public static Currency Usd { get; } = new("USD", 2);

    public static Currency Aed { get; } = new("AED", 2);

    public static Currency FromCode(string code) => code.ToUpperInvariant() switch
    {
        "INR" => Inr,
        "USD" => Usd,
        "AED" => Aed,
        _ => throw new ArgumentOutOfRangeException(nameof(code), code, "Unsupported currency."),
    };

    public override string ToString() => Code;
}
