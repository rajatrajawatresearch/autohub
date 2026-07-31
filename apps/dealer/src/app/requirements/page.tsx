import type { Metadata } from 'next';
import { serverApi } from '@autohub/api/server';
import { Badge, Button, FieldBlock, SectionHeading } from '@autohub/ui';

export const metadata: Metadata = { title: 'Requirements' };
export const revalidate = 120;

/**
 * The requirement board inverts the marketplace: instead of browsing what exists, a dealer posts
 * what they need — "20 Dzire, 2019 or newer, under 80,000 km" — and the platform matches supply
 * as it arrives. It is the cheapest demand signal the company has, because dealers volunteer it.
 */
export default async function RequirementsPage() {
  const tree = await serverApi.taxonomyTree().catch(() => null);
  const categories = tree?.categories ?? [];

  return (
    <div className="mx-auto max-w-[var(--container-wide)] px-4 py-6">
      <SectionHeading
        eyebrow="Stock"
        title="Requirements"
        description="Tell the network what you are buying. We match it against new listings and other dealers' stock."
        level={1}
        action={<Button size="sm">Post a requirement</Button>}
      />

      <div className="mt-6 grid gap-4 lg:grid-cols-3">
        {DEMO_REQUIREMENTS.map((requirement) => (
          <article
            key={requirement.id}
            className="rounded-[var(--radius-card)] border border-[color:var(--border-hairline)] bg-[color:var(--surface-card)] p-4"
          >
            <div className="flex items-start justify-between gap-2">
              <h2 className="font-display text-subheading">{requirement.title}</h2>
              <Badge variant={requirement.matches > 0 ? 'primary' : 'neutral'} size="sm">
                {requirement.matches} matched
              </Badge>
            </div>

            <dl className="mt-4 grid grid-cols-2 gap-4">
              <FieldBlock label="Quantity" value={String(requirement.quantity)} mono />
              <FieldBlock label="Budget each" value={requirement.budget} mono />
              <FieldBlock label="Year" value={requirement.year} mono />
              <FieldBlock label="Max kms" value={requirement.maxKms} mono />
            </dl>

            <p className="mt-4 text-body-sm text-[color:var(--text-secondary)]">
              {requirement.note}
            </p>
          </article>
        ))}
      </div>

      <section className="mt-10">
        <SectionHeading
          title="What the network is buying"
          description="Categories with open requirements right now, from the taxonomy registry."
          level={2}
          size="sm"
          rule
        />
        <div className="mt-4 flex flex-wrap gap-2">
          {categories.map((category) => (
            <Badge key={category.code} variant="outline">
              {category.name}
            </Badge>
          ))}
          {categories.length === 0 ? (
            <p className="text-body-sm text-[color:var(--text-secondary)]">
              The taxonomy registry is unreachable, so categories cannot be listed.
            </p>
          ) : null}
        </div>
      </section>
    </div>
  );
}

/**
 * Placeholder rows until the requirement-board endpoints land. Kept here, clearly named, rather
 * than dressed up as live data — a demo screen that quietly lies is worse than an honest stub.
 */
const DEMO_REQUIREMENTS = [
  {
    id: 'r1',
    title: 'Maruti Dzire — fleet refresh',
    quantity: 20,
    budget: '₹5.5L – ₹7L',
    year: '2019+',
    maxKms: '80,000',
    matches: 6,
    note: 'For an airport taxi contract. CNG preferred, single owner, transferable permit.',
  },
  {
    id: 'r2',
    title: 'Maruti Ertiga',
    quantity: 15,
    budget: '₹7L – ₹9L',
    year: '2020+',
    maxKms: '60,000',
    matches: 2,
    note: 'Corporate shuttle. Must clear a 200-point inspection before we take delivery.',
  },
  {
    id: 'r3',
    title: 'Tata Nexon EV',
    quantity: 5,
    budget: '₹9L – ₹12L',
    year: '2022+',
    maxKms: '40,000',
    matches: 0,
    note: 'Battery health above 85% and a full service record. No accident history.',
  },
] as const;
