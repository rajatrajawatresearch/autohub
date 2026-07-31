'use client';

import * as LabelPrimitive from '@radix-ui/react-label';
import type * as React from 'react';

import { cn } from '../lib/cn';

export interface LabelProps extends React.ComponentPropsWithRef<typeof LabelPrimitive.Root> {
  /** Marks the field as required in the schema. Renders a mark; the input still carries `required`. */
  required?: boolean;
}

/**
 * The RC-card atom, in its form-control role: a small uppercase letter-spaced caption sitting
 * above the thing it names. Every value in AutoHub wears one, and that single repetition does
 * more for the product's coherence than any other decision in the system.
 */
export function Label({ className, required, children, ...props }: LabelProps) {
  return (
    <LabelPrimitive.Root
      className={cn(
        'ah-label block cursor-default',
        'peer-disabled:cursor-not-allowed peer-disabled:opacity-60',
        className,
      )}
      {...props}
    >
      {children}
      {required === true ? (
        <span aria-hidden="true" className="ml-1 text-flag">
          *
        </span>
      ) : null}
    </LabelPrimitive.Root>
  );
}
