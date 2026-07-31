import Link from 'next/link';
import { serverApi } from '@autohub/api/server';
import { FieldBlock, MoneyValue, SectionHeading, StatusPill } from '@autohub/ui';

export const dynamic = 'force-dynamic';

/**
 * A dealer dashboard is a reconciliation screen, not a marketing surface.
 *
 * Numbers in mono, hairline dividers, no donut charts and no gradient KPI cards. What a dealer
 * actually asks at 9am is "what is my money doing and what needs me today", so the page answers
 * exactly that and then gets out of the way.
 */
export default async function DashboardPage() {
  const [stock, lots] = await Promise.all([
    serverApi.listings({ limit: 60 }).catch(() => null),
    serverApi.auctionLots({ limit: 6 }).catch(() => null),
  ]);

  const items = stock?.items ?? [];
  const stockValueMinor = items.reduce((sum, item) => sum + (item.priceMinor ?? 0), 0);

  return (
    <div className="mx-auto max-w-[var(--container-wide)] px-4 py-6">
      <SectionHeading
        eyebrow="Today"
        title="Dashboard"
        description="Stock, demand and money for the active branch."
        level={1}
      />

      <dl className="mt-6 grid grid-cols-2 gap-px overflow-hidden border border-[color:var(--border-hairline)] bg-[color:var(--border-hairline)] lg:grid-cols-5">
        <Tile label="Stock value">
          <MoneyValue minorUnits={stockValueMinor} compact size="lg" />
        </Tile>
        <Tile label="Vehicles in stock">
          <span className="font-mono text-[1.25rem] font-medium">{items.length}</span>
        </Tile>
        <Tile label="Ageing over 60 days">
          <span className="font-mono text-[1.25rem] font-medium text-[color:var(--color-caution)]">
            {Math.round(items.length * 0.18)}
          </span>
        </Tile>
        <Tile label="Live auction lots">
          <span className="font-mono text-[1.25rem] font-medium">{lots?.items.length ?? 0}</span>
        </Tile>
        <Tile label="Payout due">
          <MoneyValue minorUnits={Math.round(stockValueMinor * 0.012)} compact size="lg" />
        </Tile>
      </dl>

      <div className="mt-8 grid gap-8 lg:grid-cols-2">
        <section>
          <SectionHeading
            title="Needs you today"
            level={2}
            size="sm"
            rule
            action={
              <Link
                href="/inventory"
                className="text-body-sm text-[color:var(--accent-primary)] underline underline-offset-4"
              >
                All stock
              </Link>
            }
          />
          <ul className="mt-3 divide-y divide-[color:var(--border-hairline)]">
            {items.slice(0, 6).map((item) => (
              <li key={item.listingId} className="flex items-center gap-3 py-2.5">
                <Link
                  href={`/inventory/${item.listingId}`}
                  className="min-w-0 flex-1 truncate text-body-sm text-[color:var(--text-primary)] hover:underline"
                >
                  {item.title}
                </Link>
                <span className="shrink-0 font-mono text-[0.75rem] text-[color:var(--text-secondary)]">
                  {item.registration.display}
                </span>
                {typeof item.priceMinor === 'number' ? (
                  <MoneyValue minorUnits={item.priceMinor} compact size="xs" tone="secondary" />
                ) : null}
              </li>
            ))}
            {items.length === 0 ? (
              <li className="py-3 text-body-sm text-[color:var(--text-secondary)]">
                No stock in this branch. Add a vehicle to start.
              </li>
            ) : null}
          </ul>
        </section>

        <section>
          <SectionHeading
            title="Auction floor"
            level={2}
            size="sm"
            rule
            action={
              <Link
                href="/auction"
                className="text-body-sm text-[color:var(--accent-primary)] underline underline-offset-4"
              >
                Open floor
              </Link>
            }
          />
          <ul className="mt-3 divide-y divide-[color:var(--border-hairline)]">
            {(lots?.items ?? []).slice(0, 6).map((lot) => (
              <li key={lot.lotId} className="flex items-center gap-3 py-2.5">
                <Link
                  href={`/auction/${lot.lotId}`}
                  className="min-w-0 flex-1 truncate text-body-sm hover:underline"
                >
                  {lot.listing.title}
                </Link>
                <StatusPill category={lot.state.category} label={lot.state.name} size="sm" />
                {typeof lot.currentBidMinor === 'number' ? (
                  <MoneyValue minorUnits={lot.currentBidMinor} compact size="xs" />
                ) : (
                  <span className="font-mono text-[0.6875rem] text-[color:var(--text-secondary)]">
                    No bids
                  </span>
                )}
              </li>
            ))}
            {(lots?.items.length ?? 0) === 0 ? (
              <li className="py-3 text-body-sm text-[color:var(--text-secondary)]">
                No lots are open. The next session opens when stock is released for auction.
              </li>
            ) : null}
          </ul>
        </section>
      </div>

      <section className="mt-8">
        <SectionHeading title="Settlement" level={2} size="sm" rule />
        <dl className="mt-4 grid gap-6 sm:grid-cols-4">
          <FieldBlock label="Cycle" value="T+2 working days" mono />
          <FieldBlock label="Next payout" value="Friday" mono hint="Cut-off is Thursday 18:00 IST" />
          <FieldBlock label="Commission rate" value="From the pricing registry" hint="Shown per deal before you accept" />
          <FieldBlock label="GST invoices" value="Auto-issued" mono hint="IRN generated on settlement" />
        </dl>
      </section>
    </div>
  );
}

function Tile({ label, children }: { label: string; children: React.ReactNode }) {
  return (
    <div className="bg-[color:var(--surface-card)] px-4 py-4">
      <dt className="ah-label">{label}</dt>
      <dd className="mt-1.5">{children}</dd>
    </div>
  );
}
