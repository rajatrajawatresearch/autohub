/**
 * Mock deals — the settlement spine with its fee breakdown attached, because the fee breakdown is
 * the part a dealer actually reads.
 */

import type { z } from 'zod';
import type { DealDetailSchema, DealSummarySchema } from '../schemas';
import { mockPricingQuote } from './pricing';
import { MOCK_VEHICLES, toSearchItem } from './vehicles';
import { mockWorkflowSnapshot } from './workflows';

type DealSummaryInput = z.input<typeof DealSummarySchema>;
type DealDetailInput = z.input<typeof DealDetailSchema>;

interface MockDeal {
  dealId: string;
  reference: string;
  listingId: string;
  amountMinor: number;
  stateCode: string;
  side: 'buying' | 'selling';
  counterpartyName: string;
  counterpartyRole: string;
  createdDaysAgo: number;
}

const DEALS: MockDeal[] = [
  {
    dealId: '019fabcc-0001-7000-8000-000000000001',
    reference: 'AH-D-2026-04417',
    listingId: '019fab0c-0001-7000-8000-000000000001',
    amountMinor: 53500000,
    stateCode: 'escrow_funded',
    side: 'buying',
    counterpartyName: 'Rohit Kulkarni',
    counterpartyRole: 'seller',
    createdDaysAgo: 4,
  },
  {
    dealId: '019fabcc-0002-7000-8000-000000000002',
    reference: 'AH-D-2026-04502',
    listingId: '019fab0c-0002-7000-8000-000000000002',
    amountMinor: 125000000,
    stateCode: 'offer_made',
    side: 'selling',
    counterpartyName: 'Nikhil Raghavan',
    counterpartyRole: 'buyer',
    createdDaysAgo: 1,
  },
  {
    dealId: '019fabcc-0003-7000-8000-000000000003',
    reference: 'AH-D-2026-04390',
    listingId: '019fab0c-0010-7000-8000-000000000010',
    amountMinor: 83000000,
    stateCode: 'completed',
    side: 'selling',
    counterpartyName: 'Ahmedabad Motors',
    counterpartyRole: 'buyer',
    createdDaysAgo: 22,
  },
];

const DOCUMENT_SET = [
  { code: 'rc', label: 'RC', status: 'verified' },
  { code: 'insurance', label: 'Insurance', status: 'verified' },
  { code: 'form_29', label: 'Form 29', status: 'pending' },
  { code: 'form_30', label: 'Form 30', status: 'pending' },
  { code: 'noc', label: 'NOC', status: 'not_required' },
];

function daysAgoIso(days: number): string {
  return new Date(Date.now() - days * 86_400_000).toISOString();
}

function stateOf(stateCode: string) {
  const snapshot = mockWorkflowSnapshot('deal', stateCode);
  return snapshot?.currentState ?? { code: stateCode, name: stateCode, category: 'in_progress' };
}

function toSummary(deal: MockDeal): DealSummaryInput | null {
  const vehicle = MOCK_VEHICLES.find((v) => v.listingId === deal.listingId);
  if (!vehicle) return null;
  return {
    dealId: deal.dealId,
    reference: deal.reference,
    listing: toSearchItem(vehicle),
    currency: 'INR',
    amountMinor: deal.amountMinor,
    state: stateOf(deal.stateCode),
    counterparty: {
      id: `${deal.dealId}-cp`,
      displayName: deal.counterpartyName,
      role: deal.counterpartyRole,
      isYou: false,
      phone: null,
    },
    createdAt: daysAgoIso(deal.createdDaysAgo),
    updatedAt: daysAgoIso(Math.max(0, deal.createdDaysAgo - 1)),
  };
}

export function mockDealList(query: Record<string, string>): {
  items: DealSummaryInput[];
  nextCursor: null;
  totalEstimate: number;
} {
  const items = DEALS.filter((deal) => (query.side ? deal.side === query.side : true))
    .filter((deal) => (query.state ? deal.stateCode === query.state : true))
    .map(toSummary)
    .filter((deal): deal is DealSummaryInput => deal !== null);
  return { items, nextCursor: null, totalEstimate: items.length };
}

export function mockDealDetail(dealId: string): DealDetailInput | null {
  const deal = DEALS.find((d) => d.dealId === dealId);
  if (!deal) return null;
  const summary = toSummary(deal);
  if (!summary) return null;

  const vehicle = MOCK_VEHICLES.find((v) => v.listingId === deal.listingId);
  const quote = mockPricingQuote({
    scope: 'deal.settlement',
    context: {
      vehicle: { category: vehicle?.categoryCode, makeCode: vehicle?.makeCode, year: vehicle?.year },
      location: { city: vehicle?.cityCode, state: vehicle?.plate.slice(0, 2) },
      party: { type: 'dealer', tier: 'gold' },
      amount: { minorUnits: deal.amountMinor, currency: 'INR' },
    },
  });

  return {
    ...summary,
    parties: [
      {
        id: `${deal.dealId}-you`,
        displayName: 'You',
        role: deal.side === 'buying' ? 'buyer' : 'seller',
        isYou: true,
      },
      {
        id: `${deal.dealId}-cp`,
        displayName: deal.counterpartyName,
        role: deal.counterpartyRole,
        isYou: false,
      },
    ],
    fees: quote.lines,
    totalFeesMinor: quote.totalFeesMinor,
    payableMinor: quote.payableMinor,
    escrowHeldMinor: deal.stateCode === 'escrow_funded' ? deal.amountMinor : 0,
    documents: DOCUMENT_SET.map((doc, index) => ({
      id: `${deal.dealId}-doc-${index + 1}`,
      code: doc.code,
      label: doc.label,
      status: doc.status,
      url: null,
      uploadedAt: doc.status === 'verified' ? daysAgoIso(deal.createdDaysAgo - 1) : null,
    })),
    workflow: mockWorkflowSnapshot('deal', deal.stateCode),
  };
}
