using System.Globalization;
using System.Text.Json.Nodes;
using AutoHub.Modules.Catalog.Contracts;
using AutoHub.Modules.Valuation.Application.Abstractions;
using AutoHub.Modules.Valuation.Application.Validation;
using AutoHub.Platform.Taxonomy;
using AutoHub.SharedKernel.Money;
using AutoHub.SharedKernel.Results;
using AutoHub.SharedKernel.Time;
using AutoHub.SharedKernel.Vehicles;
using FluentValidation;
using MediatR;

namespace AutoHub.Modules.Valuation.Application.RequestValuation;

/// <summary>
/// Prices a vehicle and publishes the §10 response.
/// </summary>
/// <remarks>
/// <b>The order of operations is the design.</b> Identify the vehicle, gather everything already
/// known about it, let the router decide which model answers, store the result immutably, and only
/// then render. Rendering from the stored aggregate rather than from the prediction is deliberate:
/// the response a seller sees is provably the row that was written, so the quote they screenshot and
/// the quote support can look up are the same quote.
/// <para>
/// <b>Shadow predictions are stored and never returned.</b> They are the evidence a model promotion
/// is decided on, and they only exist if they were persisted at the time — a back-test cannot
/// reproduce the traffic mix that produced them.
/// </para>
/// <para>
/// <b>Nothing is fabricated when the inputs run out.</b> A plate we have never seen, with no category
/// to build one from, returns <c>valuation.insufficient_inputs</c>. A number invented for a vehicle
/// nobody can identify is the failure that costs a marketplace its credibility exactly once.
/// </para>
/// </remarks>
public sealed class RequestValuationHandler : IRequestHandler<RequestValuationCommand, Result<ValuationQuote>>
{
    private readonly IValuationRepository _valuations;
    private readonly IValuationModelRouter _router;
    private readonly IVehicleDirectory _vehicles;
    private readonly IVehicleTaxonomy _taxonomy;
    private readonly ITaxonomyRegistry _registry;
    private readonly IValidator<RequestValuationCommand> _validator;
    private readonly IClock _clock;

    public RequestValuationHandler(
        IValuationRepository valuations,
        IValuationModelRouter router,
        IVehicleDirectory vehicles,
        IVehicleTaxonomy taxonomy,
        ITaxonomyRegistry registry,
        IValidator<RequestValuationCommand> validator,
        IClock clock)
    {
        _valuations = valuations;
        _router = router;
        _vehicles = vehicles;
        _taxonomy = taxonomy;
        _registry = registry;
        _validator = validator;
        _clock = clock;
    }

    public async Task<Result<ValuationQuote>> Handle(
        RequestValuationCommand command,
        CancellationToken cancellationToken)
    {
        var validation = await _validator.ValidateAsync(command, cancellationToken);
        if (!validation.IsValid)
        {
            return RequestValidation.ToError(validation);
        }

        RegistrationNumber? registration = null;
        if (!string.IsNullOrWhiteSpace(command.RegistrationNumber))
        {
            var parsed = RegistrationNumber.TryParse(command.RegistrationNumber);
            if (parsed.IsFailure)
            {
                return Result.Failure<ValuationQuote>(parsed.Error);
            }

            registration = parsed.Value;
        }

        var vehicle = await ResolveVehicleAsync(command, registration, cancellationToken);
        if (vehicle.IsFailure)
        {
            return Result.Failure<ValuationQuote>(vehicle.Error);
        }

        var inputs = await BuildInputsAsync(command, vehicle.Value, cancellationToken);
        if (inputs.IsFailure)
        {
            return Result.Failure<ValuationQuote>(inputs.Error);
        }

        // The affinity key keeps a retried request on the model that answered the first attempt.
        // Falling back to the quote's own identity rather than a random value keeps that property
        // for callers that did not send an Idempotency-Key — every request is at least stable
        // against itself.
        var affinity = command.IdempotencyKey
            ?? registration?.Canonical
            ?? inputs.Value.CategoryCode;

        var run = await _router.RunAsync(inputs.Value, affinity, cancellationToken);
        if (run.IsFailure)
        {
            return Result.Failure<ValuationQuote>(run.Error);
        }

        var now = _clock.UtcNow;
        var asOf = _clock.TodayInIst;

        var recorded = Record(
            run.Value.Served, vehicle.Value, registration, inputs.Value, asOf, now, isShadow: false);

        if (recorded.IsFailure)
        {
            return Result.Failure<ValuationQuote>(recorded.Error);
        }

        await _valuations.AddAsync(recorded.Value, cancellationToken);

        foreach (var shadow in run.Value.Shadow)
        {
            var stored = Record(shadow, vehicle.Value, registration, inputs.Value, asOf, now, isShadow: true);

            if (stored.IsSuccess)
            {
                await _valuations.AddAsync(stored.Value, cancellationToken);
            }
        }

        await _valuations.SaveChangesAsync(cancellationToken);

        return await RenderAsync(recorded.Value, run.Value.Served, vehicle.Value, inputs.Value, cancellationToken);
    }

