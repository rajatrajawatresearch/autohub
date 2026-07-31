using System.Text.Json.Nodes;
using AutoHub.Api.Application.Behaviours;
using AutoHub.Persistence;
using AutoHub.Platform.Access;
using AutoHub.Platform.Caching;
using AutoHub.Platform.Taxonomy;
using AutoHub.SharedKernel.Results;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace AutoHub.Api.Application.RegistryAdmin;

/// <summary>The write side of registry 1 — the acceptance test of the whole architecture.</summary>
/// <remarks>
/// "A new vehicle category, with its own attributes, its own sell form, its own commission slab,
/// goes live without a deployment." These three commands are the first third of that sentence.
/// </remarks>
public sealed record CreateCategoryBody(
    string Code,
    string Name,
    string? ParentCode,
    string? Icon,
    int SortOrder);

public sealed record CreateAttributeBody(
    string Code,
    string Label,
    string DataType,
    string? Unit,
    bool Required,
    bool Searchable,
    string? FacetStyle,
    string? DisplayGroup,
    int SortOrder,
    decimal? Min,
    decimal? Max,
    decimal? Step,
    int? MinLength,
    int? MaxLength,
    string? Pattern,
    IReadOnlyList<AttributeOptionBody>? Options);

public sealed record AttributeOptionBody(string Value, string Label, int SortOrder);

public sealed record AttachAttributeBody(
    string AttributeCode,
    bool? Required,
    bool? Searchable,
    string? FacetStyle,
    string? DisplayGroup,
    int? SortOrder,
    bool Hidden);

internal sealed record CreateCategoryCommand(CreateCategoryBody Body)
    : IRequest<Result<RegistryWriteResponse>>, ITransactionalRequest;

internal sealed record CreateAttributeCommand(CreateAttributeBody Body)
    : IRequest<Result<RegistryWriteResponse>>, ITransactionalRequest;

internal sealed record AttachAttributeCommand(string CategoryCode, AttachAttributeBody Body)
    : IRequest<Result<RegistryWriteResponse>>, ITransactionalRequest;

internal sealed class CreateCategoryValidator : AbstractValidator<CreateCategoryCommand>
{
    public CreateCategoryValidator()
    {
        RuleFor(command => command.Body.Code).NotEmpty().WithErrorCode("taxonomy.category.code_invalid");
        RuleFor(command => command.Body.Name).NotEmpty().WithErrorCode("taxonomy.category.name_required");
    }
}

internal sealed class CreateAttributeValidator : AbstractValidator<CreateAttributeCommand>
{
    public CreateAttributeValidator()
    {
        RuleFor(command => command.Body.Code).NotEmpty().WithErrorCode("taxonomy.attribute.code_invalid");
        RuleFor(command => command.Body.Label).NotEmpty().WithErrorCode("taxonomy.attribute.label_required");
        RuleFor(command => command.Body.DataType).NotEmpty().WithErrorCode("taxonomy.attribute.data_type_unknown");
    }
}

