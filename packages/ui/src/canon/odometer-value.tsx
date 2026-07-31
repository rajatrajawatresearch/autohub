'use client';

import { useEffect, useLayoutEffect, useState } from 'react';
import { cn } from '../lib/cn';
import { formatIndianMoney } from '../lib/format';

/**
 * The one animation in the entire product.
 *
 * When a valuation returns, the figure rolls up like a mechanical odometer — digits counting
 * into place, ~600ms, easing out, each one settling a beat after the one to its left. Once, on
 * the reveal. Nowhere else. Everything else in AutoHub is instant: no scroll reveals, no
 * page-load choreography, no ambient motion. The restraint is what makes this land.
 *
 * Under `prefers-reduced-motion` the final value is rendered immediately and nothing moves.
 * Server-rendered output is also the final value, so a failed hydration or a disabled-JS client
 * never reads a number that is not the number.
 */

export interface OdometerValueProps {
  /** Integer paise — the figure being revealed. */
  minorUnits: number;
  compact?: boolean;
  size?: 'md' | 'lg' | 'xl';
  durationMs?: number;
  className?: string;
}

const SIZES = {
  md: 'text-[length:1.5rem]',
  lg: 'text-[length:clamp(2rem,6vw,2.75rem)]',
  xl: 'text-[length:clamp(2.5rem,8vw,4rem)]',
} as const;

const DIGITS = ['0', '1', '2', '3', '4', '5', '6', '7', '8', '9'] as const;

/** The roll must be in place before the first paint, so the final value is never seen twice. */
const useIsomorphicLayoutEffect = typeof window === 'undefined' ? useEffect : useLayoutEffect;

export function OdometerValue({
  minorUnits,
  compact = false,
  size = 'lg',
  durationMs = 600,
  className,
}: OdometerValueProps) {
  const formatted = formatIndianMoney(minorUnits, { compact });
  const [phase, setPhase] = useState<'static' | 'start' | 'roll'>('static');

  useIsomorphicLayoutEffect(() => {
    const reduced = window.matchMedia('(prefers-reduced-motion: reduce)').matches;
    if (reduced) return;
    setPhase('start');
  }, [formatted]);

  useEffect(() => {
    if (phase !== 'start') return;
    let second = 0;
    const first = window.requestAnimationFrame(() => {
      second = window.requestAnimationFrame(() => setPhase('roll'));
    });
    return () => {
      window.cancelAnimationFrame(first);
      window.cancelAnimationFrame(second);
    };
  }, [phase]);

  let digitIndex = 0;

  return (
    <span
      className={cn(
        'inline-flex items-stretch whitespace-nowrap font-mono font-bold leading-none text-[color:var(--text-primary)]',
        SIZES[size],
        className,
      )}
    >
      <span className="sr-only">{formatted}</span>

      {Array.from(formatted).map((char, index) => {
        const isDigit = char >= '0' && char <= '9';

        if (!isDigit) {
          return (
            <span
              key={`${char}-${index}`}
              aria-hidden="true"
              className={cn(
                'inline-block h-[1.25em] leading-[1.25em]',
                char === '₹' && 'text-[0.85em]',
              )}
            >
              {char}
            </span>
          );
        }

        const value = Number(char);
        const delay = digitIndex * 45;
        digitIndex += 1;

        return (
          <span key={`${char}-${index}`} aria-hidden="true" className="inline-block h-[1.25em] overflow-hidden">
            <span
              className="flex flex-col"
              style={{
                transform: `translateY(-${(phase === 'start' ? 0 : value) * 10}%)`,
                transition:
                  phase === 'roll'
                    ? `transform ${durationMs}ms var(--ease-instrument) ${delay}ms`
                    : 'none',
              }}
            >
              {DIGITS.map((digit) => (
                <span key={digit} className="h-[1.25em] leading-[1.25em]">
                  {digit}
                </span>
              ))}
            </span>
          </span>
        );
      })}
    </span>
  );
}
