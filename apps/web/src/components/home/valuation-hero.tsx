'use client';

import { useState } from 'react';
import Link from 'next/link';
import {
  primaryValuation,
  useValuationQuote,
  type ValuationCondition,
  type ValuationQuote,
} from '@autohub/api';
import {
  Button,
  FieldBlock,
  MoneyValue,
  OdometerValue,
  PlateBlock,
  PlateInput,
} from '@autohub/ui';
import { useActiveCity } from '@/lib/use-active-city';

/**
 * The thesis, rendered as an interaction.
 *
 * The page does not open with a photograph of a car and a headline over it. It opens with an empty
 * number plate waiting to be typed in — the most characteristic artifact in Indian automotive life,
 * doing the most valuable job in the product. One input, one outcome, no funnel and no phone number.
 *
 * The answer is revealed in place rather than on a route of its own. A navigation between the
 * question and the answer would put a page load in the middle of the one moment this product is
 * judged on.
 */
export function ValuationHero() {
  const [plate, setPlate] = useState('');
  const [kms, setKms] = useState('');
  const [city] = useActiveCity();
  const quote = useValuationQuote();

  const canSubmit = plate.length >= 6 && !quote.isPending;

  function submit() {
    if (!canSubmit) return;
    quote.mutate({
      registrationNumber: plate,
      kmsDriven: kms ? Number(kms.replace(/\D/g, '')) : undefined,
      city,
      condition: 'good' satisfies ValuationCondition,
    });
  }

  if (quote.data) {
    return <ValuationReveal quote={quote.data} onReset={() => { quote.reset(); setPlate(''); }} />;
  }

  return (
    <section className="border-b border-[color:var(--border-hairline)]">
      <div className="mx-auto max-w-[var(--container-content)] px-4 py-20 sm:px-6 sm:py-28">
        <div className="mx-auto max-w-[34rem] text-center">
          <p className="ah-label">Free valuation</p>

          <h1 className="mt-4 font-display text-display-xl text-[color:var(--text-primary)]">
            What is your car worth today?
          </h1>

          <div className="mt-10">
            <PlateInput
              value={plate}
              onChange={setPlate}
              onSubmit={submit}
              size="lg"
              label="Registration number"
              placeholder="MH 12 AB 1234"
              error={quote.isError ? readableError(quote.error) : undefined}
            />
          </div>

          <div className="mx-auto mt-6 flex max-w-[22rem] flex-col gap-3">
            <label className="text-left">
              <span className="ah-label">Kms driven — optional</span>
              <input
                inputMode="numeric"
                value={kms}
                onChange={(event) => setKms(event.target.value.replace(/\D/g, '').slice(0, 6))}
                onKeyDown={(event) => event.key === 'Enter' && submit()}
                placeholder="38400"
                aria-label="Kms driven"
                className="mt-1 h-[var(--density-control-h)] w-full rounded-[var(--radius-control)] border border-[color:var(--border-hairline)] bg-[color:var(--surface-card)] px-3 font-mono text-data text-[color:var(--text-primary)] outline-none placeholder:text-[color:var(--text-secondary)] focus-visible:border-[color:var(--accent-primary)]"
              />
            </label>

            {/* The single rationed yellow element on this page. */}
            <Button variant="action" size="lg" onClick={submit} disabled={!canSubmit}>
              {quote.isPending ? 'Checking RTO records…' : 'Get valuation'}
            </Button>
          </div>

          <p className="mt-5 font-mono text-[0.75rem] text-[color:var(--text-secondary)]">
            Free · 30 seconds · no phone number needed
          </p>
        </div>
      </div>
    </section>
  );
}

function ValuationReveal({ quote, onReset }: { quote: ValuationQuote; onReset: () => void }) {
  const headline = primaryValuation(quote);
  const rest = quote.valuations.filter((band) => band.code !== headline?.code);

  return (
    <section className="border-b border-[color:var(--border-hairline)]">
      <div className="mx-auto max-w-[var(--container-content)] px-4 py-16 sm:px-6 sm:py-20">
        <div className="mx-auto max-w-[42rem]">
          <div className="flex flex-wrap items-center justify-between gap-4">
            <PlateBlock
              registration={quote.vehicle.registration.canonical}
              display={quote.vehicle.registration.display}
              plateClass={quote.vehicle.registration.plateClass}
              size="md"
            />
            <button
              type="button"
              onClick={onReset}
              className="text-body-sm text-[color:var(--text-secondary)] underline underline-offset-4 transition-colors hover:text-[color:var(--text-primary)]"
            >
              Check another vehicle
            </button>
          </div>

          <h1 className="mt-6 font-display text-display-lg text-[color:var(--text-primary)]">
            {quote.vehicle.title}
          </h1>

          <div className="mt-10 border-y border-[color:var(--border-hairline)] py-8">
            <p className="ah-label">{headline?.label ?? 'Market value'}</p>
            {/* The one animation in the product. Suppressed under prefers-reduced-motion. */}
            <OdometerValue minorUnits={headline?.amountMinor ?? 0} size="xl" className="mt-2" />
            <p className="mt-3 font-mono text-body-sm text-[color:var(--text-secondary)]">
              Range {' '}
              <MoneyValue minorUnits={quote.range.lowMinor} compact size="sm" tone="secondary" />
              {' – '}
              <MoneyValue minorUnits={quote.range.highMinor} compact size="sm" tone="secondary" />
            </p>
          </div>

          <dl className="mt-8 grid grid-cols-2 gap-x-6 gap-y-6 sm:grid-cols-4">
            {rest.map((band) => (
              <FieldBlock
                key={band.code}
                label={band.label}
                value={<MoneyValue minorUnits={band.amountMinor} compact size="md" />}
                mono
              />
            ))}
          </dl>

          <div className="mt-10 grid gap-6 border-t border-[color:var(--border-hairline)] pt-6 sm:grid-cols-2">
            <FieldBlock
              label="Confidence"
              value={`${Math.round(quote.confidence.score * 100)}% · ${quote.confidence.band}`}
              hint={quote.confidence.basedOn}
              mono
            />
            <FieldBlock
              label="As of"
              value={quote.asOf}
              hint={quote.modelVersion ? `Model ${quote.modelVersion}` : undefined}
              mono
            />
          </div>

          <div className="mt-10 flex flex-wrap gap-3">
            <Button variant="action" size="lg" asChild>
              <Link href="/sell">Sell at this price</Link>
            </Button>
            <Button variant="outline" size="lg" asChild>
              <Link href="/buy">Browse similar vehicles</Link>
            </Button>
          </div>

          <p className="mt-6 max-w-[52ch] text-body-sm text-[color:var(--text-secondary)]">
            A valuation is an estimate from comparable sales, not an offer. The price a dealer pays
            depends on the inspection.
          </p>
        </div>
      </div>
    </section>
  );
}

/**
 * Errors state the problem and the fix, in the interface's voice, and never apologise.
 * The machine code is the key; this is the fallback until the i18n registry answers.
 */
function readableError(error: unknown): string {
  const code = (error as { code?: string } | null)?.code;

  switch (code) {
    case 'registration.malformed':
      return 'Check the digits — example MH12AB1234.';
    case 'catalog.vahan.record_not_found':
      return "This registration number isn't in the RTO records. Check the digits, or enter details manually.";
    case 'common.network.unreachable':
      return 'Cannot reach AutoHub. Check your connection and try again.';
    default:
      return 'That valuation did not complete. Try again in a moment.';
  }
}
