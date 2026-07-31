'use client';

import * as TooltipPrimitive from '@radix-ui/react-tooltip';
import type * as React from 'react';

import { cn } from '../lib/cn';

export const TooltipProvider = TooltipPrimitive.Provider;
export const TooltipTrigger = TooltipPrimitive.Trigger;

export type TooltipProps = React.ComponentPropsWithRef<typeof TooltipPrimitive.Root> & {
  delayDuration?: number;
};

/**
 * Carries its own provider so a tooltip works wherever it is dropped — a shared component that
 * throws unless an ancestor was remembered is a component that quietly stops being used.
 * Mount `TooltipProvider` at the app root as well to get shared open/close delays across a toolbar.
 */
export function Tooltip({ delayDuration = 200, children, ...props }: TooltipProps) {
  return (
    <TooltipPrimitive.Provider delayDuration={delayDuration}>
      <TooltipPrimitive.Root {...props}>{children}</TooltipPrimitive.Root>
    </TooltipPrimitive.Provider>
  );
}

/**
 * Inverted surface, one line, no arrow. A tooltip explains a control; it never carries the only
 * copy of something the user needs — touch devices do not hover.
 */
export function TooltipContent({
  className,
  sideOffset = 6,
  ...props
}: React.ComponentPropsWithRef<typeof TooltipPrimitive.Content>) {
  return (
    <TooltipPrimitive.Portal>
      <TooltipPrimitive.Content
        sideOffset={sideOffset}
        collisionPadding={12}
        className={cn(
          'z-50 max-w-64 rounded-control bg-[var(--surface-inverse)] px-2 py-1',
          'text-body-sm text-[color:var(--text-inverse)] shadow-overlay',
          className,
        )}
        {...props}
      />
    </TooltipPrimitive.Portal>
  );
}
