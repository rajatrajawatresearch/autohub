/**
 * Mock auction — lots, a bid ladder, and a synthetic broadcast.
 *
 * The state here is mutable on purpose. In a demo the interesting moment is placing a bid and
 * watching the number move, the clock extend, and a rival come back thirty seconds later. Fixtures
 * that only ever return the same JSON cannot show that.
 */

import type { z } from 'zod';
import type { AuctionEventSchema, AuctionLotSchema, BidSchema, ProblemDetails } from '../schemas';
import { MOCK_VEHICLES, toSearchItem } from './vehicles';
import { MOCK_WORKFLOWS } from './workflows';

type BidInput = z.input<typeof BidSchema>;
type AuctionLotInput = z.input<typeof AuctionLotSchema>;
type AuctionEventInput = z.input<typeof AuctionEventSchema>;

interface LotState {
  lotId: string;
  lotNumber: string;
  listingId: string;
  stateCode: string;
  startsAt: number;
  endsAt: number;
  startPriceMinor: number;
  incrementMinor: number;
  reserveMinor: number;
  extensionCount: number;
  bids: BidInput[];
  nextSeq: number;
}

const RIVAL_MASKS = ['Dealer ••42', 'Dealer ••07', 'Dealer ••88', 'Dealer ••15'];

function minutesFromNow(minutes: number): number {
  return Date.now() + minutes * 60_000;
}

function seedBids(lot: LotState, count: number): void {
  let amount = lot.startPriceMinor;
  for (let i = 0; i < count; i += 1) {
    amount += lot.incrementMinor;
    lot.nextSeq += 1;
    lot.bids.push({
      bidId: `${lot.lotId}-bid-${lot.nextSeq}`,
      lotId: lot.lotId,
      seq: lot.nextSeq,
      amountMinor: amount,
      bidderMask: RIVAL_MASKS[i % RIVAL_MASKS.length] ?? 'Dealer ••00',
      placedAt: new Date(Date.now() - (count - i) * 4 * 60_000).toISOString(),
      isYours: false,
      isAutoBid: i % 3 === 2,
    });
  }
}

function createLots(): LotState[] {
  const auctionVehicles = MOCK_VEHICLES.filter((v) => v.intent === 'auction');

  const lots: LotState[] = auctionVehicles.map((vehicle, index) => {
    const startPrice = Math.round(vehicle.priceMinor * 0.78);
    return {
      lotId: `019fabaa-000${index + 1}-7000-8000-00000000000${index + 1}`,
      lotNumber: `L-${String(index + 1).padStart(3, '0')}`,
      listingId: vehicle.listingId,
      stateCode: index === 2 ? 'scheduled' : 'live',
      startsAt: index === 2 ? minutesFromNow(90) : minutesFromNow(-140),
      endsAt: index === 2 ? minutesFromNow(210) : index === 0 ? minutesFromNow(7) : minutesFromNow(52),
      startPriceMinor: startPrice,
      incrementMinor: startPrice > 100000000 ? 1000000 : 500000,
      reserveMinor: Math.round(vehicle.priceMinor * 0.94),
      extensionCount: 0,
      bids: [],
      nextSeq: 1000,
    };
  });

  const first = lots[0];
  const second = lots[1];
  if (first) seedBids(first, 9);
  if (second) seedBids(second, 4);
  return lots;
}

const LOTS: LotState[] = createLots();

function currentBid(lot: LotState): BidInput | undefined {
  return lot.bids[lot.bids.length - 1];
}

function minNextBid(lot: LotState): number {
  const top = currentBid(lot);
  return top ? top.amountMinor + lot.incrementMinor : lot.startPriceMinor;
}

function lotWorkflowState(stateCode: string) {
  const definition = MOCK_WORKFLOWS.auction_lot;
  const state = definition?.states.find((s) => s.code === stateCode);
  return state ?? { code: stateCode, name: stateCode, category: 'in_progress' };
}

