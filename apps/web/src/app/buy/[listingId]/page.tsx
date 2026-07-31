import type { Metadata } from 'next';
import { notFound } from 'next/navigation';
import Link from 'next/link';
import { serverApi } from '@autohub/api/server';
import {
  Badge,
  Button,
  FieldBlock,
  MoneyValue,
  PlateBlock,
  SectionHeading,
  SpecStrip,
  StatusPill,
  WorkflowTimeline,
} from '@autohub/ui';
import { Gallery } from '@/components/listing/gallery';

export const revalidate = 60;

type Params = { params: Promise<{ listingId: string }> };

export async function generateMetadata({ params }: Params): Promise<Metadata> {
  const { listingId } = await params;
  const listing = await serverApi.listing(listingId).catch(() => null);

  if (!listing) return { title: 'Vehicle not found' };

  return {
    title: listing.title,
    description:
      listing.description ??
      `${listing.title} in ${listing.city}. RC verified, inspected, escrow protected.`,
    openGraph: { images: listing.media.coverUrl ? [listing.media.coverUrl] : [] },
  };
}

export default async function ListingPage({ params }: Params) {
  const { listingId } = await params;
  const listing = await serverApi.listing(listingId).catch(() => null);

  if (!listing) notFound();

  // The timeline turns state codes into names, and the names live in the workflow registry — not
  // in the snapshot. Fetching the definition alongside is what keeps this page from hardcoding
  // "Under review" next to "pending_review".
  const workflow = listing.workflow
    ? await serverApi.workflowDefinition(listing.workflow.workflowKey).catch(() => null)
    : null;

  return (
    <article className="mx-auto max-w-[var(--container-content)] px-4 py-8 sm:px-6">
      <div className="flex flex-wrap items-center gap-3">
        <PlateBlock
          registration={listing.registration.canonical}
          display={listing.registration.display}
          plateClass={listing.registration.plateClass}
          size="lg"
        />
        {listing.workflow ? (
          <StatusPill
            category={listing.workflow.currentState.category}
            label={listing.workflow.currentState.name}
          />
        ) : null}
        {listing.badges.map((code) => (
          <Badge key={code} variant="outline">
            {code.replace(/_/g, ' ')}
          </Badge>
        ))}
      </div>

      <h1 className="mt-5 font-display text-display-lg text-[color:var(--text-primary)]">
        {listing.title}
      </h1>

      <p className="mt-2 text-body-sm text-[color:var(--text-secondary)]">
        {[listing.locality, listing.city].filter(Boolean).join(' · ')}
        {listing.viewCount ? ` · ${listing.viewCount} views` : ''}
      </p>

      <div className="mt-8 grid gap-10 lg:grid-cols-[1fr_20rem]">
        <div>
          <Gallery
            assets={listing.gallery}
            cover={listing.media.coverUrl}
            aspectRatio={listing.media.aspectRatio}
            alt={listing.title}
          />

          <div className="mt-8 border-y border-[color:var(--border-hairline)] py-5">
            <SpecStrip items={listing.specStrip} />
          </div>

          {listing.description ? (
            <div className="mt-8">
              <SectionHeading title="About this vehicle" level={2} size="sm" />
              <p className="mt-3 max-w-[68ch] whitespace-pre-line text-body text-[color:var(--text-primary)]">
                {listing.description}
              </p>
            </div>
          ) : null}

          {listing.specGroups.map((group) => (
            <section key={group.code} className="mt-10">
              <SectionHeading title={group.label} level={2} size="sm" rule />
              <dl className="mt-5 grid grid-cols-2 gap-x-6 gap-y-5 sm:grid-cols-3">
                {group.items.map((item) => (
                  <FieldBlock key={item.label} label={item.label} value={item.value} mono />
                ))}
              </dl>
            </section>
          ))}

          {listing.workflow && workflow ? (
            <section className="mt-10">
              <SectionHeading
                title="Listing history"
                description="Every state change on this listing, and who made it."
                level={2}
                size="sm"
                rule
              />
              <div className="mt-5">
                <WorkflowTimeline
                  entries={listing.workflow.history}
                  states={workflow.states}
                  order="newest_first"
                  empty="Nothing has happened to this listing yet."
                />
              </div>
            </section>
          ) : null}
        </div>

        <aside className="lg:sticky lg:top-24 lg:self-start">
          <div className="rounded-[var(--radius-card)] border border-[color:var(--border-hairline)] bg-[color:var(--surface-card)] p-5">
            <p className="ah-label">Asking price</p>
            {typeof listing.priceMinor === 'number' ? (
              <MoneyValue minorUnits={listing.priceMinor} size="xl" className="mt-1 block" />
            ) : (
              <p className="mt-1 font-display text-heading">Price on request</p>
            )}
            {typeof listing.priceMinor === 'number' ? (
              <p className="mt-1 font-mono text-[0.75rem] text-[color:var(--text-secondary)]">
                <MoneyValue minorUnits={listing.priceMinor} compact size="xs" tone="secondary" /> ·
                EMI from{' '}
                <MoneyValue
                  minorUnits={estimateMonthlyEmiMinor(listing.priceMinor)}
                  size="xs"
                  tone="secondary"
                />
                /mo
              </p>
            ) : null}

            {/* One rationed yellow action per viewport. Booking a test drive is the one that matters. */}
            <div className="mt-5 flex flex-col gap-2">
              <Button variant="action" size="lg">
                Book test drive
              </Button>
              <Button variant="outline" size="lg">
                Make an offer
              </Button>
            </div>

            <p className="mt-4 text-body-sm text-[color:var(--text-secondary)]">
              Payment is held in escrow and released to the seller only after the RC transfer is filed.
            </p>
          </div>

          {listing.seller ? (
            <div className="mt-5 rounded-[var(--radius-card)] border border-[color:var(--border-hairline)] p-5">
              <p className="ah-label">Seller</p>
              <p className="mt-1 font-display text-subheading">{listing.seller.displayName}</p>
              <p className="text-body-sm text-[color:var(--text-secondary)]">
                {listing.seller.type === 'dealer' ? 'Verified dealer' : 'Individual owner'}
                {listing.seller.city ? ` · ${listing.seller.city}` : ''}
              </p>
              <dl className="mt-4 grid grid-cols-2 gap-4">
                {typeof listing.seller.listingsCount === 'number' ? (
                  <FieldBlock label="Listings" value={String(listing.seller.listingsCount)} mono />
                ) : null}
                {typeof listing.seller.responseTimeMinutes === 'number' ? (
                  <FieldBlock
                    label="Replies in"
                    value={`${listing.seller.responseTimeMinutes} min`}
                    mono
                  />
                ) : null}
              </dl>
            </div>
          ) : null}

          <Link
            href="/buy"
            className="mt-5 inline-block text-body-sm text-[color:var(--accent-primary)] underline underline-offset-4"
          >
            Back to all vehicles
          </Link>
        </aside>
      </div>
    </article>
  );
}

/**
 * An indicative EMI, not an offer.
 *
 * Deliberately a rough on-page hint at a nominal 9.5% over 5 years with 20% down — the real number
 * comes from the loan marketplace once a lender has run eligibility, and this is labelled "from"
 * so it never reads as a quote. Anything more precise here would be a promise we cannot keep.
 */
function estimateMonthlyEmiMinor(priceMinor: number): number {
  const principal = priceMinor * 0.8;
  const monthlyRate = 0.095 / 12;
  const months = 60;
  const factor = (1 + monthlyRate) ** months;
  return Math.round((principal * monthlyRate * factor) / (factor - 1));
}
