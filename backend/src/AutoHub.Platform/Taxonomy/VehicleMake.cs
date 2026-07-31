using AutoHub.SharedKernel.Domain;
using AutoHub.SharedKernel.Ids;
using AutoHub.SharedKernel.Results;

namespace AutoHub.Platform.Taxonomy;

/// <summary>
/// A manufacturer — Maruti Suzuki, Mahindra, Tata, Ashok Leyland, Hero.
/// </summary>
/// <remarks>
/// A make deliberately carries no category. Mahindra sells an SUV, a pickup and a tractor; Tata
/// sells a hatchback and a 49-tonne tractor-trailer. Pinning a make to one category would make
/// <c>?category=commercial</c> either wrong or a special case, so the category lives on the
/// <see cref="VehicleModel"/> and "which makes exist in this category" is answered by the models
/// underneath it.
/// </remarks>
public sealed class VehicleMake : AggregateRoot, IAuditable, ISoftDeletable
{
    private VehicleMake(Guid id, string code, string name, string? countryOfOrigin, string? logoKey, int sortOrder)
        : base(id)
    {
        Code = code;
        Name = name;
        CountryOfOrigin = countryOfOrigin;
        LogoKey = logoKey;
        SortOrder = sortOrder;
        IsActive = true;
    }

    private VehicleMake() { }

    /// <summary>URL slug, e.g. <c>maruti-suzuki</c>. Part of the public contract once shipped.</summary>
    public string Code { get; private set; } = string.Empty;

    public string Name { get; private set; } = string.Empty;

    /// <summary>
    /// ISO 3166-1 alpha-2 country code of the parent company, e.g. <c>JP</c> for Suzuki.
    /// Buyers filter on "Indian brands" and "German brands"; a free-text country name would make
    /// that filter unjoinable against the geography registry.
    /// </summary>
    public string? CountryOfOrigin { get; private set; }

    /// <summary>
    /// Object-store key, not a URL. The CDN host, the signing policy and the rendition suffix all
    /// change without the marque changing, so the resolvable URL is built at the edge.
    /// </summary>
    public string? LogoKey { get; private set; }

    public int SortOrder { get; private set; }

    public bool IsActive { get; private set; }

    public DateTimeOffset CreatedAt { get; set; }

    public Guid? CreatedBy { get; set; }

    public DateTimeOffset? UpdatedAt { get; set; }

    public Guid? UpdatedBy { get; set; }

    public DateTimeOffset? DeletedAt { get; set; }

    public Guid? DeletedBy { get; set; }

    public static Result<VehicleMake> Create(
        string code,
        string name,
        string? countryOfOrigin = null,
        string? logoKey = null,
        int sortOrder = 0)
    {
        var validCode = TaxonomyCode.Validate(code, "taxonomy.make.code_invalid");
        if (validCode.IsFailure)
        {
            return Result.Failure<VehicleMake>(validCode.Error);
        }

        if (string.IsNullOrWhiteSpace(name))
        {
            return Error.Validation("taxonomy.make.name_required", "A make needs a display name.");
        }

        return new VehicleMake(UuidV7.New(), validCode.Value, name.Trim(), countryOfOrigin, logoKey, sortOrder);
    }

    public void UpdateBranding(string? logoKey, int sortOrder)
    {
        LogoKey = logoKey;
        SortOrder = sortOrder;
    }

    public void Activate() => IsActive = true;

    public void Deactivate() => IsActive = false;
}
