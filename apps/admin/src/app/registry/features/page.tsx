import type { Metadata } from 'next';
import { serverApi } from '@autohub/api/server';
import { Badge, SectionHeading, Switch } from '@autohub/ui';
import { RegistryPage } from '@/components/registry/registry-page';

export const metadata: Metadata = { title: 'Features' };
export const dynamic = 'force-dynamic';

/**
 * Modules and flags. Targeting rules live on the server and only booleans reach a client — a
 * targeting rule shipped to the browser is a targeting rule an attacker can read and work around.
 */
export default async function FeatureRegistryPage() {
  const features = await serverApi.features().catch(() => null);

  const modules = Object.entries(features?.modules ?? {});
  const flags = Object.entries(features?.flags ?? {});

  return (
    <RegistryPage
      eyebrow="Registry 6 of 10"
      title="Features & modules"
      description="What exists, for whom, and where. Evaluated server-side per user and city."
      version={features?.version ?? '—'}
      publishedBy="ops@autohub.in"
    >
      <div className="grid gap-8 lg:grid-cols-2">
        <section>
          <SectionHeading title="Modules" description="A module off is a module absent — no empty page, no coming-soon." level={2} size="sm" rule />
          <ul className="mt-4 divide-y divide-[color:var(--border-hairline)] border-y border-[color:var(--border-hairline)]">
            {modules.map(([code, on]) => (
              <li key={code} className="flex items-center gap-3 py-2.5">
                <span className="font-mono text-body-sm">{code}</span>
                <Badge variant={on ? 'clear' : 'neutral'} size="sm">{on ? 'on' : 'off'}</Badge>
                <span className="ml-auto"><Switch checked={on} aria-label={`Toggle ${code}`} /></span>
              </li>
            ))}
          </ul>
        </section>

        <section>
          <SectionHeading title="Flags" description="Each flag carries targeting rules; this shows the resolved value for your account." level={2} size="sm" rule />
          <ul className="mt-4 divide-y divide-[color:var(--border-hairline)] border-y border-[color:var(--border-hairline)]">
            {flags.map(([code, on]) => (
              <li key={code} className="flex items-center gap-3 py-2.5">
                <span className="font-mono text-body-sm">{code}</span>
                <Badge variant={on ? 'clear' : 'neutral'} size="sm">{on ? 'on' : 'off'}</Badge>
                <span className="ml-auto"><Switch checked={on} aria-label={`Toggle ${code}`} /></span>
              </li>
            ))}
          </ul>
        </section>
      </div>
    </RegistryPage>
  );
}