internal sealed class TaxonomyAdminHandler
    : IRequestHandler<CreateCategoryCommand, Result<RegistryWriteResponse>>,
      IRequestHandler<CreateAttributeCommand, Result<RegistryWriteResponse>>,
      IRequestHandler<AttachAttributeCommand, Result<RegistryWriteResponse>>
{
    private const string CategoryEntity = "platform.vehicle_category";
    private const string AttributeEntity = "platform.attribute_definition";

    private readonly AutoHubDbContext _context;
    private readonly RegistryPublisher _publisher;

    public TaxonomyAdminHandler(AutoHubDbContext context, RegistryPublisher publisher)
    {
        _context = context;
        _publisher = publisher;
    }

    public async Task<Result<RegistryWriteResponse>> Handle(
        CreateCategoryCommand command,
        CancellationToken cancellationToken)
    {
        var body = command.Body;

        Guid? parentId = null;

        if (!string.IsNullOrWhiteSpace(body.ParentCode))
        {
            var parent = await _context.VehicleCategories
                .FirstOrDefaultAsync(category => category.Code == body.ParentCode, cancellationToken);

            if (parent is null)
            {
                return Error.NotFound(
                    "taxonomy.category.parent_not_found",
                    $"No category with code '{body.ParentCode}'.")
                    .With("parentCode", body.ParentCode);
            }

            parentId = parent.Id;
        }

        var created = VehicleCategory.Create(body.Code, body.Name, parentId, body.Icon, body.SortOrder);
        if (created.IsFailure)
        {
            return Result.Failure<RegistryWriteResponse>(created.Error);
        }

        _context.VehicleCategories.Add(created.Value);

        var version = await _publisher.PublishAsync(
            RegistryNames.Taxonomy,
            PlatformPermissions.TaxonomyPublish,
            CategoryEntity,
            created.Value.Code,
            created.Value.Id,
            $"Created category '{created.Value.Code}'.",
            before: null,
            after: new JsonObject
            {
                ["code"] = created.Value.Code,
                ["name"] = created.Value.Name,
                ["parentCode"] = body.ParentCode,
            },
            cancellationToken);

        return new RegistryWriteResponse(RegistryNames.Taxonomy, version, created.Value.Code, created.Value.Id);
    }

    public async Task<Result<RegistryWriteResponse>> Handle(
        CreateAttributeCommand command,
        CancellationToken cancellationToken)
    {
        var body = command.Body;

        var dataType = TaxonomyWireNames.ParseDataType(body.DataType);
        if (dataType.IsFailure)
        {
            return Result.Failure<RegistryWriteResponse>(dataType.Error);
        }

        var facetStyle = string.IsNullOrWhiteSpace(body.FacetStyle)
            ? Result.Success(FacetStyle.List)
            : TaxonomyWireNames.ParseFacetStyle(body.FacetStyle);

        if (facetStyle.IsFailure)
        {
            return Result.Failure<RegistryWriteResponse>(facetStyle.Error);
        }

        var created = AttributeDefinition.Create(body.Code, body.Label, dataType.Value);
        if (created.IsFailure)
        {
            return Result.Failure<RegistryWriteResponse>(created.Error);
        }

        var definition = created.Value;

        definition.SetPresentation(
            body.Unit, body.DisplayGroup, body.SortOrder, body.Required, body.Searchable, facetStyle.Value);

        var numeric = definition.SetNumericBounds(body.Min, body.Max, body.Step);
        if (numeric.IsFailure)
        {
            return Result.Failure<RegistryWriteResponse>(numeric.Error);
        }

        var text = definition.SetTextBounds(body.MinLength, body.MaxLength, body.Pattern);
        if (text.IsFailure)
        {
            return Result.Failure<RegistryWriteResponse>(text.Error);
        }

        foreach (var option in body.Options ?? [])
        {
            var added = definition.AddOption(option.Value, option.Label, option.SortOrder);
            if (added.IsFailure)
            {
                return Result.Failure<RegistryWriteResponse>(added.Error);
            }
        }

        // Validated before it is stored, not before it is served. An enum with no options is a
        // dropdown a seller cannot complete and a facet with nothing in it; catching that here means
        // the operator sees it while they are still editing.
        var validation = definition.Validate();
        if (validation.IsFailure)
        {
            return Result.Failure<RegistryWriteResponse>(validation.Error);
        }

        _context.AttributeDefinitions.Add(definition);

        var version = await _publisher.PublishAsync(
            RegistryNames.Taxonomy,
            PlatformPermissions.TaxonomyPublish,
            AttributeEntity,
            definition.Code,
            definition.Id,
            $"Created attribute '{definition.Code}' ({body.DataType}).",
            before: null,
            after: new JsonObject
            {
                ["code"] = definition.Code,
                ["dataType"] = body.DataType,
                ["options"] = definition.Options.Count,
            },
            cancellationToken);

        return new RegistryWriteResponse(RegistryNames.Taxonomy, version, definition.Code, definition.Id);
    }

    public async Task<Result<RegistryWriteResponse>> Handle(
        AttachAttributeCommand command,
        CancellationToken cancellationToken)
    {
        var category = await _context.VehicleCategories
            .FirstOrDefaultAsync(item => item.Code == command.CategoryCode, cancellationToken);

        if (category is null)
        {
            return Error.NotFound("taxonomy.category.not_found", $"No category '{command.CategoryCode}'.");
        }

        var attribute = await _context.AttributeDefinitions
            .FirstOrDefaultAsync(item => item.Code == command.Body.AttributeCode, cancellationToken);

        if (attribute is null)
        {
            return Error.NotFound(
                "taxonomy.attribute.not_found",
                $"No attribute '{command.Body.AttributeCode}'.");
        }

        var existing = await _context.CategoryAttributes.FirstOrDefaultAsync(
            link => link.CategoryId == category.Id && link.AttributeDefinitionId == attribute.Id,
            cancellationToken);

        if (existing is not null)
        {
            return Error.Conflict(
                "taxonomy.category_attribute.duplicate",
                $"'{attribute.Code}' is already attached to '{category.Code}'.");
        }

        var link = command.Body.Hidden
            ? CategoryAttribute.Hide(category.Id, attribute.Id)
            : CategoryAttribute.Attach(category.Id, attribute.Id);

        if (!command.Body.Hidden)
        {
            var facetStyle = string.IsNullOrWhiteSpace(command.Body.FacetStyle)
                ? Result.Success<FacetStyle?>(null)
                : TaxonomyWireNames.ParseFacetStyle(command.Body.FacetStyle).Map(style => (FacetStyle?)style);

            if (facetStyle.IsFailure)
            {
                return Result.Failure<RegistryWriteResponse>(facetStyle.Error);
            }

            link.Override(
                command.Body.Required,
                command.Body.Searchable,
                facetStyle.Value,
                command.Body.DisplayGroup,
                command.Body.SortOrder);
        }

        _context.CategoryAttributes.Add(link);

        var version = await _publisher.PublishAsync(
            RegistryNames.Taxonomy,
            PlatformPermissions.TaxonomyPublish,
            CategoryEntity,
            category.Code,
            category.Id,
            $"Attached '{attribute.Code}' to '{category.Code}'.",
            before: null,
            after: new JsonObject
            {
                ["categoryCode"] = category.Code,
                ["attributeCode"] = attribute.Code,
                ["hidden"] = command.Body.Hidden,
            },
            cancellationToken);

        return new RegistryWriteResponse(RegistryNames.Taxonomy, version, category.Code, link.Id);
    }
}
