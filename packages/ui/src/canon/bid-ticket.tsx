'use client';

import { useEffect, useRef, useState, type ReactNode } from 'react';
import { cn } from '../lib/cn';
import { MoneyValue } from './money-value';

/**
 * Auction only — and the one place in the product where signal yellow is loud.
 *
 * A live lot is the single most consequential thing on any screen it appears on: money moving,
 * a clock running, a position that can flip while you read it. The countdown strip carries the
 * yellow; the action inside `children` should therefore stay primary green, so the viewport
 * still holds exactly one yellow element.
 *
 * The countdown is display only. Bids are placed against
 * `POST /auction/lots/{id}/bids` with an Idempotency-Key — never over the socket — so a dropped
 * connection can neither lose nor duplicate one.
 */

export interface BidTicketLabels {
  timeLeft: string;
  closed: string;
  currentBid: string;
  noBids: string;
  bids: string;
  leading: string;
  outbid: string;
  yourMax: string;
  extended: string;
}

const DEFAULT_LABELS: BidTicketLabels = {
  timeLeft: 'Time left',
  closed: 'Closed',
  currentBid: 'Current bid',
  noBids: 'No bids yet',
  bids: 'Bids',
  leading: 'You lead',
  outbid: 'Outbid',
  yourMax: 'Your maximum',
  extended: 'Extended — bid in the last minutes moves the close',
};

export interface BidTicketProps {
  /** Integer paise, or null while the lot has no bids. */
  currentBidMinor?: number | null;
  /** ISO-8601 UTC close time. Re-passed on every `lot.extended` event. */
  endsAt: string;
  position?: 'leading' | 'outbid' | 'none';
  bidCount?: number;
  yourMaxMinor?: number | null;
  /** True after an anti-snipe extension, so the notice explains why the clock grew. */
  extended?: boolean;
  onEnd?: () => void;
  /** en-IN defaults; pass resolved strings from the localisation registry in production. */
  labels?: Partial<BidTicketLabels>;
  /** The bid control itself — the app owns the action, the ticket owns the state. */
  children?: ReactNode;
  className?: string;
}

const PLACEHOLDER = '--:--:--';

function pad(value: number): string {
  return value < 10 ? `0${value}` : String(value);
}

function formatRemaining(ms: number): string {
  const total = Math.floor(ms / 1000);
  const days = Math.floor(total / 86400);
  const hours = Math.floor((total % 86400) / 3600);
  const minutes = Math.floor((total % 3600) / 60);
  const seconds = total % 60;

  return days > 0
    ? `${days}d ${pad(hours)}:${pad(minutes)}:${pad(seconds)}`
    : `${pad(hours)}:${pad(minutes)}:${pad(seconds)}`;
}

export function BidTicket({
  currentBidMinor,
  endsAt,
  position = 'none',
  bidCount,
  yourMaxMinor,
  extended = false,
  onEnd,
  labels,
  children,
  className,
}: BidTicketProps) {
  const resolved = { ...DEFAULT_LABELS, ...labels };
  /** null until mounted: the server cannot know "now", and a guess would hydrate wrong. */
  const [remaining, setRemaining] = useState<number | null>(null);
  const onEndRef = useRef(onEnd);
  const firedRef = useRef(false);

  onEndRef.current = onEnd;

  useEffect(() => {
    const closesAt = new Date(endsAt).getTime();
    if (Number.isNaN(closesAt)) return;

    firedRef.current = false;

    const tick = () => {
      const left = Math.max(0, closesAt - Date.now());
      setRemaining(left);
      if (left === 0 && !firedRef.current) {
        firedRef.current = true;
        onEndRef.current?.();
      }
    };

    tick();
    const timer = window.setInterval(tick, 1000);
    return () => window.clearInterval(timer);
  }, [endsAt]);

  const live = remaining === null || remaining > 0;

  return (
    <section
      className={cn(
        'flex min-w-0 flex-col overflow-hidden rounded-card border border-[color:var(--border-hairline)] bg-[var(--surface-card)]',
        className,
      )}
    >
      <div
        className={cn(
          'flex flex-wrap items-baseline justify-between gap-x-4 gap-y-1 px-4 py-2.5',
          live
            ? 'bg-signal text-[color:var(--color-tarmac)]'
            : 'border-b border-[color:var(--border-hairline)] bg-[var(--surface-sunken)] text-[color:var(--text-secondary)]',
        )}
      >
        <span
          className={cn(
            'text-[length:var(--text-label)] font-semibold uppercase leading-[1.2] tracking-[0.08em]',
            live && 'text-[color:var(--color-tarmac)]',
          )}
        >
          {live ? resolved.timeLeft : resolved.closed}
        </span>
        <span
          aria-live="off"
          className="font-mono text-[length:1.375rem] font-bold leading-none tracking-[0.04em]"
        >
          {remaining === null ? PLACEHOLDER : formatRemaining(remaining)}
        </span>
      </div>

      {extended && live ? (
        <p className="border-b border-[color:var(--border-hairline)] border-l-2 border-l-[color:var(--color-caution)] bg-[var(--color-caution-soft)] px-4 py-2 text-[length:var(--text-body-sm)] leading-[1.45] text-[color:var(--color-caution)]">
          {resolved.extended}
        </p>
      ) : null}

      <div className="flex min-w-0 flex-col gap-3 p-4">
        <div className="flex flex-wrap items-end justify-between gap-x-4 gap-y-2">
          <div className="flex min-w-0 flex-col gap-1">
            <span className="ah-label">{resolved.currentBid}</span>
            {typeof currentBidMinor === 'number' ? (
              <MoneyValue minorUnits={currentBidMinor} size="xl" />
            ) : (
              <span className="font-mono text-[length:1.5rem] font-semibold leading-[1.2] text-[color:var(--text-secondary)]">
                {resolved.noBids}
              </span>
            )}
          </div>

          {typeof bidCount === 'number' ? (
            <div className="flex flex-col items-end gap-1">
              <span className="ah-label">{resolved.bids}</span>
              <span className="font-mono text-[length:var(--text-data)] font-medium text-[color:var(--text-primary)]">
                {bidCount}
              </span>
            </div>
          ) : null}
        </div>

        {position !== 'none' ? (
          <p
            className={cn(
              'border-l-2 px-2 py-1 text-[length:var(--text-body-sm)] font-semibold leading-[1.45]',
              position === 'leading'
                ? 'border-[color:var(--color-clear)] bg-[var(--color-clear-soft)] text-[color:var(--color-clear)]'
                : 'border-[color:var(--color-flag)] bg-[var(--color-flag-soft)] text-[color:var(--color-flag)]',
            )}
          >
            {position === 'leading' ? resolved.leading : resolved.outbid}
          </p>
        ) : null}

        {typeof yourMaxMinor === 'number' ? (
          <div className="flex items-baseline justify-between gap-4 border-t border-[color:var(--border-hairline)] pt-3">
            <span className="ah-label">{resolved.yourMax}</span>
            <MoneyValue minorUnits={yourMaxMinor} size="sm" tone="secondary" />
          </div>
        ) : null}

        {children ? <div className="mt-1">{children}</div> : null}
      </div>
    </section>
  );
}
