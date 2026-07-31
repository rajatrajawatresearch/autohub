import Link from 'next/link';
import { serverApi } from '@autohub/api/server';
import { FieldBlock, SectionHeading } from '@autohub/ui';

export const dynamic = 'force-dynamic';

/**
 * What needs a human today, and what state the platform's configuration is in.
 *
 * The second half matters more than it looks: a registry that nobody watches drifts, and the first
 * sign is usually a queue that stopped moving because a workflow was published with a dead end.
 */
export default async function ConsoleDashboard() {
  const [tree, features, cities] = await Promise.all([
    serverApi.taxonomyTree().catch(() => null),
    serverApi.features().catch(() => null),
    serverApi.cities(true).catch(() => null),
  ]);

  const modules = Object.entries(features?.modules ?? {});
  const flags = Object.entries(features?.flags ?? {});

  return (
    <div className="mx-auto max-w-[var(--container-wide)] px-4 py-6">
      <SectionHeading
        eyebrow="Operations"
        title="Console"
        description="Queues first, configuration second. Both are the platform."
        level={1}
      />

      <dl className="mt-6 grid grid-cols-2 gap-px overflow-hidden border border-[color:var(--border-hairline)] bg-[color:var(--border-hairline)] lg:grid-cols-4">
        <Tile label="Listings awaiting review" value="18" hint="SLA 4h · 2 overdue" />
        <Tile label="Dealers pending KYC" value="6" hint="3 waiting on bank penny-drop" />
        <Tile label="Open disputes" value="2" hint="Oldest 3 days" />
        <Tile label="Payouts blocked" value="0" hint="Nothing held" />
      </dl>

      <div className="mt-8 grid gap-8 lg:grid-cols-2">
        <section>
          <SectionHeading title="Registry state" level={2} size="sm" rule />
          <dl className="mt-4 grid gap-5 sm:grid-cols-2">
            <FieldBlock
              label="Taxonomy version"
              value={String(tree?.version ?? '—')}
              mono
              hint={`${tree?.categories.length ?? 0} top-level categories`}
            />
            <FieldBlock
              label="Feature registry version"
              value={String(features?.version ?? '—')}
              mono
              hint={`${modules.filter(([, on]) => on).length} of ${modules.length} modules on`}
            />
            <FieldBlock
              label="Geography version"
              value={String(cities?.version ?? '—')}
              mono
              hint={`${cities?.items.length ?? 0} serviceable cities`}
            />
            <FieldBlock
              label="Active flags"
              value={String(flags.filter(([, on]) => on).length)}
              mono
              hint={`${flags.length} defined`}
            />
          </dl>

          <p className="mt-5 max-w-[60ch] text-body-sm text-[color:var(--text-secondary)]">
            Clients cache each registry against its version and refetch when it moves. A publish that
            does not bump a version is a publish nobody downstream will see.
          </p>
        </section>

        <section>
          <SectionHeading title="Editors" level={2} size="sm" rule />
          <ul className="mt-4 divide-y divide-[color:var(--border-hairline)] border-y border-[color:var(--border-hairline)]">
            {EDITORS.map((editor) => (
              <li key={editor.href}>
                <Link
                  href={editor.href}
                  className="flex items-baseline gap-3 py-2.5 transition-colors hover:bg-[color:var(--surface-sunken)]"
                >
                  <span className="ah-label w-14 shrink-0">{editor.index}</span>
                  <span className="text-body-sm font-medium">{editor.label}</span>
                  <span className="ml-auto max-w-[28ch] truncate text-body-sm text-[color:var(--text-secondary)]">
                    {editor.change}
                  </span>
                </Link>
              </li>
            ))}
          </ul>
        </section>
      </div>
    </div>
  );
}

function Tile({ label, value, hint }: { label: string; value: string; hint: string }) {
  return (
    <div className="bg-[color:var(--surface-card)] px-4 py-4">
      <dt className="ah-label">{label}</dt>
      <dd className="mt-1.5 font-mono text-[1.25rem] font-medium">{value}</dd>
      <p className="mt-1 font-mono text-[0.6875rem] text-[color:var(--text-secondary)]">{hint}</p>
    </div>
  );
}

/** The list doubles as documentation: each row says what stops being a deployment. */
const EDITORS = [
  { index: '01', href: '/registry/taxonomy', label: 'Taxonomy', change: 'New category with its own attributes' },
  { index: '02', href: '/registry/forms', label: 'Forms', change: 'A field on the sell flow, everywhere at once' },
  { index: '03', href: '/registry/workflows', label: 'Workflows', change: 'A legal-hold state before payout' },
  { index: '04', href: '/registry/pricing', label: 'Pricing', change: '1.5% auction commission in Pune only' },
  { index: '06', href: '/registry/features', label: 'Features', change: 'Lease in Bengaluru, dealers only' },
  { index: '07', href: '/registry/notifications', label: 'Notifications', change: 'The outbid message in Marathi' },
  { index: '09', href: '/registry/i18n', label: 'Localisation', change: 'Tamil without an app release' },
  { index: '10', href: '/registry/geo', label: 'Geography', change: 'Opening a new service city' },
] as const;
