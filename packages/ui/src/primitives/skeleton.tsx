import type * as React from 'react';

import { cn } from '../lib/cn';

/**
 * A held slot, not a shimmer.
 *
 * The only animation in AutoHub is the odometer roll on a valuation reveal; a pulsing skeleton
 * would be ambient motion on every screen and would cost that one moment its weight. So loading
 * reads as an unfilled field on a form — which is exactly what it is.
 *
 * Its job is holding layout: give it the final element's dimensions and nothing shifts on arrival.
 */
export function Skeleton({ className, ...props }: React.ComponentPropsWithRef<'div'>) {
  return (
    <div
      aria-hidden="true"
      className={cn(
        'rounded-control border border-[var(--border-hairline)] bg-[var(--surface-sunken)]',
        className,
      )}
      {...props}
    />
  );
}
