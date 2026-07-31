'use client';

import * as AccordionPrimitive from '@radix-ui/react-accordion';
import { ChevronDown } from 'lucide-react';
import type * as React from 'react';

import { cn } from '../lib/cn';

export const Accordion = AccordionPrimitive.Root;

/**
 * Sections divided by a rule — filter groups in search, spec groups on a vehicle page, an
 * inspection checklist's chapters. The panel opens instantly; only the chevron moves, and that
 * is a state indicator rather than an animation.
 */
export function AccordionItem({
  className,
  ...props
}: React.ComponentPropsWithRef<typeof AccordionPrimitive.Item>) {
  return (
    <AccordionPrimitive.Item
      className={cn('border-b border-[var(--border-hairline)]', className)}
      {...props}
    />
  );
}

export function AccordionTrigger({
  className,
  children,
  ...props
}: React.ComponentPropsWithRef<typeof AccordionPrimitive.Trigger>) {
  return (
    <AccordionPrimitive.Header className="flex">
      <AccordionPrimitive.Trigger
        className={cn(
          'group flex flex-1 items-center justify-between gap-3 text-left',
          'py-[calc(var(--density-pad-y)*1.6)] text-body font-semibold',
          'text-[color:var(--text-primary)]',
          'outline-none focus-visible:outline-2 focus-visible:-outline-offset-2 focus-visible:outline-[var(--focus-ring)]',
          'disabled:pointer-events-none disabled:opacity-45',
          className,
        )}
        {...props}
      >
        {children}
        <ChevronDown
          aria-hidden="true"
          className={cn(
            'size-4 shrink-0 text-[color:var(--text-secondary)]',
            'transition-transform duration-100 ease-instrument',
            'group-data-[state=open]:rotate-180',
          )}
        />
      </AccordionPrimitive.Trigger>
    </AccordionPrimitive.Header>
  );
}

export function AccordionContent({
  className,
  children,
  ...props
}: React.ComponentPropsWithRef<typeof AccordionPrimitive.Content>) {
  return (
    <AccordionPrimitive.Content
      className={cn('overflow-hidden text-body', className)}
      {...props}
    >
      <div className="pb-[calc(var(--density-pad-y)*1.6)]">{children}</div>
    </AccordionPrimitive.Content>
  );
}
