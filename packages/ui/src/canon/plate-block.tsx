import { cva, type VariantProps } from 'class-variance-authority';
import type { HTMLAttributes } from 'react';
import { cn } from '../lib/cn';

/**
 * The signature.
 *
 * Every vehicle in AutoHub is identified by a rendering of its actual registration plate — in
 * search, in an auction lot, in a dealer's inventory table, on an inspection sheet. It is not
 * decoration: it is the primary key made visible, and it is the one thing a user can match
 * against the object standing in front of them.
 *
 * The plate colour is real information and follows how Indian plates actually work, so it does
 * not flip with the theme — a commercial plate is yellow at 3am on a dealer's dark screen for
 * the same reason it is yellow in a car park.
 */

/**
 * The five plate classes that exist on Indian roads, coloured the way they actually are:
 *
 *   private     white plate, black lettering
 *   commercial  yellow plate, black lettering — taxis, goods carriers, anything on a permit
 *   ev          green plate, white lettering
 *   government  white plate, black lettering — distinguished by an emblem, not a colour
 *   defence     black plate, white lettering
 *
 * These are not theme decisions and they do not flip in dark mode. A user matching a plate on
 * screen against the car in front of them needs the two to agree.
 */
export type PlateClass = 'private' | 'commercial' | 'ev' | 'government' | 'defence';

/**
 * Groups a canonical registration ("MH12AB1234") the way a person proofreads it off a
 * windscreen: "MH 12 AB 1234".
 *
 * Splitting on runs of letters and digits covers every series without a per-format table —
 * the old series (MH12AB1234), the BH series (22BH1234AA), armed-forces and partial strings
 * typed one character at a time — which is why PlateInput shares it.
 */
export function groupRegistration(value: string): string {
  const canonical = value.replace(/[^0-9A-Za-z]/g, '').toUpperCase();
  return canonical.match(/[0-9]+|[A-Z]+/g)?.join(' ') ?? '';
}

const plateBlock = cva(
  'relative inline-flex items-center justify-center whitespace-nowrap rounded-plate border-2 font-mono font-bold uppercase',
  {
    variants: {
      size: {
        xs: 'px-1.5 py-[0.125rem] text-[length:0.6875rem] leading-[1.15] tracking-[0.1em]',
        sm: 'px-2 py-1 text-[length:0.8125rem] leading-[1.15] tracking-[0.12em]',
        md: 'px-2.5 py-1.5 text-[length:1rem] leading-[1.15] tracking-[0.14em]',
        lg: 'px-4 py-2.5 text-[length:var(--text-plate)] leading-[1.1] tracking-[0.14em]',
      },
      plateClass: {
        private: 'border-[color:var(--color-tarmac)] bg-plate text-[color:var(--color-tarmac)]',
        commercial: 'border-[color:var(--color-tarmac)] bg-signal text-[color:var(--color-tarmac)]',
        ev: 'border-[color:var(--color-highway)] bg-highway text-[color:var(--color-plate)]',
        government: 'border-[color:var(--color-tarmac)] bg-plate text-[color:var(--color-tarmac)]',
        defence: 'border-[color:var(--color-tarmac)] bg-tarmac text-[color:var(--color-plate)]',
      },
    },
    defaultVariants: { size: 'md', plateClass: 'private' },
  },
);

export interface PlateBlockProps
  extends Omit<HTMLAttributes<HTMLSpanElement>, 'children'>,
    VariantProps<typeof plateBlock> {
  /** Canonical registration from the API, e.g. "MH12AB1234". */
  registration: string;
  /**
   * The server's pre-grouped `registration.display` when there is one. Trusting it keeps the
   * client from re-deriving a formatting decision the registry already made.
   */
  display?: string;
}

export function PlateBlock({
  registration,
  display,
  size,
  plateClass = 'private',
  className,
  ...rest
}: PlateBlockProps) {
  const grouped = display && display.trim() ? display.trim().toUpperCase() : groupRegistration(registration);

  return (
    <span
      {...rest}
      data-registration={registration}
      className={cn(plateBlock({ size, plateClass }), className)}
    >
      {grouped}
    </span>
  );
}
