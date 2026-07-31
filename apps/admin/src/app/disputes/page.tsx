import type { Metadata } from 'next';
import { EmptyState, FieldBlock, SectionHeading, StatusPill } from '@autohub/ui';

export const metadata: Metadata = { title: 'Disputes' };

/**
 * A dispute freezes the money, not the record. Escrow release is blocked while a case is open, and
 * the ledger keeps every entry — reversing a payout is a new balanced pair, never an edit.
 */
export default function DisputesPage() {
  return (
    <div className="mx-auto max-w-[var(--container-wide)] px-4 py-6">
      <SectionHeading
        eyebrow="Operations"
        title="Disputes"
        description="Open cases hold escrow. Nothing is deleted; a reversal is a new ledger entry."
        level={1}
      />

      <ul className="mt-6 space-y-3">
        {CASES.map((dispute) => (
          <li key={dispute.id} className="rounded-[var(--radius-card)] border border-[color:var(--border-hairline)] bg-[color:var(--surface-card)] p-4">
            <div className="flex flex-wrap items-center gap-3">
              <span className="font-mono text-body-sm">{dispute.id}</span>
              <StatusPill category={dispute.category} label={dispute.state} size="sm" />
              <span className="text-body-sm">{dispute.subject}</span>
              <span className="ml-auto font-mono text-[0.75rem] text-[color:var(--text-secondary)]">
                {dispute.age}
              </span>
            </div>
            <dl className="mt-3 grid gap-5 sm:grid-cols-4">
              <FieldBlock label="Raised by" value={dispute.raisedBy} />
              <FieldBlock label="Deal" value={dispute.deal} mono />
              <FieldBlock label="Escrow" value={dispute.escrow} mono />
              <FieldBlock label="Next step" value={dispute.next} />
            </dl>
          </li>
        ))}
      </ul>

      {CASES.length === 0 ? <EmptyState title="No open disputes" body="Money is moving without anyone contesting it." align="center" /> : null}
    </div>
  );
}

const CASES = [
  {
    id: 'DSP-2026-0141', state: 'Under review', category: 'in_progress' as const,
    subject: 'Odometer reading disputed after delivery',
    raisedBy: 'Buyer', deal: 'DL-88231', escrow: 'Held ₹5.4L', age: '3 days open',
    next: 'Inspector re-check booked for Thursday',
  },
  {
    id: 'DSP-2026-0142', state: 'Awaiting seller', category: 'awaiting_action' as const,
    subject: 'RC transfer not filed within the agreed window',
    raisedBy: 'Buyer', deal: 'DL-88474', escrow: 'Held ₹3.9L', age: '1 day open',
    next: 'Seller has 48h to upload Form 29/30',
  },
];