    /// <summary>
    /// Finds the vehicle, creating one only when the caller supplied a category.
    /// </summary>
    /// <remarks>
    /// An anonymous browse must not fill the catalog with cars nobody is selling, so the hero path
    /// looks up and stops. A caller who names a category is committing to a vehicle — the sell flow,
    /// a dealer upload — and gets one created through the catalog's deduplication.
    /// </remarks>
    private async Task<Result<VehicleSummary?>> ResolveVehicleAsync(
        RequestValuationCommand command,
        RegistrationNumber? registration,
        CancellationToken cancellationToken)
    {
        if (registration is { } plate)
        {
            var found = await _vehicles.FindByIdentifierAsync(
                new VehicleIdentifierInput(VehicleIdentifierType.Registration, plate.Canonical),
                cancellationToken);

            if (found.IsSuccess)
            {
                return found.Value;
            }

            // Anything other than "we have not seen it" — an ambiguous identifier, for instance — is
            // a real failure and must not be papered over by creating a second vehicle.
            if (found.Error.Kind != ErrorKind.NotFound)
            {
                return Result.Failure<VehicleSummary?>(found.Error);
            }
        }

        if (string.IsNullOrWhiteSpace(command.CategoryCode))
        {
            return registration is null
                ? Result.Failure<VehicleSummary?>(InsufficientInputs("no_identity"))
                : Result.Failure<VehicleSummary?>(InsufficientInputs("unknown_vehicle"));
        }

        var category = await _taxonomy.ResolveCategoryAsync(command.CategoryCode, cancellationToken);
        if (category.IsFailure)
        {
            return Result.Failure<VehicleSummary?>(category.Error);
        }

        if (registration is not { } known)
        {
            // A category with no identifier is a hypothetical car — "what is a 2021 Swift worth?" —
            // which is a legitimate question and does not deserve a row in the canonical catalog.
            return Result.Success<VehicleSummary?>(null);
        }

        var resolved = await _vehicles.ResolveOrCreateAsync(
            category.Value.Id,
            [new VehicleIdentifierInput(VehicleIdentifierType.Registration, known.Canonical)],
            cancellationToken);

        return resolved.IsSuccess
            ? Result.Success<VehicleSummary?>(resolved.Value)
            : Result.Failure<VehicleSummary?>(resolved.Error);
    }

