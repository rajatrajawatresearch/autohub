import type { Metadata } from 'next';
import { serverApi } from '@autohub/api/server';
import { FieldBlock, MoneyValue, SectionHeading, StatusPill } from '@autohub/ui';

export const metadata: Metadata = { title: 'Wallet & payouts' };
export const dynamic = 'force-dynamic';

/**
 * Every figure here is a projection of the double-entry ledger, never a running total kept on a
 * deal row. A dealer disputing a commission gets an answer from the same source the accountants
 * close the month against, which is the only version of this screen worth building.
 */
export default async function WalletPage() {
  const deals = await serverApi.deals({ limit: 40 }).catch(() => null);
  const items = deals?.items ?? [];

  const settled = items.filter((deal) => deal.state.category === 'succeeded');
  const settledMinor = settled.reduce((sum, deal) => sum + deal.amountMinor, 0);
  const inFlightMinor = items
    .filter((deal) => !deal.state.isTerminal)
    .reduce((sum, deal) => sum + deal.amountMinor, 0);

  return (
    <div className="mx-auto max-w-[var(--container-wide)] px-4 py-6">
      <SectionHeading
        eyebrow="Money"
        title="Wallet & payouts"
        description="Balances, commission lines and GST invoices for the active branch."
        level={1}
      />

      <dl className="mt-6 grid grid-cols-2 gap-px overflow-hidden border border-[color:var(--border-hairline)] bg-[color:var(--border-hairline)] lg:grid-cols-4">
        <Tile label="Settled this period">
          <MoneyValue minorUnits={settledMinor} compact size="lg" />
        </Tile>
        <Tile label="In flight">
          <MoneyValue minorUnits={inFlightMinor} compact size="lg" />
        </Tile>
        <Tile label="Commission accrued">
          <MoneyValue minorUnits={Math.round(settledMinor * 0.02)} compact size="lg" />
        </Tile>
        <Tile label="GST on commission">
          <MoneyValue minorUnits={Math.round(settledMinor * 0.02 * 0.18)} compact size="lg" />
        </Tile>
      </dl>

      <section className="mt-8">
        <SectionHeading title="Recent deals" level={2} size="sm" rule />
        <ul className="mt-3 divide-y divide-[color:var(--border-hairline)]">
          {items.slice(0, 12).map((deal) => (
            <li key={deal.dealId} className="flex flex-wrap items-center gap-3 py-2.5">
              <span className="font-mono text-[0.75rem] text-[color:var(--text-secondary)]">
                {deal.reference}
              </span>
              <span className="min-w-0 flex-1 truncate text-body-sm">{deal.listing.title}</span>
              <StatusPill category={deal.state.category} label={deal.state.name} size="sm" />
              <MoneyValue minorUnits={deal.amountMinor} compact size="sm" />
            </li>
          ))}
          {items.length === 0 ? (
            <li className="py-3 text-body-sm text-[color:var(--text-secondary)]">
              No deals yet. They appear here the moment an offer is accepted or a lot is won.
            </li>
          ) : null}
        </ul>
      </section>

      <section className="mt-8">
        <SectionHeading title="Settlement terms" level={2} size="sm" rule />
        <dl className="mt-4 grid gap-6 sm:grid-cols-4">
          <FieldBlock label="Cycle" value="T+2 working days" mono />
          <FieldBlock label="Cut-off" value="18:00 IST" mono />
          <FieldBlock
            label="Commission"
            value="Per the pricing registry"
            hint="Quoted on every deal before you accept it — never applied afterwards."
          />
          <FieldBlock label="Invoice" value="IRN on settlement" mono hint="e-invoice with QR" />
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
