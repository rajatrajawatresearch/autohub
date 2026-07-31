'use client';

import * as PopoverPrimitive from '@radix-ui/react-popover';
import type * as React from 'react';

import { cn } from '../lib/cn';

export const Popover = PopoverPrimitive.Root;
export const PopoverTrigger = PopoverPrimitive.Trigger;

/**
 * A panel anchored to its trigger — filter groups, city pickers, a bid breakdown.
 * Hairline border, flat surface, overlay shadow. It appears; it does not arrive.
 */
export function PopoverContent({
  className,
  align = 'start',
  sideOffset = 6,
  ...props
}: React.ComponentPropsWithRef<typeof PopoverPrimitive.Content>) {
  return (
    <PopoverPrimitive.Portal>
      <PopoverPrimitive.Content
        align={align}
        sideOffset={sideOffset}
        collisionPadding={12}
        className={cn(
          'z-50 w-72 max-w-[92vw] rounded-card border border-[var(--border-hairline)]',
          'bg-[var(--surface-card)] text-[color:var(--text-primary)] shadow-overlay',
          'p-[var(--density-pad-x)] outline-none',
          className,
        )}
        {...props}
      />
    </PopoverPrimitive.Portal>
  );
}
