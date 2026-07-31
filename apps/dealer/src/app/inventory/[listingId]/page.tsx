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

export const dynamic = 'force-dynamic';

type Params = { params: Promise<{ listingId: string }> };

export async function generateMetadata({ params }: Params): Promise<Metadata> {
  const { listingId } = await params;
  const listing = await serverApi.listing(listingId).catch(() => null);
  return { title: listing?.title ?? 'Vehicle not found' };
}

export default async function StockDetailPage({ params }: Params) {
  const { listingId } = await params;
  const listing = await serverApi.listing(listingId).catch(() => null);

  if (!listing) notFound();

  const workflow = listing.workflow
    ? await serverApi.workflowDefinition(listing.workflow.workflowKey).catch(() => null)
    : null;

  return (
    <div className="mx-auto max-w-[var(--container-wide)] px-4 py-6">
      <Link
        href="/inventory"
        className="text-body-sm text-[color:var(--text-secondary)] underline underline-offset-4"
      >
        Back to inventory
      </Link>

      <div className="mt-4 flex flex-wrap items-center gap-3">
        <PlateBlock
          registration={listing.registration.canonical}
          display={listing.registration.display}
          plateClass={listing.registration.plateClass}
          size="md"
        />
        {listing.workflow ? (
          <StatusPill
            category={listing.workflow.currentState.category}
            label={listing.workflow.currentState.name}
          />
        ) : null}
        {listing.badges.map((code) => (
          <Badge key={code} variant="outline" size="sm">
            {code.replace(/_/g, ' ')}
          </Badge>
        ))}
      </div>

      <h1 className="mt-3 font-display text-display-lg">{listing.title}</h1>

      <div className="mt-6 grid gap-8 lg:grid-cols-[1fr_20rem]">
        <div>
          <div className="border-y border-[color:var(--border-hairline)] py-4">
            <SpecStrip items={listing.specStrip} />
          </div>

          {listing.specGroups.map((group) => (
            <section key={group.code} className="mt-8">
              <SectionHeading title={group.label} level={2} size="sm" rule />
              <dl className="mt-4 grid grid-cols-2 gap-x-6 gap-y-4 sm:grid-cols-4">
                {group.items.map((item) => (
                  <FieldBlock key={item.label} label={item.label} value={item.value} mono />
                ))}
              </dl>
            </section>
          ))}

          {listing.workflow && workflow ? (
            <section className="mt-8">
              <SectionHeading
                title="History"
                description="Every state change, who made it, and how long it sat."
                level={2}
                size="sm"
                rule
              />
              <div className="mt-4">
                <WorkflowTimeline
                  entries={listing.workflow.history}
                  states={workflow.states}
                  order="newest_first"
                  empty="Nothing has happened to this vehicle yet."
                />
              </div>
            </section>
          ) : null}
        </div>

        <aside className="lg:sticky lg:top-6 lg:self-start">
          <div className="rounded-[var(--radius-card)] border border-[color:var(--border-hairline)] bg-[color:var(--surface-card)] p-4">
            <p className="ah-label">Asking price</p>
            {typeof listing.priceMinor === 'number' ? (
              <MoneyValue minorUnits={listing.priceMinor} size="lg" className="mt-1 block" />
            ) : (
              <p className="mt-1 font-display text-subheading">On request</p>
            )}

            <dl className="mt-4 grid grid-cols-2 gap-4">
              <FieldBlock label="Views" value={String(listing.viewCount ?? 0)} mono />
              <FieldBlock label="City" value={listing.city} mono />
            </dl>

            <div className="mt-4 flex flex-col gap-2">
              <Button size="sm">Edit price</Button>
              <Button variant="outline" size="sm">
                Send to auction
              </Button>
            </div>

            {/* Available actions come from the workflow snapshot — guards and permissions were
                already evaluated server-side, so this list is exactly what will work. */}
            {listing.workflow && listing.workflow.availableActions.length > 0 ? (
              <div className="mt-4 border-t border-[color:var(--border-hairline)] pt-3">
                <p className="ah-label">Available now</p>
                <ul className="mt-2 space-y-1.5">
                  {listing.workflow.availableActions.map((action) => (
                    <li key={action.actionCode}>
                      <Button variant="ghost" size="sm" className="w-full justify-start">
                        {action.label}
                      </Button>
                    </li>
                  ))}
                </ul>
              </div>
            ) : null}
          </div>
        </aside>
      </div>
    </div>
  );
}
