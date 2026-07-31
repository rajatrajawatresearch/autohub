import type { Metadata } from 'next';
import { SectionHeading } from '@autohub/ui';
import { SellFlow } from '@/components/sell/sell-flow';

export const metadata: Metadata = {
  title: 'Sell your vehicle',
  description:
    'One form. Choose a dealer auction, an instant offer, or list it yourself. Free inspection at home.',
};

/**
 * The architecture proving itself.
 *
 * There is not a single vehicle field written by hand on this page. Every step, field, validation
 * rule and conditional in the sell flow comes from the Form Schema Registry, which means adding
 * "battery health" for EVs — or a whole new vehicle category — is a database row, on web and on
 * both mobile apps at once. If this file ever grows an `<input>`, the bet has been lost.
 */
export default function SellPage() {
  return (
    <div className="mx-auto max-w-[var(--container-content)] px-4 py-10 sm:px-6">
      <div className="mx-auto max-w-[44rem]">
        <SectionHeading
          eyebrow="Sell"
          title="Tell us about your vehicle"
          description="Takes about four minutes. You will see dealer offers before you commit to anything."
          level={1}
          size="lg"
        />
        <div className="mt-10">
          <SellFlow />
        </div>
      </div>
    </div>
  );
}
