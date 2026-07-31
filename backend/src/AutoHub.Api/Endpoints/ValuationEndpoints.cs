using AutoHub.Api.Http;
using AutoHub.Api.Middleware;
using AutoHub.Modules.Valuation.Application.RequestValuation;
using AutoHub.SharedKernel.Vehicles;
using MediatR;

namespace AutoHub.Api.Endpoints;

/// <summary>
/// §10 of the contract — <c>POST /valuation/quotes</c>, the homepage hero.
/// </summary>
/// <remarks>
/// Anonymous, and that is the product decision the endpoint exists to protect: asking for a phone
/// number before showing a number is why users bounce, so the first quote costs nothing and the
/// module declares no permission for it. Everything that follows — saving the quote to an account,
/// turning it into a listing — is authenticated, because by then the user has a reason to sign in.
/// <para>
/// The <c>Idempotency-Key</c> is <b>required</b> rather than exempt, unlike the pricing quote next
/// door, and the difference is that this one writes. A valuation is recorded immutably so that the
/// number a seller screenshots and the number support can look up are provably the same row; a
/// double-submitted hero form without a key would write two rows with two model runs behind them
/// and hand the seller whichever arrived second.
/// </para>
/// </remarks>
internal static class ValuationEndpoints
{
    public static IEndpointRouteBuilder MapValuationEndpoints(this IEndpointRouteBuilder app)
    {
        var valuation = app.MapGroup("/valuation").WithTags("Valuation");

        valuation.MapPost("/quotes", async (
                ValuationQuoteBody body,
                ISender sender,
                HttpContext context,
                CancellationToken ct) =>
            {
                var quote = await sender.Send(
                    new RequestValuationCommand
                    {
                        RegistrationNumber = body.RegistrationNumber,
                        KilometresDriven = body.KmsDriven,
                        City = body.City,
                        Condition = body.Condition,
                        CategoryCode = body.CategoryCode,
                        MakeCode = body.MakeCode,
                        ModelCode = body.ModelCode,
                        VariantCode = body.VariantCode,
                        ManufactureYear = body.Year,

                        // The routing affinity key, so a seller who refreshes a slow page lands on
                        // the model that answered the first attempt rather than on either side of a
                        // canary split — two different prices for one car destroys the trust the
                        // number exists to build.
                        IdempotencyKey = context.Request.Headers[IdempotencyMiddleware.HeaderName]
                            .ToString() is { Length: > 0 } key ? key : null,
                    },
                    ct);

                return quote.IsSuccess
                    ? Results.Ok(ValuationQuoteResponse.From(quote.Value))
                    : ApiProblem.Result(quote.Error, context);
            })
            .AllowAnonymous()
            .WithName("CreateValuationQuote")
            .WithSummary("Prices one vehicle. No auth — the first quote is the top of the funnel.");

        return app;
    }
}

/// <summary>The request body of <c>POST /valuation/quotes</c>.</summary>
/// <remarks>
/// <c>kmsDriven</c> and <c>year</c> are spelled as the contract spells them rather than as the
/// command's properties are named. The wire is the seam the frontend was built against and the
/// command is internal vocabulary; translating here is what lets either be renamed without the other
/// noticing.
/// </remarks>
public sealed record ValuationQuoteBody
{
    public string? RegistrationNumber { get; init; }

    public long? KmsDriven { get; init; }

    public string? City { get; init; }

    public string? Condition { get; init; }

    /// <summary>Supplying it lets a plate we have never seen become a canonical vehicle.</summary>
    public string? CategoryCode { get; init; }

    public string? MakeCode { get; init; }

    public string? ModelCode { get; init; }

    public string? VariantCode { get; init; }

    public int? Year { get; init; }
}

/// <summary>
/// The §10 response shape.
/// </summary>
/// <remarks>
/// Distinct from the module's own <see cref="ValuationQuote"/> for one substantive reason:
/// <c>registration</c> is an object on the wire — <c>{ canonical, display }</c> — and a flat display
/// string in the module. The frontend's <c>PlateBlock</c> renders the grouped form and its
/// deduplication compares the canonical one, so collapsing them to a single string would force the
/// client to re-derive a plate format that includes the Bharat-series rule.
/// </remarks>
public sealed record ValuationQuoteResponse
{
    public required Guid QuoteId { get; init; }

    public required ValuationVehicleResponse Vehicle { get; init; }

    public required string Currency { get; init; }

    public required IReadOnlyList<QuotedValuation> Valuations { get; init; }

    public required QuotedRange Range { get; init; }

    public required QuotedConfidence Confidence { get; init; }

    public required IReadOnlyList<QuotedForecastPoint> Forecast { get; init; }

    public required string AsOf { get; init; }

    public required DateTimeOffset ExpiresAt { get; init; }

    public required string ModelVersion { get; init; }

    public static ValuationQuoteResponse From(ValuationQuote quote) => new()
    {
        QuoteId = quote.QuoteId,
        Vehicle = ValuationVehicleResponse.From(quote.Vehicle),
        Currency = quote.Currency,
        Valuations = quote.Valuations,
        Range = quote.Range,
        Confidence = quote.Confidence,
        Forecast = quote.Forecast,
        AsOf = quote.AsOf,
        ExpiresAt = quote.ExpiresAt,
        ModelVersion = quote.ModelVersion,
    };
}

/// <summary>The vehicle block, with the plate in the shape the design system renders.</summary>
public sealed record ValuationVehicleResponse
{
    public Guid? VehicleId { get; init; }

    public required string Title { get; init; }

    public PlateResponse? Registration { get; init; }

    public int? Year { get; init; }

    public string? Fuel { get; init; }

    /// <summary>Provenance — <c>vahan</c>, <c>rc_ocr</c>, <c>seller</c>. Changes how far to trust it.</summary>
    public string? Source { get; init; }

    public static ValuationVehicleResponse From(QuotedVehicle vehicle) => new()
    {
        VehicleId = vehicle.VehicleId,
        Title = vehicle.Title,
        Registration = PlateResponse.From(vehicle.RegistrationDisplay),
        Year = vehicle.Year,
        Fuel = vehicle.Fuel,
        Source = vehicle.Source,
    };
}

/// <summary>A registration number in both the form we store and the form a plate is read in.</summary>
public sealed record PlateResponse(string Canonical, string Display)
{
    /// <summary>
    /// Rebuilds both forms from whichever one the module carried.
    /// </summary>
    /// <remarks>
    /// Re-parsed rather than assumed, because a plate that will not parse still has to render — the
    /// vehicle exists and the seller typed something. Falling back to the raw value keeps the block
    /// on screen instead of blanking the one field the seller uses to recognise their own car.
    /// </remarks>
    public static PlateResponse? From(string? display)
    {
        if (string.IsNullOrWhiteSpace(display))
        {
            return null;
        }

        return RegistrationNumber.TryParse(display) is { IsSuccess: true } parsed
            ? new PlateResponse(parsed.Value.Canonical, parsed.Value.ToPlateDisplay())
            : new PlateResponse(display, display);
    }
}
