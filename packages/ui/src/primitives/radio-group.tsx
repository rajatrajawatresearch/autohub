'use client';

import * as RadioGroupPrimitive from '@radix-ui/react-radio-group';
import type * as React from 'react';

import { cn } from '../lib/cn';

export type RadioGroupProps = React.ComponentPropsWithRef<typeof RadioGroupPrimitive.Root>;
export type RadioGroupItemProps = React.ComponentPropsWithRef<typeof RadioGroupPrimitive.Item>;

export function RadioGroup({ className, ...props }: RadioGroupProps) {
  return (
    <RadioGroupPrimitive.Root
      className={cn('grid gap-[var(--density-gap)]', className)}
      {...props}
    />
  );
}

/**
 * The one round control in the system, deliberately.
 *
 * Everything else is near-square, but round-versus-square is the only signal a user has for
 * "pick one" versus "pick several" — that is grammar, not decoration, and breaking it to stay
 * on-style would cost comprehension for nothing.
 */
export function RadioGroupItem({ className, ...props }: RadioGroupItemProps) {
  return (
    <RadioGroupPrimitive.Item
      className={cn(
        'relative inline-flex size-[1.125rem] shrink-0 items-center justify-center rounded-full',
        'border border-[var(--border-strong)] bg-[var(--surface-card)]',
        'transition-colors duration-100 ease-instrument',
        'outline-none focus-visible:outline-2 focus-visible:outline-offset-2 focus-visible:outline-[var(--focus-ring)]',
        'data-[state=checked]:border-[var(--accent-primary)]',
        'disabled:cursor-not-allowed disabled:opacity-45',
        "after:absolute after:top-1/2 after:left-1/2 after:size-11 after:-translate-x-1/2 after:-translate-y-1/2 after:content-['']",
        className,
      )}
      {...props}
    >
      <RadioGroupPrimitive.Indicator className="block size-2.5 rounded-full bg-[var(--accent-primary)]" />
    </RadioGroupPrimitive.Item>
  );
}
