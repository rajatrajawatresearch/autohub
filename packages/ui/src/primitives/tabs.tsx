'use client';

import * as TabsPrimitive from '@radix-ui/react-tabs';
import type * as React from 'react';

import { cn } from '../lib/cn';

export const Tabs = TabsPrimitive.Root;

/**
 * `.ah-label` typography, spelled out from the same tokens — the utility pins a secondary colour
 * and the active tab has to be able to darken.
 */
const LABEL_TYPE =
  'text-[length:var(--text-label)] leading-[1.2] font-semibold tracking-[0.08em] uppercase';

/**
 * Tabs are a rule with a marker on it — the index tabs on a file, not pills in a tray.
 * The active tab is marked by a 2px bar in the primary green *and* by weight, so the state does
 * not rest on colour alone.
 */
export function TabsList({
  className,
  ...props
}: React.ComponentPropsWithRef<typeof TabsPrimitive.List>) {
  return (
    <TabsPrimitive.List
      className={cn(
        'flex w-full items-stretch gap-1 overflow-x-auto border-b border-[var(--border-hairline)]',
        className,
      )}
      {...props}
    />
  );
}

export function TabsTrigger({
  className,
  ...props
}: React.ComponentPropsWithRef<typeof TabsPrimitive.Trigger>) {
  return (
    <TabsPrimitive.Trigger
      className={cn(
        LABEL_TYPE,
        '-mb-px inline-flex shrink-0 items-center gap-2 border-b-2 border-transparent',
        'px-[calc(var(--density-pad-x)*0.75)] py-3 whitespace-nowrap',
        'text-[color:var(--text-secondary)] transition-colors duration-100 ease-instrument',
        'outline-none focus-visible:outline-2 focus-visible:-outline-offset-2 focus-visible:outline-[var(--focus-ring)]',
        'hover:text-[color:var(--text-primary)]',
        'data-[state=active]:border-[var(--accent-primary)] data-[state=active]:text-[color:var(--text-primary)]',
        'disabled:pointer-events-none disabled:opacity-45',
        className,
      )}
      {...props}
    />
  );
}

export function TabsContent({
  className,
  ...props
}: React.ComponentPropsWithRef<typeof TabsPrimitive.Content>) {
  return (
    <TabsPrimitive.Content
      className={cn(
        'pt-[var(--density-gap)] outline-none',
        'focus-visible:outline-2 focus-visible:outline-offset-2 focus-visible:outline-[var(--focus-ring)]',
        className,
      )}
      {...props}
    />
  );
}
