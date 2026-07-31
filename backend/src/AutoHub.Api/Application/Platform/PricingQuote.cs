using System.Text.Json.Nodes;
using AutoHub.Platform.Pricing;
using AutoHub.SharedKernel.Money;
using AutoHub.SharedKernel.Results;
using FluentValidation;
using MediatR;

namespace AutoHub.Api.Application.Platform;

/// <summary>
/// <c>POST /platform/pricing/quote</c> — §4 of the contract.
/// </summary>
/// <remarks>
/// <see cref="Context"/> is an untyped <see cref="JsonObject"/> on purpose, and it is the same
/// decision the pricing engine itself makes. A typed context would mean that "charge Pune dealers
/// differently once they have completed ten sales" is a new property, a new migration and a new
/// deploy — which is precisely the hardcoding the registry exists to remove. The engine matches
/// rules against whatever facts arrive; a rule referencing a fact the caller did not send simply
/// does not match.
/// <para>
/// <see cref="Amount"/> is separate from the context because callers place it in both spots. §4 of
/// the contract nests it under <c>context</c>, next to the vehicle and the party, and reads
/// naturally that way — the amount is one more fact a rule may band on. Callers who think of the
/// amount as the subject of the request rather than a fact about it put it at the top level, and
/// both readings are defensible enough that rejecting either would be a 422 that teaches a client
/// nothing. Accepted in both positions, normalised to one in the handler.
/// </para>
/// </remarks>
internal sealed record GetPriceQuoteCommand(string Scope, JsonObject? Context, JsonObject? Amount)
    : IRequest<Result<PriceQuoteResponse>>;

public sealed record PriceQuoteResponse(
    string Currency,
    long BaseAmount,
    IReadOnlyList<PriceQuoteLineResponse> Lines,
    long TotalFeesMinor,
    long PayableMinor,
    string? Explanation,
    string? AppliedRuleCode,
    int RuleSetVersion);

public sealed record PriceQuoteLineResponse(
    string Code,
    string Label,
    string Basis,
    decimal? Rate,
    long AmountMinor,
    IReadOnlyList<string>? AppliesTo);

internal sealed class GetPriceQuoteValidator : AbstractValidator<GetPriceQuoteCommand>
{
    public GetPriceQuoteValidator()
        => RuleFor(command => command.Scope)
            .NotEmpty().WithErrorCode("pricing.scope_required")
            .MaximumLength(64).WithErrorCode("pricing.scope_invalid");
}

internal sealed class GetPriceQuoteHandler : IRequestHandler<GetPriceQuoteCommand, Result<PriceQuoteResponse>>
{
    private readonly IPricingEngine _engine;

    public GetPriceQuoteHandler(IPricingEngine engine) => _engine = engine;

    public async Task<Result<PriceQuoteResponse>> Handle(
        GetPriceQuoteCommand command,
        CancellationToken cancellationToken)
    {
        var context = command.Context is { } supplied
            ? supplied.DeepClone().AsObject()
            : [];

        // Cloned before this, because writing the top-level amount into the caller's own node would
        // mutate the object the model binder handed us — harmless today, and the kind of shared-state
        // edit that stops being harmless the moment anything else reads the request body.
        if (command.Amount is { } amount)
        {
            context["amount"] = amount.DeepClone();
        }

        var baseAmount = ReadAmount(context);
        if (baseAmount.IsFailure)
        {
            return Result.Failure<PriceQuoteResponse>(baseAmount.Error);
        }

        var quote = await _engine.QuoteAsync(
            new PriceQuoteRequest
            {
                Scope = command.Scope,
                BaseAmount = baseAmount.Value,
                Context = context,
            },
            cancellationToken);

        if (quote.IsFailure)
        {
            return Result.Failure<PriceQuoteResponse>(quote.Error);
        }

        var result = quote.Value;

        return new PriceQuoteResponse(
            result.BaseAmount.Currency.Code,
            result.BaseAmount.MinorUnits,
            result.Lines
                .Select(line => new PriceQuoteLineResponse(
                    line.Code,
                    line.Label,
                    line.Basis.ToWireName(),
                    line.Rate,
                    line.Amount.MinorUnits,
                    line.AppliesTo.Count == 0 ? null : line.AppliesTo))
                .ToList(),
            result.TotalFees.MinorUnits,
            result.Payable.MinorUnits,
            result.Explanation,
            result.AppliedRuleCode,
            result.RuleSetVersion);
    }

    /// <summary>
    /// Reads the normalised <c>amount</c> as minor units.
    /// </summary>
    /// <remarks>
    /// Rejected rather than defaulted when absent. A quote against a zero base returns zero fees and
    /// looks like a successful answer, so a caller who forgot the amount would ship a screen that
    /// confidently tells a dealer they owe nothing.
    /// <para>
    /// Read as a <c>long</c> and never as a decimal of rupees. This is the boundary where a
    /// hundred-fold error enters a system, and the contract is explicit that every amount on the
    /// wire is <c>minorUnits</c>.
    /// </para>
    /// </remarks>
    private static Result<Money> ReadAmount(JsonObject context)
    {
        if (context["amount"] is not JsonObject amount)
        {
            return Error.Validation(
                "pricing.amount_required",
                "The quote context needs an 'amount' with minorUnits and currency.");
        }

        if (amount["minorUnits"] is not JsonValue value || !value.TryGetValue<long>(out var minorUnits))
        {
            return Error.Validation(
                "pricing.amount_invalid",
                "'amount.minorUnits' must be a whole number of minor units (paise for INR).");
        }

        var currencyCode = amount["currency"]?.GetValue<string>();

        try
        {
            var currency = string.IsNullOrWhiteSpace(currencyCode)
                ? Currency.Inr
                : Currency.FromCode(currencyCode);

            return Money.FromMinorUnits(minorUnits, currency);
        }
        catch (ArgumentOutOfRangeException)
        {
            return Error.Validation("pricing.currency_unsupported", $"'{currencyCode}' is not supported.")
                .With("currency", currencyCode);
        }
    }
}
