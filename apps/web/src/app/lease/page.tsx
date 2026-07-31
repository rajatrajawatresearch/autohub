import type { Metadata } from 'next';
import { SectionHeading } from '@autohub/ui';
import { SearchSection } from '@/components/search/search-section';

export const metadata: Metadata = {
  title: 'Lease a vehicle',
  description: 'Corporate, individual and fleet leases with a fixed monthly cost.',
};

export default function LeasePage() {
  return (
    <>
      <div className="border-b border-[color:var(--border-hairline)]">
        <div className="mx-auto max-w-[var(--container-content)] px-4 py-10 sm:px-6">
          <SectionHeading
            eyebrow="Lease"
            title="One monthly figure, everything included"
            description="Insurance, maintenance and roadside cover in the monthly payment. Return, renew or buy at the end."
            level={1}
            size="lg"
          />
        </div>
      </div>
      <SearchSection intent="lease" />
    </>
  );
}
