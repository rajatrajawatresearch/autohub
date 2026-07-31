import type { Metadata } from 'next';
import Link from 'next/link';
import { serverApi } from '@autohub/api/server';
import { Button, FieldBlock, SectionHeading, VehicleCard } from '@autohub/ui';

export const metadata: Metadata = {
  title: 'Taxi Attachment Centre',
  description:
    'Attach your taxi to a fleet contract, or find vehicles for your fleet. Monthly payouts, commission handled.',
};

export const revalidate = 60;

/**
 * The attachment centre is a two-sided matching market, not a listings page, so it opens by asking
 * which side you are on. Vehicle owners have supply; fleet operators, travel companies and
 * corporates have demand. The platform matches them and takes a monthly commission.
 *
 * This is the module that has no equivalent on Cars24 or Spinny, and the one a fleet operator will
 * judge the product by — so it states the commercial terms up front rather than burying them.
 */
export default async function AttachPage() {
  const supply = await serverApi.search({ intent: 'attachment', limit: 6 }).catch(() => null);

  return (
    <>
      <section className="border-b border-[color:var(--border-hairline)]">
        <div className="mx-auto max-w-[var(--container-content)] px-4 py-16 sm:px-6">
          <div className="max-w-[46rem]">
            <p className="ah-label">Taxi Attachment Centre</p>
            <h1 className="mt-4 font-display text-display-lg text-[color:var(--text-primary)]">
              Put your taxi on a fleet contract
            </h1>
            <p className="mt-4 max-w-[54ch] text-body text-[color:var(--text-secondary)]">
              Fleet operators, travel companies and corporates post what they need. Vehicle owners
              post what they have. We match, paper the contract, and settle the monthly payout.
            </p>
          </div>

          <div className="mt-10 grid gap-px overflow-hidden border border-[color:var(--border-hairline)] bg-[color:var(--border-hairline)] sm:grid-cols-2">
            <SideCard
              eyebrow="I have a vehicle"
              title="Earn a fixed monthly rate"
              body="List your taxi with its permit, driver and available routes. Get matched with operators who need it."
              points={['Monthly payout, not per trip', 'Driver documents verified', 'Contract and renewal handled']}
              cta="List my vehicle"
              href="/sell"
              primary
            />
            <SideCard
              eyebrow="I need vehicles"
              title="Fill a fleet requirement"
              body="Post the count, duration and rate you need. See matched vehicles with permits and driver compliance already checked."
              points={['Airport and corporate contracts', 'Permit and fitness verified', 'One invoice for the fleet']}
              cta="Post a requirement"
              href="/buy"
            />
          </div>
        </div>
      </section>

      <section className="mx-auto max-w-[var(--container-content)] px-4 py-16 sm:px-6">
        <SectionHeading
          eyebrow="Available now"
          title="Vehicles looking for a contract"
          description="Permit, fitness and driver documents checked before a vehicle appears here."
          rule
        />

        {supply && supply.items.length > 0 ? (
          <div className="mt-8 grid gap-5 sm:grid-cols-2 lg:grid-cols-3">
            {supply.items.map((item) => (
              <VehicleCard
                key={item.listingId}
                href={`/buy/${item.listingId}`}
                title={item.title}
                registration={item.registration}
                priceMinor={item.priceMinor}
                priceLabel="Per month"
                priceCompact
                specs={item.specStrip}
                media={{ ...item.media, alt: item.title }}
                location={item.city}
              />
            ))}
          </div>
        ) : (
          <p className="mt-8 text-body-sm text-[color:var(--text-secondary)]">
            No vehicles are seeking a contract in this city yet. Post a requirement and we will
            match one as soon as it lists.
          </p>
        )}
      </section>

      <section className="border-t border-[color:var(--border-hairline)] bg-[color:var(--surface-sunken)]">
        <div className="mx-auto max-w-[var(--container-content)] px-4 py-12 sm:px-6">
          <dl className="grid gap-8 sm:grid-cols-3">
            <FieldBlock
              label="Commission"
              value="On the monthly rate"
              hint="Charged to the operator, shown on every invoice before you accept a match."
            />
            <FieldBlock
              label="Settlement"
              value="Monthly, T+2"
              hint="Paid to your registered bank account two working days after the operator settles."
            />
            <FieldBlock
              label="Compliance"
              value="Permit · fitness · DL"
              hint="Checked at listing and re-checked at renewal. An expired document pauses the contract."
            />
          </dl>
        </div>
      </section>
    </>
  );
}

function SideCard({
  eyebrow,
  title,
  body,
  points,
  cta,
  href,
  primary = false,
}: {
  eyebrow: string;
  title: string;
  body: string;
  points: string[];
  cta: string;
  href: '/sell' | '/buy';
  primary?: boolean;
}) {
  return (
    <div className="bg-[color:var(--surface-card)] p-8">
      <p className="ah-label">{eyebrow}</p>
      <h2 className="mt-3 font-display text-heading text-[color:var(--text-primary)]">{title}</h2>
      <p className="mt-3 max-w-[42ch] text-body-sm text-[color:var(--text-secondary)]">{body}</p>

      <ul className="mt-5 space-y-2">
        {points.map((point) => (
          <li key={point} className="flex gap-2.5 text-body-sm">
            <span
              aria-hidden="true"
              className="mt-[0.55em] h-px w-3 shrink-0 bg-[color:var(--accent-primary)]"
            />
            {point}
          </li>
        ))}
      </ul>

      <Button variant={primary ? 'action' : 'outline'} size="lg" className="mt-7" asChild>
        <Link href={href}>{cta}</Link>
      </Button>
    </div>
  );
}
