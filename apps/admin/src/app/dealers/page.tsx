import type { Metadata } from 'next';
import { Badge, Button, DataTable, SectionHeading, type DataTableColumn } from '@autohub/ui';

export const metadata: Metadata = { title: 'Dealers' };

type DealerRow = {
  id: string; name: string; city: string; gstin: string;
  kyc: 'verified' | 'pending' | 'rejected'; tier: string; stock: number;
};

/**
 * Dealer approval gates the highest-margin part of the platform: only a verified dealer may bid,
 * and only a bank-verified dealer may be paid. Both checks are shown as separate columns because
 * they fail independently — a GSTIN can be valid while a penny-drop is still bouncing.
 */
export default function DealersPage() {
  const columns: Array<DataTableColumn<DealerRow>> = [
    { id: 'name', header: 'Dealer', cell: (row) => (<><span className="block">{row.name}</span><span className="block font-mono text-[0.6875rem] text-[color:var(--text-secondary)]">{row.city}</span></>) },
    { id: 'gstin', header: 'GSTIN', mono: true, hideBelow: 'md', cell: (row) => row.gstin },
    {
      id: 'kyc', header: 'KYC',
      cell: (row) => (
        <Badge variant={row.kyc === 'verified' ? 'clear' : row.kyc === 'pending' ? 'caution' : 'flag'} size="sm">
          {row.kyc}
        </Badge>
      ),
    },
    { id: 'tier', header: 'Tier', mono: true, cell: (row) => row.tier },
    { id: 'stock', header: 'Stock', align: 'end', mono: true, cell: (row) => String(row.stock) },
    {
      id: 'actions', header: '', align: 'end', width: '1%',
      cell: (row) => row.kyc === 'pending' ? <Button size="sm" variant="outline">Review</Button> : null,
    },
  ];

  return (
    <div className="mx-auto max-w-[var(--container-wide)] px-4 py-6">
      <SectionHeading
        eyebrow="Operations"
        title="Dealers"
        description="Verification gates bidding and payouts. Tier benefits come from the pricing registry, not from code."
        level={1}
      />
      <div className="mt-6">
        <DataTable columns={columns} rows={DEALERS} getRowId={(row) => row.id} caption="Registered dealers" />
      </div>
    </div>
  );
}

/** Placeholder rows until the dealer endpoints land — named so nobody mistakes them for live data. */
const DEALERS: DealerRow[] = [
  { id: 'd1', name: 'Sharma Motors', city: 'Pune', gstin: '27AAECS1234F1Z5', kyc: 'verified', tier: 'gold', stock: 42 },
  { id: 'd2', name: 'Deccan Auto Hub', city: 'Pune', gstin: '27AAFCD8871K1ZP', kyc: 'verified', tier: 'silver', stock: 18 },
  { id: 'd3', name: 'Andheri Car Bazaar', city: 'Mumbai', gstin: '27AAGCA4412M1Z2', kyc: 'pending', tier: 'bronze', stock: 7 },
  { id: 'd4', name: 'Whitefield Wheels', city: 'Bengaluru', gstin: '29AAHCW9920L1ZK', kyc: 'verified', tier: 'platinum', stock: 96 },
  { id: 'd5', name: 'Karol Bagh Motors', city: 'Delhi', gstin: '07AAJCK3312N1ZQ', kyc: 'pending', tier: 'bronze', stock: 3 },
  { id: 'd6', name: 'Nashik Auto Point', city: 'Nashik', gstin: '27AAKCN7781P1ZX', kyc: 'rejected', tier: 'bronze', stock: 0 },
];
