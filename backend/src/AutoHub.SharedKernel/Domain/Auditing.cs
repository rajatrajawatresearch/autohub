namespace AutoHub.SharedKernel.Domain;

/// <summary>
/// Who touched this row and when. Populated by a save-changes interceptor, never by hand —
/// a field that developers are trusted to set is a field that is wrong.
/// </summary>
public interface IAuditable
{
    DateTimeOffset CreatedAt { get; set; }

    Guid? CreatedBy { get; set; }

    DateTimeOffset? UpdatedAt { get; set; }

    Guid? UpdatedBy { get; set; }
}

/// <summary>
/// Rows we hide rather than delete.
/// </summary>
/// <remarks>
/// A sold listing, a terminated attachment contract, and a rejected KYC record all have to survive
/// deletion — tax law, dispute resolution, and fraud investigation all need them. The DPDP Act's
/// erasure right is served by a separate, deliberate anonymisation job that scrubs personal fields
/// while preserving the transaction record, not by dropping rows.
/// </remarks>
public interface ISoftDeletable
{
    DateTimeOffset? DeletedAt { get; set; }

    Guid? DeletedBy { get; set; }

    bool IsDeleted => DeletedAt.HasValue;
}
