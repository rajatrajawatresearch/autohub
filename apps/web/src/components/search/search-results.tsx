'use client';

import Link from 'next/link';
import { useSearch, type ListingIntent } from '@autohub/api';
import { Badge, Button, EmptyState, Input, Select, Skeleton, VehicleCard } from '@autohub/ui';
import { FacetRail } from './facet-rail';
import { useSearchFilters } from './use-search-filters';
import { useActiveCity } from '@/lib/use-active-city';

const SORTS = [
  { value: 'relevance', label: 'Most relevant' },
  { value: 'newest', label: 'Newest first' },
  { value: 'price_asc', label: 'Price: low to high' },
  { value: 'price_desc', label: 'Price: high to low' },
  { value: 'kms_asc', label: 'Kms: low to high' },
] as const;

/** What the money on a card means changes with the intent, so the caller says it. */
const PRICE_LABEL: Record<ListingIntent, string> = {
  sale: 'Asking price',
  auction: 'Current bid',
  rental: 'Per day',
  lease: 'Per month',
  attachment: 'Per month',
  exchange: 'Exchange value',
};

export function SearchResults({ intent }: { intent: ListingIntent }) {
  const { filters, setFilters, clear, activeCount } = useSearchFilters();
  const [city] = useActiveCity();

  const search = useSearch({
    intent,
    city,
    q: filters.q || undefined,
    sort: filters.sort,
    attributes: Object.keys(filters.attributes).length > 0 ? filters.attributes : undefined,
    limit: 24,
  });

  return (
    <div className="mx-auto max-w-[var(--container-content)] px-4 py-10 sm:px-6">
      <div className="flex flex-col gap-3 sm:flex-row sm:items-center">
        <Input
          value={filters.q}
          onChange={(event) => setFilters({ q: event.target.value })}
          placeholder="Search make, model or variant"
          aria-label="Search vehicles"
          className="sm:max-w-sm"
        />

        <div className="flex items-center gap-3 sm:ml-auto">
          <p className="font-mono text-body-sm text-[color:var(--text-secondary)]">
            {search.isLoading
              ? '…'
              : `${search.totalEstimate ?? search.items.length} vehicles`}
          </p>
          <Select
            value={filters.sort}
            onValueChange={(value) => setFilters({ sort: value as (typeof SORTS)[number]['value'] })}
            options={SORTS.map((s) => ({ value: s.value, label: s.label }))}
            aria-label="Sort results"
            className="w-[12rem]"
          />
        </div>
      </div>

      <div className="mt-8 grid gap-10 lg:grid-cols-[15rem_1fr]">
        <FacetRail
          facets={search.facets}
          filters={filters}
          onChange={setFilters}
          onClear={clear}
          activeCount={activeCount}
        />

        <div>
          {search.isLoading ? (
            <div className="grid gap-5 sm:grid-cols-2 xl:grid-cols-3">
              {Array.from({ length: 6 }, (_, i) => (
                <Skeleton key={i} className="h-[19rem] w-full" />
              ))}
            </div>
          ) : search.isError ? (
            <EmptyState
              title="Search did not respond"
              body="The catalogue is unreachable right now. Retry, or clear your filters and try a broader search."
              action={
                <Button variant="outline" onClick={search.refetch}>
                  Retry
                </Button>
              }
            />
          ) : search.items.length === 0 ? (
            <EmptyState
              title="No vehicles match these filters"
              body="Widen the price range or clear a filter to see more."
              action={
                activeCount > 0 ? (
                  <Button variant="outline" onClick={clear}>
                    Clear filters
                  </Button>
                ) : (
                  <Button variant="outline" asChild>
                    <Link href="/sell">Sell your vehicle instead</Link>
                  </Button>
                )
              }
            />
          ) : (
            <>
              <div className="grid gap-5 sm:grid-cols-2 xl:grid-cols-3">
                {search.items.map((item) => (
                  <VehicleCard
                    key={item.listingId}
                    href={`/buy/${item.listingId}`}
                    title={item.title}
                    registration={item.registration}
                    priceMinor={item.priceMinor}
                    priceLabel={PRICE_LABEL[item.intent]}
                    priceCompact
                    specs={item.specStrip}
                    media={{ ...item.media, alt: item.title }}
                    badges={item.badges.map((code) => (
                      <Badge key={code} variant="outline">
                        {badgeLabel(code)}
                      </Badge>
                    ))}
                    footer={
                      item.dealerName ? (
                        <span className="text-body-sm text-[color:var(--text-secondary)]">
                          {item.dealerName} · {item.city}
                        </span>
                      ) : (
                        <span className="text-body-sm text-[color:var(--text-secondary)]">
                          {item.city}
                        </span>
                      )
                    }
                  />
                ))}
              </div>

              {search.hasNextPage ? (
                <div className="mt-10 flex justify-center">
                  <Button
                    variant="outline"
                    size="lg"
                    onClick={search.fetchNextPage}
                    disabled={search.isFetchingNextPage}
                  >
                    {search.isFetchingNextPage ? 'Loading…' : 'Show more'}
                  </Button>
                </div>
              ) : null}
            </>
          )}
        </div>
      </div>
    </div>
  );
}

/**
 * Fallback labels until the i18n registry answers. The badge *code* is the contract; the word is
 * a translation, which is why this is a lookup and not a string baked into the card.
 */
function badgeLabel(code: string): string {
  switch (code) {
    case 'inspected':
      return 'Inspected';
    case 'verified_rc':
      return 'RC verified';
    case 'single_owner':
      return 'First owner';
    case 'accident_free':
      return 'No accidents';
    case 'warranty':
      return 'Under warranty';
    default:
      return code.replace(/_/g, ' ');
  }
}
