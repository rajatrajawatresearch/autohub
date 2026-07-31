import type { Metadata } from 'next';
import { serverApi } from '@autohub/api/server';
import { Badge, SectionHeading } from '@autohub/ui';
import { RegistryPage } from '@/components/registry/registry-page';

export const metadata: Metadata = { title: 'Localisation' };
export const revalidate = 60;

const LOCALES = ['en-IN', 'hi-IN', 'mr-IN', 'ta-IN', 'te-IN', 'bn-IN', 'gu-IN', 'kn-IN'] as const;

/**
 * Strings live here, not in the bundle, which is what makes shipping Tamil a publish rather than an
 * App Store review. A missing key falls back to en-IN and then to the key itself — never to an
 * empty string, because a blank button is worse than an untranslated one.
 */
export default async function I18nRegistryPage({
  searchParams,
}: {
  searchParams: Promise<{ locale?: string }>;
}) {
  const { locale } = await searchParams;
  const selected = locale ?? 'hi-IN';

  const [base, target] = await Promise.all([
    serverApi.i18n('en-IN').catch(() => null),
    serverApi.i18n(selected).catch(() => null),
  ]);

  const keys = Object.keys(base?.resources ?? {}).sort();
  const missing = keys.filter((key) => !target?.resources[key]);

  return (
    <RegistryPage
      eyebrow="Registry 9 of 10"
      title="Localisation"
      description="Every user-facing string, served from the API and cached against a version."
      version={target?.version ?? '—'}
      publishedBy="content@autohub.in"
    >
      <div className="flex flex-wrap items-center gap-2 pb-4">
        {LOCALES.map((code) => (
          <a
            key={code}
            href={`/registry/i18n?locale=${code}`}
            className={[
              'rounded-[var(--radius-control)] border px-2.5 py-1 font-mono text-body-sm',
              code === selected
                ? 'border-[color:var(--accent-primary)] bg-[color:var(--accent-primary)] text-[color:var(--text-on-primary)]'
                : 'border-[color:var(--border-hairline)] text-[color:var(--text-secondary)]',
            ].join(' ')}
          >
            {code}
          </a>
        ))}
        <span className="ml-auto font-mono text-[0.75rem] text-[color:var(--text-secondary)]">
          {keys.length - missing.length} of {keys.length} translated
        </span>
      </div>

      <SectionHeading title={`en-IN → ${selected}`} level={2} size="sm" rule />

      <div className="mt-4 overflow-x-auto">
        <table className="w-full border-collapse text-body-sm">
          <thead>
            <tr className="border-b border-[color:var(--border-hairline)]">
              <th className="ah-label py-2 text-left">Key</th>
              <th className="ah-label py-2 text-left">en-IN</th>
              <th className="ah-label py-2 text-left">{selected}</th>
            </tr>
          </thead>
          <tbody className="divide-y divide-[color:var(--border-hairline)]">
            {keys.map((key) => (
              <tr key={key}>
                <td className="py-2 pr-3 font-mono text-[0.75rem] text-[color:var(--text-secondary)]">{key}</td>
                <td className="py-2 pr-3">{base?.resources[key]}</td>
                <td className="py-2">
                  {target?.resources[key] ?? (
                    <Badge variant="caution" size="sm">missing — falls back to en-IN</Badge>
                  )}
                </td>
              </tr>
            ))}
          </tbody>
        </table>
      </div>
    </RegistryPage>
  );
}
