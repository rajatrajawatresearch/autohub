import type { Metadata } from 'next';
import { notFound } from 'next/navigation';
import Link from 'next/link';
import { serverApi } from '@autohub/api/server';
import { FieldBlock, MoneyValue, PlateBlock, SectionHeading, SpecStrip, StatusPill } from '@autohub/ui';
import { BidPanel } from '@/components/auction/bid-panel';

export const dynamic = 'force-dynamic';

type Params = { params: Promise<{ lotId: string }> };

export async function generateMetadata({ params }: Params): Promise<Metadata> {
  const { lotId } = await params;
  const lot = await serverApi.auctionLot(lotId).catch(() => null);
  return { title: lot ? `Lot ${lot.lotNumber ?? ''} · ${lot.listing.title}` : 'Lot not found' };
}

export default async function LotPage({ params }: Params) {
  const { lotId } = await params;
  const lot = await serverApi.auctionLot(lotId).catch(() => null);

  if (!lot) notFound();

  return (
    <div className="mx-auto max-w-[var(--container-wide)] px-4 py-6">
      <Link
        href="/auction"
        className="text-body-sm text-[color:var(--text-secondary)] underline underline-offset-4"
      >
        Back to floor
      </Link>

      <div className="mt-4 flex flex-wrap items-center gap-3">
        <PlateBlock
          registration={lot.listing.registration.canonical}
          display={lot.listing.registration.display}
          plateClass={lot.listing.registration.plateClass}
          size="md"
        />
        <StatusPill category={lot.state.category} label={lot.state.name} />
        {lot.lotNumber ? (
          <span className="font-mono text-body-sm text-[color:var(--text-secondary)]">
            Lot {lot.lotNumber}
          </span>
        ) : null}
      </div>

      <h1 className="mt-3 font-display text-display-lg">{lot.listing.title}</h1>

      <div className="mt-6 grid gap-8 lg:grid-cols-[1fr_22rem]">
        <div>
          <div className="border-y border-[color:var(--border-hairline)] py-4">
            <SpecStrip items={lot.listing.specStrip} />
          </div>

          <section className="mt-8">
            <SectionHeading title="Lot terms" level={2} size="sm" rule />
            <dl className="mt-4 grid gap-6 sm:grid-cols-3">
              <FieldBlock
                label="Start price"
                value={<MoneyValue minorUnits={lot.startPriceMinor} compact size="md" />}
                mono
              />
              <FieldBlock
                label="Increment"
                value={<MoneyValue minorUnits={lot.incrementMinor} compact size="md" />}
                mono
                hint="The ladder step. A bid must clear the current high by at least this."
              />
              <FieldBlock
                label="Reserve"
                value={lot.reserveMet ? 'Met' : 'Not met'}
                mono
                hint="The reserve amount itself is never shown to bidders."
              />
              <FieldBlock label="Bids so far" value={String(lot.bidCount)} mono />
              <FieldBlock
                label="Extensions"
                value={String(lot.extensionCount)}
                mono
                hint="A bid inside the final window pushes the close time out."
              />
              <FieldBlock
                label="Inspection"
                value={
                  lot.inspectionReportUrl ? (
                    <a
                      href={lot.inspectionReportUrl}
                      className="text-[color:var(--accent-primary)] underline underline-offset-4"
                    >
                      Open report
                    </a>
                  ) : (
                    'Not available'
                  )
                }
              />
            </dl>
          </section>

          <p className="mt-8 max-w-[64ch] text-body-sm text-[color:var(--text-secondary)]">
            Bids are binding. Winning a lot opens a deal with a payment window; missing it forfeits
            the deposit and the lot is relisted. The server clock is authoritative — your screen may
            be a second behind it.
          </p>
        </div>

        <aside className="lg:sticky lg:top-6 lg:self-start">
          <BidPanel lotId={lot.lotId} />
        </aside>
      </div>
    </div>
  );
}