function toAuctionLot(lot: LotState): AuctionLotInput | null {
  const vehicle = MOCK_VEHICLES.find((v) => v.listingId === lot.listingId);
  if (!vehicle) return null;
  const top = currentBid(lot);

  return {
    lotId: lot.lotId,
    lotNumber: lot.lotNumber,
    listing: toSearchItem(vehicle),
    currency: 'INR',
    state: lotWorkflowState(lot.stateCode),
    startsAt: new Date(lot.startsAt).toISOString(),
    endsAt: new Date(lot.endsAt).toISOString(),
    startPriceMinor: lot.startPriceMinor,
    incrementMinor: lot.incrementMinor,
    currentBidMinor: top?.amountMinor ?? null,
    minNextBidMinor: minNextBid(lot),
    bidCount: lot.bids.length,
    reserveMet: (top?.amountMinor ?? 0) >= lot.reserveMinor,
    latestSeq: lot.nextSeq,
    highestBidderIsYou: top?.isYours ?? false,
    yourMaxBidMinor: null,
    extensionCount: lot.extensionCount,
    inspectionReportUrl: null,
  };
}

export function mockAuctionLots(query: Record<string, string>): {
  items: AuctionLotInput[];
  nextCursor: null;
  totalEstimate: number;
} {
  const items = LOTS.filter((lot) => (query.state ? lot.stateCode === query.state : true))
    .map(toAuctionLot)
    .filter((lot): lot is AuctionLotInput => lot !== null);
  return { items, nextCursor: null, totalEstimate: items.length };
}

export function mockAuctionLot(lotId: string): AuctionLotInput | null {
  const lot = LOTS.find((l) => l.lotId === lotId);
  return lot ? toAuctionLot(lot) : null;
}

export function mockLotBids(lotId: string): { items: BidInput[]; nextCursor: null } {
  const lot = LOTS.find((l) => l.lotId === lotId);
  return { items: lot ? [...lot.bids].reverse() : [], nextCursor: null };
}

export type MockBidResult =
  | { ok: true; body: { bid: BidInput; lot: AuctionLotInput } }
  | { ok: false; status: number; problem: ProblemDetails };

/** Anti-snipe: a bid inside the last two minutes pushes the close out by two minutes. */
const ANTI_SNIPE_WINDOW_MS = 120_000;

export function mockPlaceBid(
  lotId: string,
  body: { amountMinor?: number; seenBidSeq?: number },
): MockBidResult {
  const lot = LOTS.find((l) => l.lotId === lotId);
  if (!lot) {
    return {
      ok: false,
      status: 404,
      problem: {
        code: 'auction.auction.not_found',
        title: 'Lot not found',
        status: 404,
        detail: 'This lot is no longer in the sale.',
      },
    };
  }

  if (lot.stateCode !== 'live') {
    return {
      ok: false,
      status: 409,
      problem: {
        code: 'auction.auction.not_started',
        title: 'Bidding has not opened',
        status: 409,
        detail: 'Bidding opens when the lot goes live.',
        metadata: { startsAt: new Date(lot.startsAt).toISOString() },
      },
    };
  }

  if (Date.now() > lot.endsAt) {
    return {
      ok: false,
      status: 409,
      problem: {
        code: 'auction.auction.closed',
        title: 'Lot closed',
        status: 409,
        detail: 'This lot closed before the bid reached us.',
      },
    };
  }

  const amount = body.amountMinor ?? 0;
  const minimum = minNextBid(lot);
  if (amount < minimum) {
    return {
      ok: false,
      status: 422,
      problem: {
        code: 'auction.bid.below_increment',
        title: 'Bid is below the minimum increment',
        status: 422,
        detail: 'Raise the bid to at least the minimum next bid.',
        errors: [
          {
            pointer: '/amountMinor',
            code: 'auction.bid.below_increment',
            params: { minNextMinor: minimum, incrementMinor: lot.incrementMinor },
          },
        ],
        metadata: { minNextMinor: minimum, incrementMinor: lot.incrementMinor },
      },
    };
  }

  if (body.seenBidSeq !== undefined && body.seenBidSeq < lot.nextSeq) {
    return {
      ok: false,
      status: 409,
      problem: {
        code: 'auction.bid.outbid_race',
        title: 'Someone bid first',
        status: 409,
        detail: 'The bid moved while you were typing. Check the new figure and bid again.',
        metadata: { latestSeq: lot.nextSeq, minNextMinor: minimum },
      },
    };
  }

  lot.nextSeq += 1;
  const bid: BidInput = {
    bidId: `${lot.lotId}-bid-${lot.nextSeq}`,
    lotId: lot.lotId,
    seq: lot.nextSeq,
    amountMinor: amount,
    bidderMask: 'You',
    placedAt: new Date().toISOString(),
    isYours: true,
    isAutoBid: false,
  };
  lot.bids.push(bid);

  if (lot.endsAt - Date.now() < ANTI_SNIPE_WINDOW_MS) {
    lot.endsAt += ANTI_SNIPE_WINDOW_MS;
    lot.extensionCount += 1;
  }

  const projected = toAuctionLot(lot);
  if (!projected) {
    return {
      ok: false,
      status: 404,
      problem: { code: 'auction.auction.not_found', status: 404, title: 'Lot not found' },
    };
  }
  return { ok: true, body: { bid, lot: projected } };
}

