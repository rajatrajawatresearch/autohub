import type { Metadata } from 'next';
import { SectionHeading } from '@autohub/ui';
import { RegistryPage } from '@/components/registry/registry-page';
import { PricingTester } from '@/components/registry/pricing-tester';

export const metadata: Metadata = { title: 'Pricing & commission' };

/**
 * Every rupee the platform earns, as data.
 *
 * The tester is the part that matters. A commission rule is the one registry row that takes money
 * from a real dealer on a real transaction, so it must be provable before it is published — enter a
 * context, see the exact lines the engine would produce, and which rule won and why. Publishing a
 * pricing change you have not run through this is how a platform quietly overcharges a city for a
 * fortnight.
 */
export default function PricingRegistryPage() {
  return (
    <RegistryPage
      eyebrow="Registry 4 of 10"
      title="Pricing & commission"
      description="Rule sets per commercial moment. One rule wins per quote — the highest priority whose condition matches."
      version={7}
      publishedBy="finance@autohub.in"
    >
      <div className="grid gap-8 lg:grid-cols-[1fr_24rem]">
        <div>
          <SectionHeading
            title="Rule sets"
            description="One per commercial moment. Fifteen revenue streams, no percentage anywhere in the codebase."
            level={2}
            size="sm"
            rule
          />

          <div className="mt-4 overflow-x-auto">
            <table className="w-full border-collapse text-body-sm">
              <thead>
                <tr className="border-b border-[color:var(--border-hairline)]">
                  <th className="ah-label py-2 text-left">Scope</th>
                  <th className="ah-label py-2 text-left">Rules</th>
                  <th className="ah-label py-2 text-left">Basis</th>
                  <th className="ah-label py-2 text-right">Version</th>
                </tr>
              </thead>
              <tbody className="divide-y divide-[color:var(--border-hairline)]">
                {RULE_SETS.map((set) => (
                  <tr key={set.scope}>
                    <td className="py-2 pr-3">
                      <span className="font-mono">{set.scope}</span>
                      <span className="block text-[color:var(--text-secondary)]">{set.label}</span>
                    </td>
                    <td className="py-2 pr-3 font-mono text-[0.75rem]">{set.rules}</td>
                    <td className="py-2 pr-3 text-[color:var(--text-secondary)]">{set.basis}</td>
                    <td className="py-2 text-right font-mono text-[0.75rem]">v{set.version}</td>
                  </tr>
                ))}
              </tbody>
            </table>
          </div>

          <SectionHeading
            title="How a quote is resolved"
            level={2}
            size="sm"
            rule
            className="mt-10"
          />
          <ol className="mt-4 space-y-3 text-body-sm">
            {RESOLUTION.map((step, index) => (
              <li key={step} className="flex gap-3">
                <span className="ah-label pt-0.5">{String(index + 1).padStart(2, '0')}</span>
                <span className="max-w-[64ch] text-[color:var(--text-secondary)]">{step}</span>
              </li>
            ))}
          </ol>
        </div>

        <aside className="lg:sticky lg:top-6 lg:self-start">
          <PricingTester />
        </aside>
      </div>
    </RegistryPage>
  );
}

const RULE_SETS = [
  { scope: 'auction.win', label: 'Auction commission', rules: 4, basis: 'Slab by price band, gold-dealer override', version: 7 },
  { scope: 'sale.complete', label: 'Buy/sell commission', rules: 3, basis: 'Percent with floor and cap', version: 5 },
  { scope: 'listing.create', label: 'Listing fee', rules: 2, basis: 'Flat, waived on first listing', version: 3 },
  { scope: 'listing.premium', label: 'Premium placement', rules: 1, basis: 'Flat per 30 days', version: 2 },
  { scope: 'dealer.subscription', label: 'Dealer subscription', rules: 3, basis: 'Flat per tier', version: 4 },
  { scope: 'rental.booking', label: 'Rental commission', rules: 2, basis: 'Percent of booking value', version: 3 },
  { scope: 'lease.origination', label: 'Lease commission', rules: 1, basis: 'Percent of contract value', version: 1 },
  { scope: 'attachment.monthly', label: 'Taxi attachment', rules: 2, basis: 'Tiered on monthly rate', version: 4 },
  { scope: 'loan.referral', label: 'Loan referral', rules: 2, basis: 'Percent of disbursed amount', version: 2 },
  { scope: 'insurance.referral', label: 'Insurance referral', rules: 2, basis: 'Percent of premium', version: 2 },
  { scope: 'inspection.book', label: 'Inspection charge', rules: 2, basis: 'Flat by city', version: 3 },
  { scope: 'valuation.report', label: 'Valuation report', rules: 1, basis: 'Flat', version: 1 },
  { scope: 'membership.premium', label: 'Premium membership', rules: 1, basis: 'Flat per year', version: 1 },
  { scope: 'ads.banner', label: 'Advertisement banner', rules: 1, basis: 'Flat per impression block', version: 1 },
  { scope: 'dealer.featured', label: 'Featured dealer', rules: 1, basis: 'Flat per 30 days', version: 1 },
] as const;

const RESOLUTION = [
  'Rules effective on the quote date are collected; effective-dated rules outside the window are ignored.',
  'Each rule’s condition is evaluated against the quote context. A condition that will not parse does not match — the safe direction for a fee.',
  'The highest-priority matching rule wins. Exactly one applies: summing every match would silently double a fee the day someone adds a discount rule.',
  'Its fee components produce the lines — flat, percent, slab, tiered, or a percent of other lines for GST.',
  'Floors and caps clamp each line, then the lines and the base become the payable amount.',
  'The winning rule’s explanation travels with the quote, so a dealer asking "why 2%?" gets an answer without anyone reading code.',
] as const;
