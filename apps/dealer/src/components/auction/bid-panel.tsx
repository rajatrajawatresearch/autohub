'use client';

import { useEffect, useState } from 'react';
import {
  ApiError,
  useAuctionLot,
  useAuctionStream,
  usePlaceBid,
  type AuctionLot,
} from '@autohub/api';
import { BidTicket, Button, FieldBlock, Input, MoneyValue, Skeleton, cn } from '@autohub/ui';

/**
 * Placing a bid, with the two safeguards that matter on a live floor.
 *
 * 1. A confirm step. The amount you are about to commit is restated in words you have to read
 *    before the second click. One mis-click on a wall of countdowns is an expensive car.
 * 2. `seenBidSeq`. The bid carries the highest sequence this screen has actually rendered, so a
 *    bid composed against a stale figure is rejected by the server as an outbid race rather than
 *    silently accepted at the wrong ladder step. When that happens the answer is to refresh the
 *    truth, never to guess the next increment locally.
 *
 * The socket is read-only. Bids go over HTTP with an idempotency key, so a dropped connection can
 * neither lose a bid nor place it twice.
 */
export function BidPanel({ lotId }: { lotId: string }) {
  const lot = useAuctionLot(lotId);
  const stream = useAuctionStream(lotId);
  const placeBid = usePlaceBid(lotId);

  const [amount, setAmount] = useState('');
  const [confirming, setConfirming] = useState(false);

  // Follow the ladder as the floor moves, until the dealer types their own figure.
  useEffect(() => {
    if (lot.data && !confirming && amount === '') {
      setAmount(String(Math.round(lot.data.minNextBidMinor / 100)));
    }
  }, [lot.data, confirming, amount]);

  if (lot.isLoading || !lot.data) {
    return <Skeleton className="h-72 w-full" />;
  }

  const data: AuctionLot = lot.data;
  const rupees = Number(amount.replace(/\D/g, '') || 0);
  const amountMinor = rupees * 100;
  const belowLadder = amountMinor < data.minNextBidMinor;
  const closed = data.state.isTerminal;

  async function submit() {
    if (belowLadder || closed) return;
    try {
      await placeBid.mutateAsync({
        amountMinor,
        seenBidSeq: data.latestSeq,
      });
      setConfirming(false);
    } catch {
      // The mutation already invalidated the lot; the panel re-renders against the server's figure.
      setConfirming(false);
    }
  }

  return (
    <div className="rounded-[var(--radius-card)] border border-[color:var(--border-hairline)] bg-[color:var(--surface-card)] p-4">
      <BidTicket
        currentBidMinor={data.currentBidMinor}
        endsAt={data.endsAt}
        bidCount={data.bidCount}
        position={data.highestBidderIsYou ? 'leading' : data.bidCount > 0 ? 'outbid' : 'none'}
        yourMaxMinor={data.yourMaxBidMinor}
        extended={data.extensionCount > 0}
      />

      <dl className="mt-4 grid grid-cols-2 gap-4">
        <FieldBlock
          label="Minimum next bid"
          value={<MoneyValue minorUnits={data.minNextBidMinor} size="sm" />}
          mono
        />
        <FieldBlock
          label="Increment"
          value={<MoneyValue minorUnits={data.incrementMinor} size="sm" />}
          mono
        />
      </dl>

      {closed ? (
        <p className="mt-4 text-body-sm text-[color:var(--text-secondary)]">
          This lot has closed. Results are on the deals page.
        </p>
      ) : confirming ? (
        <div className="mt-4 border-t border-[color:var(--border-hairline)] pt-4">
          <p className="text-body-sm text-[color:var(--text-primary)]">
            Place a binding bid of{' '}
            <strong className="font-mono">
              <MoneyValue minorUnits={amountMinor} size="sm" />
            </strong>{' '}
            on {data.listing.registration.display}?
          </p>
          <p className="mt-1.5 text-body-sm text-[color:var(--text-secondary)]">
            Winning opens a deal with a payment window. Missing it forfeits your deposit.
          </p>
          <div className="mt-3 flex gap-2">
            <Button variant="action" className="flex-1" onClick={submit} disabled={placeBid.isPending}>
              {placeBid.isPending ? 'Placing…' : 'Confirm bid'}
            </Button>
            <Button variant="outline" onClick={() => setConfirming(false)}>
              Cancel
            </Button>
          </div>
        </div>
      ) : (
        <div className="mt-4 border-t border-[color:var(--border-hairline)] pt-4">
          <label className="block">
            <span className="ah-label">Your bid (₹)</span>
            <Input
              inputMode="numeric"
              value={amount}
              onChange={(event) => setAmount(event.target.value.replace(/\D/g, '').slice(0, 9))}
              aria-label="Bid amount in rupees"
              invalid={belowLadder}
              className="mt-1 font-mono"
            />
          </label>

          <p
            className={cn(
              'mt-1.5 font-mono text-[0.6875rem]',
              belowLadder ? 'text-[color:var(--color-flag)]' : 'text-[color:var(--text-secondary)]',
            )}
          >
            {belowLadder
              ? `Below the ladder. Minimum is ₹${Math.round(data.minNextBidMinor / 100).toLocaleString('en-IN')}.`
              : `Ladder step ₹${Math.round(data.incrementMinor / 100).toLocaleString('en-IN')}`}
          </p>

          <Button
            variant="action"
            className="mt-3 w-full"
            onClick={() => setConfirming(true)}
            disabled={belowLadder}
          >
            Place bid
          </Button>
        </div>
      )}

      {placeBid.isError ? (
        <p className="mt-3 text-body-sm text-[color:var(--color-flag)]">
          {bidError(placeBid.error)}
        </p>
      ) : null}

      <p className="mt-4 font-mono text-[0.6875rem] text-[color:var(--text-secondary)]">
        Live feed {stream.status === 'open' ? 'connected' : stream.status}
      </p>
    </div>
  );
}

function bidError(error: unknown): string {
  const code = error instanceof ApiError ? error.code : undefined;

  switch (code) {
    case 'auction.bid.outbid_race':
      return 'Someone bid while you were typing. The figure above is now current — bid again if you still want it.';
    case 'auction.bid.below_increment':
      return 'That is under the ladder step. Raise it to the minimum shown.';
    case 'auction.lot.closed':
      return 'This lot closed before the bid landed.';
    case 'auction.bidder.not_verified':
      return 'Your dealer KYC is not complete, so bidding is off. Finish it in Settings.';
    default:
      return 'The bid did not go through. Check the current figure before trying again.';
  }
}