    /// <summary>
    /// Merges what the caller sent with what the catalog already knows.
    /// </summary>
    /// <remarks>
    /// The caller wins on every field. A seller correcting the odometer reading on a VAHAN record is
    /// the normal case, and a merge that preferred the stored value would quietly ignore the one
    /// piece of information the seller took the trouble to give us.
    /// </remarks>
    private async Task<Result<ValuationInputs>> BuildInputsAsync(
        RequestValuationCommand command,
        VehicleSummary? vehicle,
        CancellationToken cancellationToken)
    {
        var codes = new Dictionary<Guid, TaxonomyRef>();

        if (vehicle is not null)
        {
            var ids = new List<Guid> { vehicle.CategoryId };
            AddIfPresent(ids, vehicle.MakeId);
            AddIfPresent(ids, vehicle.ModelId);
            AddIfPresent(ids, vehicle.VariantId);

            var described = await _taxonomy.DescribeAsync(ids, cancellationToken);
            if (described.IsSuccess)
            {
                codes = new Dictionary<Guid, TaxonomyRef>(described.Value);
            }
        }

        var categoryCode = command.CategoryCode
            ?? (vehicle is not null ? Code(codes, vehicle.CategoryId) : null);

        if (string.IsNullOrWhiteSpace(categoryCode))
        {
            return InsufficientInputs("no_category");
        }

        var attributes = await LoadAttributesAsync(vehicle, cancellationToken);

        return new ValuationInputs
        {
            VehicleId = vehicle?.VehicleId,
            CategoryId = vehicle?.CategoryId,
            CategoryCode = categoryCode,
            MakeCode = command.MakeCode ?? Code(codes, vehicle?.MakeId),
            ModelCode = command.ModelCode ?? Code(codes, vehicle?.ModelId),
            VariantCode = command.VariantCode ?? Code(codes, vehicle?.VariantId),
            ManufactureYear = command.ManufactureYear ?? vehicle?.ManufactureYear,
            ManufactureMonth = command.ManufactureMonth,
            FirstRegisteredOn = vehicle?.FirstRegisteredOn,
            KilometresDriven = command.KilometresDriven ?? ReadInteger(attributes, "kms_driven"),
            OwnerSerial = command.OwnerSerial ?? (int?)ReadInteger(attributes, "owners"),
            CityCode = command.City,
            FuelCode = ReadText(attributes, "fuel_type"),
            TransmissionCode = ReadText(attributes, "transmission"),
            ConditionCode = command.Condition,
            ReferencePrice = command.ReferencePriceMinorUnits is { } reference
                ? Money.FromMinorUnits(reference, Currency.Inr)
                : null,
            AsOf = _clock.TodayInIst,
            Attributes = attributes,
        };
    }

    /// <summary>
    /// The vehicle's stored attribute values, as a flat bag the model can read.
    /// </summary>
    /// <remarks>
    /// Read through the taxonomy registry rather than a column, so an attribute promoted into the
    /// registry this morning is a pricing input this afternoon — which is the same acceptance test
    /// the whole platform is built around.
    /// </remarks>
    private async Task<JsonObject> LoadAttributesAsync(VehicleSummary? vehicle, CancellationToken cancellationToken)
    {
        if (vehicle is null)
        {
            return [];
        }

        var values = await _registry.ResolveAttributeValuesAsync(
            CatalogEntityTypes.Vehicle, [vehicle.VehicleId], cancellationToken);

        if (values.IsFailure || !values.Value.ByEntity.TryGetValue(vehicle.VehicleId, out var resolved))
        {
            return [];
        }

        var bag = new JsonObject();
        foreach (var attribute in resolved)
        {
            bag[attribute.Code] = attribute.Value.ToJson();
        }

        return bag;
    }

    private static Result<Domain.Valuations.Valuation> Record(
        ValuationPrediction prediction,
        VehicleSummary? vehicle,
        RegistrationNumber? registration,
        ValuationInputs inputs,
        DateOnly asOf,
        DateTimeOffset now,
        bool isShadow)
        => Domain.Valuations.Valuation.Record(
            vehicle?.VehicleId,
            registration?.Canonical,
            vehicle?.CategoryId,
            prediction.ModelKey,
            prediction.ModelVersion,
            prediction.Currency,
            [.. prediction.Channels.Select(c => (c.Code, c.Label, c.AmountMinorUnits, c.IsPrimary, c.SortOrder))],
            [.. prediction.Adjustments.Select(a => (a.FactorCode, a.MatchValue, a.Label, a.Multiplier, a.DeltaMinorUnits))],
            prediction.LowMinorUnits,
            prediction.HighMinorUnits,
            prediction.Confidence,
            prediction.ConfidenceBand,
            inputs.ToJson(),
            ToForecastJson(prediction.Forecast),
            prediction.Explanation,
            asOf,
            now.AddDays(prediction.ValidityDays),
            isShadow);

