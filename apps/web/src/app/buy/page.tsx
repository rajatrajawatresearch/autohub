import type { Metadata } from 'next';
import { SectionHeading } from '@autohub/ui';
import { SearchSection } from '@/components/search/search-section';

export const metadata: Metadata = {
  title: 'Buy a used car',
  description:
    'Inspected, RC-verified used cars from verified dealers and owners. Filter by make, fuel, kms and price.',
};

export default function BuyPage() {
  return (
    <>
      <div className="border-b border-[color:var(--border-hairline)]">
        <div className="mx-auto max-w-[var(--container-content)] px-4 py-10 sm:px-6">
          <SectionHeading
            eyebrow="Buy"
            title="Inspected vehicles, RC verified"
            description="Every listing carries a 200-point inspection and an ownership check against VAHAN."
            level={1}
            size="lg"
          />
        </div>
      </div>
      <SearchSection intent="sale" />
    </>
  );
}
