'use client';

import * as CheckboxPrimitive from '@radix-ui/react-checkbox';
import { Check, Minus } from 'lucide-react';
import type * as React from 'react';

import { cn } from '../lib/cn';

export type CheckboxProps = React.ComponentPropsWithRef<typeof CheckboxPrimitive.Root> & {
  invalid?: boolean;
};

/**
 * A square box with a hard border — a form field on a document, ticked in ink.
 *
 * The visible box is 18px because a wall of filter chips should not be a wall of 44px squares,
 * but the hit area is expanded to 44px with a pseudo-element so a thumb still finds it.
 */
export function Checkbox({ className, invalid, ...props }: CheckboxProps) {
  return (
    <CheckboxPrimitive.Root
      aria-invalid={invalid === true ? true : props['aria-invalid']}
      className={cn(
        'relative inline-flex size-[1.125rem] shrink-0 items-center justify-center',
        'rounded-control border border-[var(--border-strong)] bg-[var(--surface-card)]',
        'transition-colors duration-100 ease-instrument',
        'outline-none focus-visible:outline-2 focus-visible:outline-offset-2 focus-visible:outline-[var(--focus-ring)]',
        'aria-[invalid=true]:border-flag',
        'data-[state=checked]:border-transparent data-[state=checked]:bg-[var(--accent-primary)]',
        'data-[state=indeterminate]:border-transparent data-[state=indeterminate]:bg-[var(--accent-primary)]',
        'disabled:cursor-not-allowed disabled:opacity-45',
        // Invisible 44px target. Outdoors, one-handed, in sunlight.
        "after:absolute after:top-1/2 after:left-1/2 after:size-11 after:-translate-x-1/2 after:-translate-y-1/2 after:content-['']",
        className,
      )}
      {...props}
    >
      <CheckboxPrimitive.Indicator className="flex items-center justify-center text-[color:var(--text-on-primary)]">
        {props.checked === 'indeterminate' ? (
          <Minus aria-hidden="true" className="size-3.5" strokeWidth={3} />
        ) : (
          <Check aria-hidden="true" className="size-3.5" strokeWidth={3} />
        )}
      </CheckboxPrimitive.Indicator>
    </CheckboxPrimitive.Root>
  );
}
