'use client';

import { useState } from 'react';
import { useQuotePricing } from '@autohub/api';
import { Button, FieldBlock, Input, MoneyValue, Select } from '@autohub/ui';

/**
 * Prove the rule before you publish it.
 *
 * A commission rule takes real money from a real dealer on a real transaction. This panel runs the
 * exact same engine the platform runs — same rule set, same precedence, same rounding — and shows
 * every line it produced and which rule won. If a change does not do what you expected here, it
 * will not do what you expected in production either.
 */
export function PricingTester() {
  const quote = useQuotePricing();

  const [scope, setScope] = useState('auction.win');
  const [rupees, setRupees] = useState('847350');
  const [state, setState] = useState('MH');
  const [city, setCity] = useState('pune');
  const [tier, setTier] = useState('gold');
  const [category, setCategory] = useState('suv');

  function run() {
    quote.mutate({
      scope,
      context: {
        vehicle: { category },
        location: { state, city },
        party: { type: 'dealer', tier },
        amount: { minorUnits: Number(rupees.replace(/\D/g, '') || 0) * 100, currency: 'INR' },
      },
    });
  }

  return (
    <div className="rounded-[var(--radius-card)] border border-[color:var(--border-hairline)] bg-[color:var(--surface-card)] p-4">
      <p className="ah-label">Test a quote</p>
      <p className="mt-1 text-body-sm text-[color:var(--text-secondary)]">
        Runs the live pricing engine. Nothing is charged.
      </p>

      <div className="mt-4 space-y-3">
        <Field label="Scope">
          <Select
            value={scope}
            onValueChange={setScope}
            triggerSize="sm"
            options={SCOPES.map((s) => ({ value: s, label: s }))}
            aria-label="Rule set scope"
          />
        </Field>

        <Field label="Base amount (₹)">
          <Input
            inputMode="numeric"
            value={rupees}
            onChange={(event) => setRupees(event.target.value.replace(/\D/g, '').slice(0, 10))}
            className="font-mono"
            aria-label="Base amount in rupees"
          />
        </Field>

        <div className="grid grid-cols-2 gap-3">
          <Field label="State">
            <Input value={state} onChange={(e) => setState(e.target.value.toUpperCase().slice(0, 2))} className="font-mono" aria-label="State code" />
          </Field>
          <Field label="City">
            <Input value={city} onChange={(e) => setCity(e.target.value)} className="font-mono" aria-label="City code" />
          </Field>
          <Field label="Dealer tier">
            <Select
              value={tier}
              onValueChange={setTier}
              triggerSize="sm"
              options={TIERS.map((t) => ({ value: t, label: t }))}
              aria-label="Dealer tier"
            />
          </Field>
          <Field label="Category">
            <Input value={category} onChange={(e) => setCategory(e.target.value)} className="font-mono" aria-label="Vehicle category code" />
          </Field>
        </div>

        <Button className="w-full" onClick={run} disabled={quote.isPending}>
          {quote.isPending ? 'Evaluating…' : 'Run quote'}
        </Button>
      </div>

      {quote.isError ? (
        <p className="mt-4 text-body-sm text-[color:var(--color-flag)]">
          No rule matched, or the engine is unreachable. A scope with no published rule set fails
          loudly on purpose — charging nothing silently is worse.
        </p>
      ) : null}

      {quote.data ? (
        <div className="mt-5 border-t border-[color:var(--border-hairline)] pt-4">
          <table className="w-full border-collapse text-body-sm">
            <tbody className="divide-y divide-[color:var(--border-hairline)]">
              <tr>
                <td className="py-1.5 text-[color:var(--text-secondary)]">Base</td>
                <td className="py-1.5 text-right">
                  <MoneyValue minorUnits={quote.data.baseAmount} size="sm" />
                </td>
              </tr>
              {quote.data.lines.map((line) => (
                <tr key={line.code}>
                  <td className="py-1.5">
                    {line.label}
                    <span className="block font-mono text-[0.6875rem] text-[color:var(--text-secondary)]">
                      {line.basis}
                      {typeof line.rate === 'number' ? ` · ${(line.rate * 100).toFixed(2)}%` : ''}
                      {line.appliesTo?.length ? ` on ${line.appliesTo.join(', ')}` : ''}
                    </span>
                  </td>
                  <td className="py-1.5 text-right align-top">
                    <MoneyValue minorUnits={line.amountMinor} size="sm" />
                  </td>
                </tr>
              ))}
              <tr>
                <td className="py-1.5 font-semibold">Payable</td>
                <td className="py-1.5 text-right font-semibold">
                  <MoneyValue minorUnits={quote.data.payableMinor} size="sm" />
                </td>
              </tr>
            </tbody>
          </table>

          <div className="mt-4 grid gap-4">
            <FieldBlock
              label="Total fees"
              value={<MoneyValue minorUnits={quote.data.totalFeesMinor} size="sm" />}
              mono
            />
            {quote.data.lines[0]?.ruleName ? (
              <FieldBlock label="Winning rule" value={quote.data.lines[0].ruleName} />
            ) : null}
            {quote.data.explanation ? (
              <FieldBlock label="Explanation shown to the payer" value={quote.data.explanation} />
            ) : null}
          </div>
        </div>
      ) : null}
    </div>
  );
}

function Field({ label, children }: { label: string; children: React.ReactNode }) {
  return (
    <label className="block">
      <span className="ah-label">{label}</span>
      <span className="mt-1 block">{children}</span>
    </label>
  );
}

const SCOPES = [
  'auction.win',
  'sale.complete',
  'listing.create',
  'rental.booking',
  'lease.origination',
  'attachment.monthly',
  'loan.referral',
  'insurance.referral',
  'inspection.book',
] as const;

const TIERS = ['bronze', 'silver', 'gold', 'platinum'] as const;