    private static JsonArray ToForecastJson(IReadOnlyList<ForecastPoint> forecast)
    {
        var array = new JsonArray();

        foreach (var point in forecast)
        {
            array.Add(new JsonObject
            {
                ["month"] = point.Month,
                ["valueMinor"] = point.ValueMinorUnits,
                ["lowMinor"] = point.LowMinorUnits,
                ["highMinor"] = point.HighMinorUnits,
            });
        }

        return array;
    }

    private async Task<Result<ValuationQuote>> RenderAsync(
        Domain.Valuations.Valuation valuation,
        ValuationPrediction prediction,
        VehicleSummary? vehicle,
        ValuationInputs inputs,
        CancellationToken cancellationToken)
    {
        var title = await ComposeTitleAsync(vehicle, inputs, cancellationToken);

        return new ValuationQuote
        {
            QuoteId = valuation.Id,
            Vehicle = new QuotedVehicle(
                vehicle?.VehicleId,
                title,
                vehicle?.RegistrationDisplay,
                inputs.ManufactureYear,
                inputs.FuelCode,
                vehicle?.ProvenanceCode),
            Currency = valuation.CurrencyCode,
            Valuations = [.. valuation.Channels
                .OrderBy(c => c.SortOrder)
                .Select(c => new QuotedValuation(c.ChannelCode, c.Label, c.AmountMinorUnits, c.IsPrimary))],
            Range = new QuotedRange(valuation.LowMinorUnits, valuation.HighMinorUnits),
            Confidence = new QuotedConfidence(valuation.Confidence, valuation.ConfidenceBandCode, valuation.Explanation),
            Forecast = [.. prediction.Forecast.Select(p =>
                new QuotedForecastPoint(p.Month, p.ValueMinorUnits, p.LowMinorUnits, p.HighMinorUnits))],
            AsOf = valuation.AsOfIso,
            ExpiresAt = valuation.ExpiresAt,
            ModelVersion = valuation.ModelVersion,
        };
    }

    /// <summary>"2021 Maruti Suzuki Swift VXi", from whatever parts are known.</summary>
    private async Task<string> ComposeTitleAsync(
        VehicleSummary? vehicle,
        ValuationInputs inputs,
        CancellationToken cancellationToken)
    {
        var ids = new List<Guid>(3);
        AddIfPresent(ids, vehicle?.MakeId);
        AddIfPresent(ids, vehicle?.ModelId);
        AddIfPresent(ids, vehicle?.VariantId);

        var described = ids.Count == 0
            ? new Dictionary<Guid, TaxonomyRef>()
            : (await _taxonomy.DescribeAsync(ids, cancellationToken)) is { IsSuccess: true } result
                ? new Dictionary<Guid, TaxonomyRef>(result.Value)
                : [];

        var parts = new List<string>(4);

        if (inputs.ManufactureYear is { } year)
        {
            parts.Add(year.ToString(CultureInfo.InvariantCulture));
        }

        foreach (var id in ids)
        {
            if (described.TryGetValue(id, out var reference) && !string.IsNullOrWhiteSpace(reference.Name))
            {
                parts.Add(reference.Name);
            }
        }

        // No taxonomy at all still deserves a readable heading: the plate is what the seller typed
        // and what they will recognise.
        return parts.Count > 0
            ? string.Join(' ', parts)
            : vehicle?.RegistrationDisplay ?? inputs.CategoryCode;
    }

    private static Error InsufficientInputs(string reason)
        => Error.Validation(
            "valuation.insufficient_inputs",
            "There is not enough information about this vehicle to price it.")
            .With("reason", reason);

    private static string? Code(Dictionary<Guid, TaxonomyRef> codes, Guid? id)
        => id is { } value && codes.TryGetValue(value, out var reference) ? reference.Code : null;

    private static void AddIfPresent(List<Guid> ids, Guid? id)
    {
        if (id is { } value)
        {
            ids.Add(value);
        }
    }

    private static long? ReadInteger(JsonObject attributes, string code)
        => attributes.TryGetPropertyValue(code, out var node)
           && node is JsonValue value
           && value.TryGetValue<long>(out var number)
            ? number
            : null;

    private static string? ReadText(JsonObject attributes, string code)
        => attributes.TryGetPropertyValue(code, out var node)
           && node is JsonValue value
           && value.TryGetValue<string>(out var text)
            ? text
            : null;
}
