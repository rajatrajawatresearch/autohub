import type { Metadata } from 'next';
import { Suspense } from 'react';
import { SectionHeading, Skeleton } from '@autohub/ui';
import { AuctionFloor } from '@/components/auction/auction-floor';

export const metadata: Metadata = { title: 'Auction floor' };

export default function AuctionPage() {
  return (
    <div className="mx-auto max-w-[var(--container-wide)] px-4 py-6">
      <SectionHeading
        eyebrow="Trade"
        title="Auction floor"
        description="Dealer-only. Bids are final and the clock extends if one lands in the last thirty seconds."
        level={1}
      />
      <div className="mt-6">
        <Suspense
          fallback={
            <div className="grid gap-4 sm:grid-cols-2 xl:grid-cols-3">
              {Array.from({ length: 6 }, (_, i) => (
                <Skeleton key={i} className="h-64 w-full" />
              ))}
            </div>
          }
        >
          <AuctionFloor />
        </Suspense>
      </div>
    </div>
  );
}
