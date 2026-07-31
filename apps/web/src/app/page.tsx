import Link from 'next/link';
import { serverApi } from '@autohub/api/server';
import { FieldBlock, SectionHeading, VehicleCard } from '@autohub/ui';
import { ValuationHero } from '@/components/home/valuation-hero';

export const revalidate = 60;

/**
 * The home page is disciplined on purpose.
 *
 * Hero, four intents, live stock, three facts. No testimonial carousel, no gradient stats band,
 * no logo wall — every one of those competes with the plate input, which is the only thing on this
 * page that makes the company money.
 */
export default async function HomePage() {
  const stock = await serverApi.search({ intent: 'sale', limit: 8 }).catch(() => null);

  return (
    <>
      <ValuationHero />

      <IntentTiles />

      <section className="mx-auto max-w-[var(--container-content)] px-4 py-16 sm:px-6">
        <SectionHeading
          eyebrow="Live stock"
          title="Inspected and ready to view"
          description="Every vehicle here has a verified RC and a 200-point inspection report."
          action={
            <Link
              href="/buy"
              className="text-body-sm font-medium text-[color:var(--accent-primary)] underline underline-offset-4"
            >
              See all
            </Link>
          }
          rule
        />

        {stock && stock.items.length > 0 ? (
          <div className="mt-8 grid gap-5 sm:grid-cols-2 lg:grid-cols-4">
            {stock.items.map((item) => (
              <VehicleCard
                key={item.listingId}
                href={`/buy/${item.listingId}`}
                title={item.title}
                registration={item.registration}
                priceMinor={item.priceMinor}
                priceLabel="Asking price"
                priceCompact
                specs={item.specStrip}
                media={{ ...item.media, alt: item.title }}
              />
            ))}
          </div>
        ) : (
          <p className="mt-8 text-body-sm text-[color:var(--text-secondary)]">
            Stock for your city is loading. Pick a different city from the header if this stays empty.
          </p>
        )}
      </section>

      <TrustRow />
    </>
  );
}

const INTENTS = [
  { href: '/buy', label: 'Buy', line: 'Inspected stock from verified dealers and owners.' },
  { href: '/sell', label: 'Sell', line: 'One form, then a dealer auction or an instant offer.' },
  { href: '/rent', label: 'Rent', line: 'Self-drive or with a driver, by the day or the month.' },
  { href: '/attach', label: 'Attach', line: 'Put your taxi on a fleet contract and get paid monthly.' },
] as const;

function IntentTiles() {
  return (
    <section className="border-b border-[color:var(--border-hairline)]">
      <div className="mx-auto grid max-w-[var(--container-content)] grid-cols-1 sm:grid-cols-2 lg:grid-cols-4">
        {INTENTS.map((intent, index) => (
          <Link
            key={intent.href}
            href={intent.href}
            className={[
              'group px-6 py-10 transition-colors hover:bg-[color:var(--surface-sunken)]',
              // Hairlines between tiles, not around them. Structure comes from rules.
              index > 0 ? 'border-t border-[color:var(--border-hairline)] sm:border-t-0' : '',
              index % 2 === 1 ? 'sm:border-l sm:border-[color:var(--border-hairline)]' : '',
              index >= 2 ? 'sm:border-t sm:border-[color:var(--border-hairline)] lg:border-t-0' : '',
              index > 0 ? 'lg:border-l lg:border-[color:var(--border-hairline)]' : '',
            ].join(' ')}
          >
            <p className="ah-label">0{index + 1}</p>
            <p className="mt-3 font-display text-heading text-[color:var(--text-primary)]">
              {intent.label}
            </p>
            <p className="mt-2 max-w-[28ch] text-body-sm text-[color:var(--text-secondary)]">
              {intent.line}
            </p>
            <span
              aria-hidden="true"
              className="mt-4 inline-block h-0.5 w-6 bg-[color:var(--accent-primary)] transition-all group-hover:w-10"
            />
          </Link>
        ))}
      </div>
    </section>
  );
}

function TrustRow() {
  return (
    <section className="border-t border-[color:var(--border-hairline)] bg-[color:var(--surface-sunken)]">
      <div className="mx-auto max-w-[var(--container-content)] px-4 py-12 sm:px-6">
        <dl className="grid gap-8 sm:grid-cols-3">
          <FieldBlock
            label="Ownership"
            value="RC verified against VAHAN"
            hint="Hypothecation, challans and blacklist status checked before a listing goes live."
          />
          <FieldBlock
            label="Condition"
            value="200-point inspection"
            hint="Engine, body, tyres, electricals and an OBD scan, photographed and graded."
          />
          <FieldBlock
            label="Payment"
            value="Escrow protected"
            hint="Money is released to the seller only after the transfer is filed."
          />
        </dl>
      </div>
    </section>
  );
}
