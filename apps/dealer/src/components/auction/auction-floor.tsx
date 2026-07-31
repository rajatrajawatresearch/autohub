'use client';

import Link from 'next/link';
import { useAuctionLots } from '@autohub/api';
import {
  BidTicket,
  Button,
  EmptyState,
  PlateBlock,
  SpecStrip,
  Skeleton,
  StatusPill,
} from '@autohub/ui';

/**
 * The floor is a wall of live lots. It is the one screen in the product where the rationed yellow
 * is loud, because everything on it is time-boxed and irreversible.
 *
 * Bidding itself happens on the lot page behind a confirm step, not from a card — a mis-click on a
 * grid of six countdowns is an expensive way to buy a car.
 */
export function AuctionFloor() {
  const lots = useAuctionLots({ limit: 24 });

  if (lots.isLoading) {
    return (
      <div className="grid gap-4 sm:grid-cols-2 xl:grid-cols-3">
        {Array.from({ length: 6 }, (_, i) => (
          <Skeleton key={i} className="h-64 w-full" />
        ))}
      </div>
    );
  }

  if (lots.isError) {
    return (
      <EmptyState
        title="The floor did not load"
        body="Do not bid from a stale screen. Retry, and check the clock before you place anything."
        action={
          <Button variant="outline" onClick={() => lots.refetch()}>
            Retry
          </Button>
        }
      />
    );
  }

  const items = lots.data?.items ?? [];

  if (items.length === 0) {
    return (
      <EmptyState
        title="No lots are open"
        body="The next session opens when stock is released for auction. You will be notified."
        align="center"
      />
    );
  }

  return (
    <div className="grid gap-4 sm:grid-cols-2 xl:grid-cols-3">
      {items.map((lot) => (
        <article
          key={lot.lotId}
          className="flex flex-col rounded-[var(--radius-card)] border border-[color:var(--border-hairline)] bg-[color:var(--surface-card)] p-4"
        >
          <div className="flex items-start justify-between gap-2">
            <PlateBlock
              registration={lot.listing.registration.canonical}
              display={lot.listing.registration.display}
              plateClass={lot.listing.registration.plateClass}
              size="xs"
            />
            <StatusPill category={lot.state.category} label={lot.state.name} size="sm" />
          </div>

          <h2 className="mt-3 truncate font-display text-subheading text-[color:var(--text-primary)]">
            {lot.listing.title}
          </h2>

          <div className="mt-2">
            <SpecStrip items={lot.listing.specStrip.slice(0, 4)} size="sm" labels={false} />
          </div>

          <div className="mt-4">
            <BidTicket
              currentBidMinor={lot.currentBidMinor}
              endsAt={lot.endsAt}
              bidCount={lot.bidCount}
              position={
                lot.highestBidderIsYou ? 'leading' : lot.bidCount > 0 ? 'outbid' : 'none'
              }
              yourMaxMinor={lot.yourMaxBidMinor}
              extended={lot.extensionCount > 0}
            >
              {/* The ticket already spends the viewport's yellow, so this stays green. */}
              <Button size="sm" className="w-full" asChild>
                <Link href={`/auction/${lot.lotId}`}>Open lot</Link>
              </Button>
            </BidTicket>
          </div>

          <p className="mt-3 font-mono text-[0.6875rem] text-[color:var(--text-secondary)]">
            {lot.reserveMet ? 'Reserve met' : 'Reserve not met'}
            {lot.lotNumber ? ` · Lot ${lot.lotNumber}` : ''}
          </p>
        </article>
      ))}
    </div>
  );
}