/* ── Synthetic broadcast ─────────────────────────────────────────────────── */

/**
 * Stands in for `WSS /realtime/auction/{lotId}`. A rival bids every so often so the live view has
 * something to be live about; the close extends when a bid lands inside the anti-snipe window.
 */
export function subscribeMockAuction(
  lotId: string,
  onEvent: (event: AuctionEventInput) => void,
): () => void {
  const lot = LOTS.find((l) => l.lotId === lotId);
  if (!lot) return () => undefined;

  const timer = setInterval(() => {
    if (lot.stateCode !== 'live') return;

    if (Date.now() > lot.endsAt) {
      lot.stateCode = 'sold';
      lot.nextSeq += 1;
      onEvent({
        seq: lot.nextSeq,
        type: 'lot.closed',
        data: {
          outcome: 'sold',
          winnerIsYou: currentBid(lot)?.isYours ?? false,
          finalAmountMinor: currentBid(lot)?.amountMinor ?? null,
        },
      });
      return;
    }

    const amount = minNextBid(lot);
    lot.nextSeq += 1;
    const rival: BidInput = {
      bidId: `${lot.lotId}-bid-${lot.nextSeq}`,
      lotId: lot.lotId,
      seq: lot.nextSeq,
      amountMinor: amount,
      bidderMask: RIVAL_MASKS[lot.nextSeq % RIVAL_MASKS.length] ?? 'Dealer ••00',
      placedAt: new Date().toISOString(),
      isYours: false,
      isAutoBid: false,
    };
    lot.bids.push(rival);

    onEvent({
      seq: rival.seq,
      type: 'bid.placed',
      data: {
        lotId: lot.lotId,
        amountMinor: rival.amountMinor,
        bidderMask: rival.bidderMask,
        placedAt: rival.placedAt,
        highestBidderIsYou: false,
        bidCount: lot.bids.length,
        minNextBidMinor: minNextBid(lot),
      },
    });

    if (lot.endsAt - Date.now() < ANTI_SNIPE_WINDOW_MS) {
      lot.endsAt += ANTI_SNIPE_WINDOW_MS;
      lot.extensionCount += 1;
      lot.nextSeq += 1;
      onEvent({
        seq: lot.nextSeq,
        type: 'lot.extended',
        data: { endsAt: new Date(lot.endsAt).toISOString(), reason: 'anti_snipe' },
      });
    }
  }, 14_000);

  return () => clearInterval(timer);
}
