import type { Metadata } from 'next';
import { serverApi } from '@autohub/api/server';
import { Badge, FieldBlock, SectionHeading } from '@autohub/ui';

export const metadata: Metadata = { title: 'Settings' };
export const revalidate = 300;

export default async function SettingsPage() {
  const [cities, features] = await Promise.all([
    serverApi.cities(true).catch(() => null),
    serverApi.features().catch(() => null),
  ]);

  const enabledModules = Object.entries(features?.modules ?? {}).filter(([, on]) => on);

  return (
    <div className="mx-auto max-w-[var(--container-wide)] px-4 py-6">
      <SectionHeading
        eyebrow="Account"
        title="Settings"
        description="Branches, verification and what this account is allowed to do."
        level={1}
      />

      <section className="mt-6">
        <SectionHeading title="Verification" level={2} size="sm" rule />
        <dl className="mt-4 grid gap-6 sm:grid-cols-4">
          <FieldBlock label="GSTIN" value="27AAECS1234F1Z5" mono hint="Verified" />
          <FieldBlock label="PAN" value="AAECS1234F" mono hint="Verified" />
          <FieldBlock label="Trade licence" value="PMC/2019/44821" mono hint="Expires Mar 2027" />
          <FieldBlock
            label="Bank"
            value="HDFC ••4471"
            mono
            hint="Penny-drop confirmed — payouts enabled"
          />
        </dl>
      </section>

      <section className="mt-8">
        <SectionHeading
          title="Branches"
          description="Stock, leads and payouts are scoped to the branch selected in the top bar."
          level={2}
          size="sm"
          rule
        />
        <ul className="mt-4 divide-y divide-[color:var(--border-hairline)]">
          {(cities?.items ?? []).slice(0, 4).map((city, index) => (
            <li key={city.code} className="flex items-center gap-3 py-2.5">
              <span className="text-body-sm">{city.name} branch</span>
              <span className="font-mono text-[0.6875rem] text-[color:var(--text-secondary)]">
                {city.stateCode}
              </span>
              {index === 0 ? (
                <Badge variant="primary" size="sm">
                  Active
                </Badge>
              ) : null}
            </li>
          ))}
        </ul>
      </section>

      <section className="mt-8">
        <SectionHeading
          title="Enabled for this account"
          description="Modules come from the feature registry — an admin turns them on per city and tier, without a release."
          level={2}
          size="sm"
          rule
        />
        <div className="mt-4 flex flex-wrap gap-2">
          {enabledModules.map(([code]) => (
            <Badge key={code} variant="outline">
              {code.replace(/_/g, ' ')}
            </Badge>
          ))}
          {enabledModules.length === 0 ? (
            <p className="text-body-sm text-[color:var(--text-secondary)]">
              The feature registry is unreachable, so entitlements cannot be shown.
            </p>
          ) : null}
        </div>
      </section>
    </div>
  );
}
