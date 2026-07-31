'use client';

import * as SwitchPrimitive from '@radix-ui/react-switch';
import type * as React from 'react';

import { cn } from '../lib/cn';

export type SwitchProps = React.ComponentPropsWithRef<typeof SwitchPrimitive.Root>;

/**
 * A rocker, not a pill. Square track, square thumb, flat fill — the toggle on a dashboard panel.
 * Off reads as an empty slot; on reads as a filled one, so the state survives a glance in sunlight
 * without depending on colour alone.
 */
export function Switch({ className, ...props }: SwitchProps) {
  return (
    <SwitchPrimitive.Root
      className={cn(
        'relative inline-flex h-6 w-11 shrink-0 items-center rounded-control border p-[2px]',
        'border-[var(--border-strong)] bg-[var(--surface-sunken)]',
        'transition-colors duration-100 ease-instrument',
        'outline-none focus-visible:outline-2 focus-visible:outline-offset-2 focus-visible:outline-[var(--focus-ring)]',
        'data-[state=checked]:border-transparent data-[state=checked]:bg-[var(--accent-primary)]',
        'disabled:cursor-not-allowed disabled:opacity-45',
        "after:absolute after:top-1/2 after:left-0 after:h-11 after:w-full after:-translate-y-1/2 after:content-['']",
        className,
      )}
      {...props}
    >
      <SwitchPrimitive.Thumb
        className={cn(
          'pointer-events-none block size-4 rounded-[1px] bg-[var(--border-strong)]',
          'transition-transform duration-100 ease-instrument',
          'data-[state=unchecked]:translate-x-0 data-[state=checked]:translate-x-[1.25rem]',
          'data-[state=checked]:bg-[var(--text-on-primary)]',
        )}
      />
    </SwitchPrimitive.Root>
  );
}
