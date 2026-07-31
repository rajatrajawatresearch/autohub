import type { Metadata } from 'next';
import { Badge, Input, SectionHeading } from '@autohub/ui';

export const metadata: Metadata = { title: 'Audit log' };

/**
 * Who, what, before, after, when, from where — append-only.
 *
 * This exists because a registry that anyone can change is a registry someone will change badly,
 * and the first question after that is always "who published v9 and what was in v8". Entries are
 * never edited or deleted, including by a super admin.
 */
export default function AuditPage() {
  return (
    <div className="mx-auto max-w-[var(--container-wide)] px-4 py-6">
      <SectionHeading
        eyebrow="System"
        title="Audit log"
        description="Append-only. Every registry publish, money movement and admin action, with the before and after."
        level={1}
        action={<Input placeholder="Filter by actor, action or entity" aria-label="Filter audit log" className="w-[18rem] font-mono" />}
      />

      <ul className="mt-6 divide-y divide-[color:var(--border-hairline)] border-y border-[color:var(--border-hairline)]">
        {ENTRIES.map((entry) => (
          <li key={entry.id} className="flex flex-wrap items-baseline gap-3 py-2.5">
            <span className="font-mono text-[0.75rem] text-[color:var(--text-secondary)]">{entry.at}</span>
            <span className="font-mono text-body-sm">{entry.action}</span>
            {entry.dangerous ? <Badge variant="flag" size="sm">dangerous</Badge> : null}
            <span className="text-body-sm text-[color:var(--text-secondary)]">{entry.detail}</span>
            <span className="ml-auto font-mono text-[0.75rem] text-[color:var(--text-secondary)]">
              {entry.actor} · {entry.ip}
            </span>
          </li>
        ))}
      </ul>
    </div>
  );
}

const ENTRIES = [
  { id: '1', at: '13:04:22', action: 'registry.pricing.publish', detail: 'auction.win v6 → v7 · gold tier 2.0% → 1.5% (MH only)', actor: 'finance@autohub.in', ip: '10.2.4.17', dangerous: true },
  { id: '2', at: '12:51:08', action: 'listing.moderate.reject', detail: 'MH12AB1234 · reason: photos do not match the registration', actor: 'ops@autohub.in', ip: '10.2.4.31', dangerous: false },
  { id: '3', at: '12:40:55', action: 'registry.workflow.publish', detail: 'listing v2 → v3 · added state legal_hold before payout', actor: 'ops@autohub.in', ip: '10.2.4.31', dangerous: true },
  { id: '4', at: '11:58:12', action: 'dealer.kyc.approve', detail: 'Whitefield Wheels · penny-drop confirmed', actor: 'support@autohub.in', ip: '10.2.4.9', dangerous: false },
  { id: '5', at: '11:22:47', action: 'system.role.assign', detail: 'auction_moderator scoped to cities [pune, nashik]', actor: 'admin@autohub.in', ip: '10.2.4.2', dangerous: true },
  { id: '6', at: '10:15:03', action: 'registry.taxonomy.publish', detail: 'v40 → v41 · added category construction, attribute boom_length', actor: 'ops@autohub.in', ip: '10.2.4.31', dangerous: true },
] as const;
