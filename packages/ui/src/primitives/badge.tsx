import { cva, type VariantProps } from 'class-variance-authority';
import type * as React from 'react';

import { cn } from '../lib/cn';

/**
 * The `.ah-label` voice, spelled out.
 *
 * The utility itself pins `color: var(--text-secondary)`, which is right for a caption sitting
 * above a value and wrong for a filled stamp — so a badge reproduces the same type from the same
 * tokens and keeps the colour under variant control. Same typography, no cascade fight.
 */
const LABEL_TYPE =
  'text-[length:var(--text-label)] leading-[1.2] font-semibold tracking-[0.08em] uppercase';

/**
 * A stamp on a document. Near-square, uppercase, letter-spaced.
 *
 * Status badges are a solid fill with a fixed text polarity rather than a tinted chip, for two
 * reasons: the fill needs no dark-mode override, and every pair clears AA — a light chip with
 * `--color-caution` text does not (that amber is a fill colour, never a text colour, which is
 * exactly why the signal yellow always takes dark text too).
 *
 * The colour is never chosen in an app. A workflow state arrives carrying a `category`; the canon
 * layer maps category → variant. Nothing outside that mapping decides "rejected" is red.
 */
export const badgeVariants = cva(
  [
    LABEL_TYPE,
    'inline-flex shrink-0 items-center gap-1 rounded-control border whitespace-nowrap',
    '[&_svg]:pointer-events-none [&_svg]:shrink-0 [&_svg]:size-3',
  ],
  {
    variants: {
      variant: {
        neutral:
          'border-[var(--border-hairline)] bg-[var(--surface-sunken)] text-[color:var(--text-primary)]',
        outline: 'border-[var(--border-strong)] bg-transparent text-[color:var(--text-secondary)]',
        primary:
          'border-transparent bg-[var(--accent-primary)] text-[color:var(--text-on-primary)]',
        /** Live, urgent, money-moving. Shares the rationed yellow — count it against the viewport's one. */
        action: 'border-transparent bg-signal text-tarmac',
        /** Expired, rejected, overdue. */
        flag: 'border-transparent bg-flag text-plate',
        /** Pending, expiring soon. Amber takes dark text, always. */
        caution: 'border-transparent bg-caution text-tarmac',
        /** Verified, settled, passed. */
        clear: 'border-transparent bg-clear text-plate',
        /** Informational, under review. */
        info: 'border-transparent bg-info text-plate',
      },
      size: {
        /** Inline in a dense table row. */
        sm: 'h-4 px-1.5',
        md: 'h-[1.375rem] px-2',
      },
    },
    defaultVariants: {
      variant: 'neutral',
      size: 'md',
    },
  },
);

export interface BadgeProps
  extends React.ComponentPropsWithRef<'span'>,
    VariantProps<typeof badgeVariants> {}

export function Badge({ className, variant, size, ...props }: BadgeProps) {
  return <span className={cn(badgeVariants({ variant, size }), className)} {...props} />;
}
