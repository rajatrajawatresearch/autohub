import type { Metadata } from 'next';
import { SectionHeading } from '@autohub/ui';
import { SearchSection } from '@/components/search/search-section';

export const metadata: Metadata = {
  title: 'Rent a vehicle',
  description: 'Self-drive and chauffeur-driven rentals by the day, week or month.',
};

/**
 * Rent is the same search shell with a different intent — which is the north star's "one Listing,
 * many intents" claim made visible. If this page needed its own results component, the model would
 * be wrong.
 */
export default function RentPage() {
  return (
    <>
      <div className="border-b border-[color:var(--border-hairline)]">
        <div className="mx-auto max-w-[var(--container-content)] px-4 py-10 sm:px-6">
          <SectionHeading
            eyebrow="Rent"
            title="Self-drive or with a driver"
            description="Daily, weekly and monthly. Security deposit refunded on return, minus fuel and challans."
            level={1}
            size="lg"
          />
        </div>
      </div>
      <SearchSection intent="rental" />
    </>
  );
}
