import type { Metadata } from 'next';
import { Suspense } from 'react';
import { SectionHeading, Skeleton } from '@autohub/ui';
import { InventoryTable } from '@/components/inventory/inventory-table';

export const metadata: Metadata = { title: 'Inventory' };

export default function InventoryPage() {
  return (
    <div className="mx-auto max-w-[var(--container-wide)] px-4 py-6">
      <SectionHeading
        eyebrow="Stock"
        title="Inventory"
        description="Every vehicle in this branch, with what it is costing you to hold it."
        level={1}
      />
      <div className="mt-6">
        <Suspense fallback={<Skeleton className="h-[28rem] w-full" />}>
          <InventoryTable />
        </Suspense>
      </div>
    </div>
  );
}
