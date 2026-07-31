using AutoHub.SharedKernel.Domain;
using AutoHub.SharedKernel.Ids;
using AutoHub.SharedKernel.Results;

namespace AutoHub.Modules.Identity.Domain;

/// <summary>
/// A person the platform knows, identified by their phone number.
/// </summary>
/// <remarks>
/// Deliberately thin. A user row carries identity and nothing else — no role column, no dealer
/// column, no tier column — because every one of those is a relationship that the RBAC registry and
/// the owning module already model, and a denormalised copy here is a copy that goes stale exactly
/// when it matters: after a revoke.
/// <para>
/// There is no password. India is phone-first, the OTP is the primary factor, and a password column
/// nobody uses is a credential-stuffing surface with no compensating benefit. When a password is
/// added for staff logins it will be a separate credential row with its own lifecycle, not a
/// nullable column on this table.
/// </para>
/// </remarks>
public sealed class User : AggregateRoot, IAuditable, ISoftDeletable
{
    private User(Guid id, string phone, string displayName) : base(id)
    {
        Phone = phone;
        DisplayName = displayName;
        IsActive = true;
    }

    private User() { }

    /// <summary>E.164, canonicalised by <see cref="PhoneNumber"/>. Unique among live rows.</summary>
    public string Phone { get; private set; } = string.Empty;

    /// <summary>
    /// What the product calls them. Seeded from the masked phone until they tell us their name,
    /// because a UI that renders an empty string looks broken and one that renders "User" is rude.
    /// </summary>
    public string DisplayName { get; private set; } = string.Empty;

    public string? Email { get; private set; }

    /// <summary>
    /// The city the user is shopping in, from <c>X-Active-City</c>.
    /// </summary>
    /// <remarks>
    /// A code, validated against the geography registry before it is stored — never a free string,
    /// or search scoping and feature targeting quietly stop matching anything.
    /// </remarks>
    public string? ActiveCityCode { get; private set; }

    /// <summary>
    /// How far KYC has progressed. A number rather than an enum because what each level requires is
    /// a compliance decision that changes without a deployment; the client only ever compares it.
    /// </summary>
    public int KycLevel { get; private set; }

    /// <summary>
    /// Whether the account may authenticate at all. Distinct from soft deletion: a suspended user is
    /// still a party to open deals and still has to appear on them.
    /// </summary>
    public bool IsActive { get; private set; }

    public DateTimeOffset? LastAuthenticatedAt { get; private set; }

    public DateTimeOffset CreatedAt { get; set; }

    public Guid? CreatedBy { get; set; }

    public DateTimeOffset? UpdatedAt { get; set; }

    public Guid? UpdatedBy { get; set; }

    public DateTimeOffset? DeletedAt { get; set; }

    public Guid? DeletedBy { get; set; }

    public static Result<User> Register(PhoneNumber phone, string? displayName)
    {
        var name = string.IsNullOrWhiteSpace(displayName)
            ? phone.ToMaskedDisplay()
            : displayName.Trim();

        return name.Length > 120
            ? Error.Validation("identity.user.name_too_long", "That name is too long.")
            : new User(UuidV7.New(), phone.Value, name);
    }

    public Result Rename(string displayName)
    {
        if (string.IsNullOrWhiteSpace(displayName))
        {
            return Error.Validation("identity.user.name_required", "Enter a name.");
        }

        DisplayName = displayName.Trim();
        return Result.Success();
    }

    public void MoveTo(string? cityCode) => ActiveCityCode = cityCode;

    public void RecordAuthentication(DateTimeOffset at) => LastAuthenticatedAt = at;

    public void Suspend() => IsActive = false;

    public void Reinstate() => IsActive = true;
}
