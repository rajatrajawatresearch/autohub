using System.Globalization;
using AutoHub.Modules.Listings.Application.Abstractions;

namespace AutoHub.Persistence.Search;

/// <summary>
/// The continuation token for a listing search, and the predicate that resumes from it.
/// </summary>
/// <remarks>
/// <b>Why a composite cursor and not an offset.</b> <c>OFFSET 240</c> makes PostgreSQL walk and
/// discard 240 rows before returning anything, and it is wrong as well as slow: a listing published
/// while a buyer is on page three shifts every later row down by one, so the buyer sees a car twice
/// and never sees another. A keyset cursor resumes from the last row's actual sort position and does
/// neither.
/// <para>
/// <b>Why it carries the sort key as well as the id.</b> A cursor of the id alone only works when the
/// ordering is the id. Sorting by price and paginating by id skips and repeats rows wherever two
/// listings share a price — which on a marketplace is constantly, because sellers round to the
/// nearest ten thousand. The token therefore carries both, and the predicate is the lexicographic
/// comparison the ordering implies.
/// </para>
/// <para>
/// The token is opaque to the client by contract, so its encoding is free to change. It is
/// deliberately not encrypted: it contains a price and a public id, neither of which is a secret,
/// and encrypting it would add a key to rotate for no protection.
/// </para>
/// </remarks>
internal readonly record struct SearchCursor(long SortValue, Guid ListingId)
{
    private const char Separator = '|';

    public static SearchCursor? Parse(string? token)
    {
        if (string.IsNullOrWhiteSpace(token))
        {
            return null;
        }

        var parts = token.Split(Separator);

        // A malformed cursor starts from the beginning rather than failing. It arrives from a stale
        // bookmark or a hand-edited URL, and a 400 there is a dead end the user cannot escape.
        if (parts.Length != 2
            || !long.TryParse(parts[0], NumberStyles.Integer, CultureInfo.InvariantCulture, out var sortValue)
            || !Guid.TryParse(parts[1], out var listingId))
        {
            return null;
        }

        return new SearchCursor(sortValue, listingId);
    }

    public static string Format(ListingSearchDocument document, ListingSort sort)
        => string.Create(
            CultureInfo.InvariantCulture,
            $"{SortValueOf(document, sort)}{Separator}{document.ListingId}");

    /// <summary>
    /// Resumes the query after the cursor, in whichever direction the sort runs.
    /// </summary>
    /// <remarks>
    /// The comparison must mirror <c>Order</c> exactly, which is why both live in this file's
    /// neighbourhood and both switch on the same enum. Two independent decisions about direction is
    /// how a "load more" button starts returning page one.
    /// </remarks>
    public static IQueryable<ListingSearchDocument> Apply(
        IQueryable<ListingSearchDocument> documents,
        ListingSort sort,
        SearchCursor? cursor)
    {
        if (cursor is not { } from)
        {
            return documents;
        }

        var value = from.SortValue;
        var id = from.ListingId;

        return sort switch
        {
            ListingSort.PriceAscending => documents.Where(d =>
                d.PriceMinorUnits > value
                || (d.PriceMinorUnits == value && d.ListingId.CompareTo(id) > 0)),

            ListingSort.PriceDescending => documents.Where(d =>
                d.PriceMinorUnits < value
                || (d.PriceMinorUnits == value && d.ListingId.CompareTo(id) > 0)),

            // Listings with no odometer reading sort last, so the cursor treats them as the maximum
            // — the same substitution the ordering makes, kept identical on purpose.
            ListingSort.KilometresAscending => documents.Where(d =>
                (d.KilometresDriven ?? long.MaxValue) > value
                || ((d.KilometresDriven ?? long.MaxValue) == value && d.ListingId.CompareTo(id) > 0)),

            // Newest and relevance both order by the id descending, and the id is UUID v7, so its
            // ordering is already chronological.
            _ => documents.Where(d => d.ListingId.CompareTo(id) < 0),
        };
    }

    private static long SortValueOf(ListingSearchDocument document, ListingSort sort) => sort switch
    {
        ListingSort.PriceAscending or ListingSort.PriceDescending => document.PriceMinorUnits,
        ListingSort.KilometresAscending => document.KilometresDriven ?? long.MaxValue,
        _ => 0,
    };
}
