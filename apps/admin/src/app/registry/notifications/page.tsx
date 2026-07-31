import type { Metadata } from 'next';
import { Badge, SectionHeading } from '@autohub/ui';
import { RegistryPage } from '@/components/registry/registry-page';

export const metadata: Metadata = { title: 'Notifications' };

/**
 * Event → audience → channel → template. All four are rows.
 *
 * SMS is the one channel that cannot be free text: TRAI's DLT regime requires every template to be
 * registered with a provider id before a message will deliver in India, so the registry holds that
 * id alongside the body and a template without one cannot be published to the SMS channel.
 */
export default function NotificationRegistryPage() {
  return (
    <RegistryPage
      eyebrow="Registry 7 of 10"
      title="Notifications"
      description="Templates per channel × locale × event, with quiet hours and DND honoured at dispatch."
      version={12}
      publishedBy="crm@autohub.in"
    >
      <SectionHeading title="Event map" level={2} size="sm" rule />
      <div className="mt-4 overflow-x-auto">
        <table className="w-full border-collapse text-body-sm">
          <thead>
            <tr className="border-b border-[color:var(--border-hairline)]">
              <th className="ah-label py-2 text-left">Event</th>
              <th className="ah-label py-2 text-left">Audience</th>
              <th className="ah-label py-2 text-left">Channels</th>
              <th className="ah-label py-2 text-left">Locales</th>
              <th className="ah-label py-2 text-left">DLT</th>
            </tr>
          </thead>
          <tbody className="divide-y divide-[color:var(--border-hairline)]">
            {EVENTS.map((event) => (
              <tr key={event.type}>
                <td className="py-2 pr-3 font-mono text-[0.75rem]">{event.type}</td>
                <td className="py-2 pr-3 text-[color:var(--text-secondary)]">{event.audience}</td>
                <td className="py-2 pr-3">
                  <span className="flex flex-wrap gap-1">
                    {event.channels.map((channel) => (
                      <Badge key={channel} variant="outline" size="sm">{channel}</Badge>
                    ))}
                  </span>
                </td>
                <td className="py-2 pr-3 font-mono text-[0.75rem]">{event.locales}</td>
                <td className="py-2">
                  {event.channels.includes('sms') ? (
                    <Badge variant={event.dlt ? 'clear' : 'flag'} size="sm">
                      {event.dlt ? 'registered' : 'missing'}
                    </Badge>
                  ) : (
                    <span className="text-[color:var(--text-secondary)]">n/a</span>
                  )}
                </td>
              </tr>
            ))}
          </tbody>
        </table>
      </div>

      <p className="mt-6 max-w-[68ch] text-body-sm text-[color:var(--text-secondary)]">
        Dispatch reads from the outbox, so a notification cannot be sent for a state change that was
        rolled back — and a state change cannot succeed with nobody told.
      </p>
    </RegistryPage>
  );
}

interface NotificationEventRow {
  type: string;
  audience: string;
  /** Typed as string[] rather than left to `as const`: a readonly tuple of literals makes
      `.includes('sms')` narrow its argument to `never`, which is the compiler being right about
      a shape we do not actually want here. */
  channels: readonly string[];
  locales: string;
  dlt: boolean;
}

const EVENTS: readonly NotificationEventRow[] = [
  { type: 'auction.outbid.v1', audience: 'Previous highest bidder', channels: ['push', 'whatsapp'], locales: 'en, hi, mr', dlt: true },
  { type: 'auction.lot_won.v1', audience: 'Winning dealer', channels: ['push', 'sms', 'email'], locales: 'en, hi', dlt: true },
  { type: 'listing.approved.v1', audience: 'Seller', channels: ['push', 'sms'], locales: 'en, hi, mr, ta', dlt: true },
  { type: 'listing.rejected.v1', audience: 'Seller', channels: ['push', 'sms'], locales: 'en, hi', dlt: false },
  { type: 'booking.reminder.v1', audience: 'Customer + inspector', channels: ['push', 'sms'], locales: 'en, hi', dlt: true },
  { type: 'deal.payment_due.v1', audience: 'Buyer', channels: ['push', 'sms', 'email'], locales: 'en', dlt: true },
  { type: 'insurance.expiring.v1', audience: 'Vehicle owner', channels: ['whatsapp', 'email'], locales: 'en, hi', dlt: true },
] as const;
