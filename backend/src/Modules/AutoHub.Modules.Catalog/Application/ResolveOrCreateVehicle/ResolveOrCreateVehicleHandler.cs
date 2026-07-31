using System.Text.Json.Nodes;
using AutoHub.Modules.Catalog.Application.Abstractions;
using AutoHub.Modules.Catalog.Application.Validation;
using AutoHub.Modules.Catalog.Contracts;
using AutoHub.Modules.Catalog.Domain.Vehicles;
using AutoHub.Platform.Taxonomy;
using AutoHub.SharedKernel.Results;
using AutoHub.SharedKernel.Time;
using FluentValidation;
using MediatR;
using Microsoft.Extensions.Logging;

namespace AutoHub.Modules.Catalog.Application.ResolveOrCreateVehicle;

/// <summary>
/// Deduplication. The one piece of logic that keeps one car from being three rows.
/// </summary>
/// <remarks>
/// <b>The algorithm, and why it is shaped this way.</b> Identifiers are ranked by permanence — VIN,
/// then chassis, then engine, then registration — and the <i>strongest matching</i> identifier
/// decides which vehicle we are looking at. That ordering is the whole design, because the only
/// interesting case is disagreement, and disagreement between a VIN and a plate has exactly one
/// real-world explanation: the plate moved.
/// <list type="number">
/// <item><b>Nothing matches.</b> A new canonical vehicle, with every submitted identifier opened as
/// an active period.</item>
/// <item><b>Everything matches one vehicle.</b> Return it, adding any identifier it did not already
/// hold. Idempotent, which matters because a dealer's stock feed replays the same rows nightly.</item>
/// <item><b>The strongest identifier matches vehicle A, and a <i>registration</i> matches vehicle
/// B.</b> The RTO reissued or transferred the number. B's claim on that plate is closed and A's is
/// opened — in the same transaction, because the partial unique index would otherwise reject the
/// insert.</item>
/// <item><b>The strongest identifier matches A and another <i>permanent</i> identifier matches
/// B.</b> Two chassis numbers cannot both be right. The module refuses and returns a conflict
/// carrying both vehicle ids, for a human to resolve.</item>
/// </list>
/// <para>
/// <b>Why an automatic merge is never attempted.</b> Merging two vehicles merges their listings,
/// their inspection reports, their deals and therefore their money. It is the one operation in this
/// module that cannot be undone by an operator with a form, so it is not something a bulk import at
/// 3am gets to trigger on the strength of a mistyped chassis number.
/// </para>
/// <para>
/// <b>Why this handler writes two aggregates.</b> The aggregate rule says a transaction saves one
/// aggregate and everything else reacts to an event. Case 3 breaks that deliberately: closing B's
/// plate period and opening A's is one invariant, enforced by one unique index, and splitting it
/// across two transactions produces either a window where two live vehicles hold one live plate —
/// which the index forbids, so the write simply fails — or a window where nobody holds it and a
/// search by plate returns nothing. One transaction is the only correct option here.
/// </para>
/// </remarks>
public sealed class ResolveOrCreateVehicleHandler
    : IRequestHandler<ResolveOrCreateVehicleCommand, Result<ResolveOrCreateVehicleResponse>>
{
    private readonly IVehicleRepository _vehicles;
    private readonly IVehicleTaxonomy _taxonomy;
    private readonly ITaxonomyRegistry _registry;
    private readonly IEntityAttributeStore _attributes;
    private readonly IValidator<ResolveOrCreateVehicleCommand> _validator;
    private readonly IClock _clock;
    private readonly ILogger<ResolveOrCreateVehicleHandler> _logger;

    public ResolveOrCreateVehicleHandler(
        IVehicleRepository vehicles,
        IVehicleTaxonomy taxonomy,
        ITaxonomyRegistry registry,
        IEntityAttributeStore attributes,
        IValidator<ResolveOrCreateVehicleCommand> validator,
        IClock clock,
        ILogger<ResolveOrCreateVehicleHandler> logger)
    {
        _vehicles = vehicles;
        _taxonomy = taxonomy;
        _registry = registry;
        _attributes = attributes;
        _validator = validator;
        _clock = clock;
        _logger = logger;
    }

    public async Task<Result<ResolveOrCreateVehicleResponse>> Handle(
        ResolveOrCreateVehicleCommand command,
        CancellationToken cancellationToken)
    {
        var validation = await _validator.ValidateAsync(command, cancellationToken);
        if (!validation.IsValid)
        {
            return RequestValidation.ToError(validation);
        }

        var identifiers = ParseIdentifiers(command.Identifiers);
        if (identifiers.IsFailure)
        {
            return Result.Failure<ResolveOrCreateVehicleResponse>(identifiers.Error);
        }

        var classification = await _taxonomy.ResolveClassificationAsync(
            command.CategoryCode, command.MakeCode, command.ModelCode, command.VariantCode, cancellationToken);

        if (classification.IsFailure)
        {
            return Result.Failure<ResolveOrCreateVehicleResponse>(classification.Error);
        }

        var now = _clock.UtcNow;

        var resolved = await ResolveAsync(identifiers.Value, classification.Value, now, cancellationToken);
        if (resolved.IsFailure)
        {
            return Result.Failure<ResolveOrCreateVehicleResponse>(resolved.Error);
        }

        var (vehicle, outcome) = resolved.Value;

        vehicle.Classify(
            classification.Value.Make?.Id,
            classification.Value.Model?.Id,
            classification.Value.Variant?.Id);

        var provenance = vehicle.SetProvenance(
            command.ManufactureYear,
            command.ManufactureMonth,
            command.FirstRegisteredOn,
            command.ProvenanceCode);

        if (provenance.IsFailure)
        {
            return Result.Failure<ResolveOrCreateVehicleResponse>(provenance.Error);
        }

        if (command.Attributes is { } submitted)
        {
            var applied = await ApplyAttributesAsync(
                vehicle, classification.Value.Category.Code, submitted, now, cancellationToken);

            if (applied.IsFailure)
            {
                return Result.Failure<ResolveOrCreateVehicleResponse>(applied.Error);
            }
        }

        await _vehicles.SaveChangesAsync(cancellationToken);

        return new ResolveOrCreateVehicleResponse(vehicle.ToSummary(), outcome);
    }

    /// <summary>
    /// Canonicalises the submitted identifiers and refuses a set that contradicts itself.
    /// </summary>
    /// <remarks>
    /// Two different VINs in one submission is a client that concatenated two rows, and resolving it
    /// as "whichever came first" would attach one car's history to another's.
    /// </remarks>
    private static Result<IReadOnlyList<VehicleIdentifier>> ParseIdentifiers(
        IReadOnlyList<VehicleIdentifierInput> inputs)
    {
        var byType = new Dictionary<VehicleIdentifierType, VehicleIdentifier>();

        foreach (var input in inputs)
        {
            var parsed = VehicleIdentifier.TryParse(input.Type, input.Value);
            if (parsed.IsFailure)
            {
                return Result.Failure<IReadOnlyList<VehicleIdentifier>>(parsed.Error);
            }

            if (byType.TryGetValue(input.Type, out var existing)
                && !string.Equals(existing.Value, parsed.Value.Value, StringComparison.Ordinal))
            {
                return Error.Validation(
                    "catalog.vehicle.identifier_self_conflict",
                    $"Two different {input.Type.ToWireName()} values were submitted for one vehicle.")
                    .With("identifierType", input.Type.ToWireName());
            }

            byType[input.Type] = parsed.Value;
        }

        // Strongest first. Every consumer below relies on this order, so it is established once here
        // rather than re-sorted at three call sites that could disagree.
        return Result.Success<IReadOnlyList<VehicleIdentifier>>(
            [.. byType.Values.OrderByDescending(i => i.Type)]);
    }

    private async Task<Result<(Vehicle Vehicle, VehicleResolution Outcome)>> ResolveAsync(
        IReadOnlyList<VehicleIdentifier> identifiers,
        VehicleClassification classification,
        DateTimeOffset now,
        CancellationToken cancellationToken)
    {
        var candidates = await _vehicles.FindByActiveIdentifiersAsync(identifiers, cancellationToken);

        if (candidates.Count == 0)
        {
            var created = Vehicle.Register(classification.Category.Id, now);

            foreach (var identifier in identifiers)
            {
                var recorded = created.RecordIdentifier(identifier, now);
                if (recorded.IsFailure)
                {
                    return Result.Failure<(Vehicle, VehicleResolution)>(recorded.Error);
                }
            }

            await _vehicles.AddAsync(created, cancellationToken);
            return (created, VehicleResolution.Created);
        }

        var holders = identifiers.ToDictionary(
            identifier => identifier,
            identifier => candidates.Where(v => v.Holds(identifier)).ToList());

        var strongest = identifiers.First(identifier => holders[identifier].Count > 0);
        var anchors = holders[strongest];

        if (anchors.Count > 1)
        {
            // The partial unique index makes this unreachable through the database. Reaching it means
            // the index is missing or was created without its filter, and continuing would pick one
            // of two vehicles arbitrarily — so it fails loudly instead.
            _logger.LogError(
                "Identifier {Identifier} is active on {Count} vehicles; the active-period unique index is not doing its job",
                strongest, anchors.Count);

            return Error.Conflict(
                "catalog.vehicle.ambiguous_identifier",
                "That identifier is active on more than one vehicle and cannot be resolved automatically.")
                .With("identifierType", strongest.Type.ToWireName())
                .With("value", strongest.Value);
        }

        var anchor = anchors[0];
        var outcome = VehicleResolution.Matched;

        foreach (var identifier in identifiers)
        {
            var others = holders[identifier].Where(v => v.Id != anchor.Id).ToList();

            foreach (var other in others)
            {
                if (identifier.Type != VehicleIdentifierType.Registration)
                {
                    // A log line rather than a domain event, and deliberately so: this request is
                    // about to be refused, nothing is committed, and an outbox row can only be
                    // written by a transaction that commits. Faking one would mean writing a record
                    // of a change that did not happen. The ERROR carries both vehicle ids, which is
                    // what a support engineer needs to open the two records side by side.
                    _logger.LogError(
                        "Vehicle {Anchor} and {Other} both claim {Identifier}; refusing to merge",
                        anchor.Id, other.Id, identifier);

                    return Error.Conflict(
                        "catalog.vehicle.identity_conflict",
                        "This identifier already belongs to a different vehicle. A person has to decide which record is right.")
                        .With("identifierType", identifier.Type.ToWireName())
                        .With("value", identifier.Value)
                        .With("vehicleId", anchor.Id)
                        .With("conflictingVehicleId", other.Id);
                }

                // A plate on another vehicle while a permanent identifier points here is exactly the
                // reissue case. The other vehicle stopped wearing that number whether or not anybody
                // told us, so its period is closed and the history stays intact.
                _logger.LogInformation(
                    "Registration {Value} moved from vehicle {From} to {To}",
                    identifier.Value, other.Id, anchor.Id);

                other.ReleaseIdentifier(identifier, now);
                outcome = VehicleResolution.Reconciled;
            }

            if (anchor.Holds(identifier))
            {
                continue;
            }

            var recorded = anchor.RecordIdentifier(identifier, now);
            if (recorded.IsFailure)
            {
                return Result.Failure<(Vehicle, VehicleResolution)>(recorded.Error);
            }

            outcome = VehicleResolution.Reconciled;
        }

        return (anchor, outcome);
    }

    /// <summary>
    /// Validates the submitted bag against the category, stores it, and refreshes the projection.
    /// </summary>
    /// <remarks>
    /// All three in one place because they are one operation: EAV rows that disagree with the
    /// denormalised bag are worse than either alone, since a range facet and a spec sheet would then
    /// show different numbers for the same car and nobody could say which was right.
    /// </remarks>
    private async Task<Result> ApplyAttributesAsync(
        Vehicle vehicle,
        string categoryCode,
        JsonObject submitted,
        DateTimeOffset now,
        CancellationToken cancellationToken)
    {
        var validated = await _registry.ValidateAttributesAsync(categoryCode, submitted, cancellationToken);
        if (validated.IsFailure)
        {
            return Result.Failure(validated.Error);
        }

        var stored = await _attributes.ReplaceAsync(
            CatalogEntityTypes.Vehicle, vehicle.Id, validated.Value, cancellationToken);

        if (stored.IsFailure)
        {
            return stored;
        }

        var projection = new JsonObject();
        foreach (var (code, value) in validated.Value)
        {
            projection[code] = value.ToJson();
        }

        vehicle.ProjectAttributes(projection, now);
        return Result.Success();
    }
}
