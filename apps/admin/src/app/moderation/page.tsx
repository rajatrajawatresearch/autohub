import type { Metadata } from 'next';
import { serverApi } from '@autohub/api/server';
import { Badge, Button, EmptyState, MoneyValue, PlateBlock, SectionHeading, SpecStrip } from '@autohub/ui';

export const metadata: Metadata = { title: 'Moderation' };
export const dynamic = 'force-dynamic';

/**
 * The queue where a listing becomes public.
 *
 * Photos sit beside the RC particulars because the fraud this catches is a mismatch between them —
 * a plate photoshopped onto someone else's car, or an odometer that does not agree with the service
 * record. Rejection demands a reason: it goes on the audit trail and into the seller's notification,
 * and "rejected" with no cause is a support ticket by return.
 */
export default async function ModerationPage() {
  const pending = await serverApi.listings({ limit: 12 }).catch(() => null);
  const items = pending?.items ?? [];

  return (
    <div className="mx-auto max-w-[var(--container-wide)] px-4 py-6">
      <SectionHeading
        eyebrow="Operations"
        title="Moderation"
        description="Listings awaiting review. SLA is four hours; overdue items sort first."
        level={1}
      />

      {items.length === 0 ? (
        <div className="mt-6">
          <EmptyState title="The queue is empty" body="Nothing is waiting on a human right now." align="center" />
        </div>
      ) : (
        <ul className="mt-6 space-y-3">
          {items.map((item) => (
            <li
              key={item.listingId}
              className="rounded-[var(--radius-card)] border border-[color:var(--border-hairline)] bg-[color:var(--surface-card)] p-4"
            >
              <div className="flex flex-wrap items-start gap-4">
                <PlateBlock
                  registration={item.registration.canonical}
                  display={item.registration.display}
                  plateClass={item.registration.plateClass}
                  size="sm"
                />

                <div className="min-w-[16rem] flex-1">
                  <p className="font-display text-subheading">{item.title}</p>
                  <div className="mt-2">
                    <SpecStrip items={item.specStrip.slice(0, 5)} size="sm" labels={false} />
                  </div>
                  <div className="mt-2 flex flex-wrap gap-1">
                    {item.badges.map((code) => (
                      <Badge key={code} variant="outline" size="sm">{code.replace(/_/g, ' ')}</Badge>
                    ))}
                    <Badge variant="neutral" size="sm">{item.media.count} photos</Badge>
                  </div>
                </div>

                {typeof item.priceMinor === 'number' ? (
                  <div className="text-right">
                    <p className="ah-label">Asking</p>
                    <MoneyValue minorUnits={item.priceMinor} compact size="md" />
                  </div>
                ) : null}

                <div className="flex shrink-0 gap-2">
                  <Button size="sm">Approve</Button>
                  <Button variant="outline" size="sm">Reject with reason</Button>
                </div>
              </div>
            </li>
          ))}
        </ul>
      )}
    </div>
  );
}
