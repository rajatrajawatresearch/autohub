'use client';

import * as DialogPrimitive from '@radix-ui/react-dialog';
import { X } from 'lucide-react';
import type * as React from 'react';

import { cn } from '../lib/cn';

export const Dialog = DialogPrimitive.Root;
export const DialogTrigger = DialogPrimitive.Trigger;

/**
 * A panel, not a modal card that floats in from somewhere.
 *
 * No entrance animation and no backdrop blur: the dim is a flat scrim (glassmorphism is banned)
 * and the panel is simply there. Confirming a bid is not a moment for choreography.
 */
export function DialogContent({
  className,
  children,
  ...props
}: React.ComponentPropsWithRef<typeof DialogPrimitive.Content>) {
  return (
    <DialogPrimitive.Portal>
      <DialogPrimitive.Overlay className="fixed inset-0 z-50 bg-tarmac/60" />
      <DialogPrimitive.Content
        className={cn(
          'fixed top-1/2 left-1/2 z-50 w-[92vw] max-w-lg -translate-x-1/2 -translate-y-1/2',
          'max-h-[88dvh] overflow-y-auto rounded-card border border-[var(--border-hairline)]',
          'bg-[var(--surface-card)] text-[color:var(--text-primary)] shadow-overlay',
          'outline-none',
          className,
        )}
        {...props}
      >
        {children}
        <DialogPrimitive.Close
          aria-label="Close"
          className={cn(
            'absolute top-3 right-3 inline-flex size-8 items-center justify-center rounded-control',
            'text-[color:var(--text-secondary)] transition-colors duration-100 ease-instrument',
            'hover:bg-[var(--surface-sunken)] hover:text-[color:var(--text-primary)]',
            'outline-none focus-visible:outline-2 focus-visible:outline-offset-2 focus-visible:outline-[var(--focus-ring)]',
          )}
        >
          <X aria-hidden="true" className="size-4" />
        </DialogPrimitive.Close>
      </DialogPrimitive.Content>
    </DialogPrimitive.Portal>
  );
}

export function DialogHeader({ className, ...props }: React.ComponentPropsWithRef<'div'>) {
  return (
    <div
      className={cn(
        'flex flex-col gap-1 border-b border-[var(--border-hairline)]',
        'px-[var(--density-pad-x)] py-[calc(var(--density-pad-y)*1.6)] pr-12',
        className,
      )}
      {...props}
    />
  );
}

export function DialogTitle({
  className,
  ...props
}: React.ComponentPropsWithRef<typeof DialogPrimitive.Title>) {
  return (
    <DialogPrimitive.Title
      className={cn('font-display text-subheading text-[color:var(--text-primary)]', className)}
      {...props}
    />
  );
}

export function DialogDescription({
  className,
  ...props
}: React.ComponentPropsWithRef<typeof DialogPrimitive.Description>) {
  return (
    <DialogPrimitive.Description
      className={cn('text-body-sm text-[color:var(--text-secondary)]', className)}
      {...props}
    />
  );
}

/** Actions read right-to-left in importance; the one primary action sits last. */
export function DialogFooter({ className, ...props }: React.ComponentPropsWithRef<'div'>) {
  return (
    <div
      className={cn(
        'flex flex-col-reverse gap-[var(--density-gap)] border-t border-[var(--border-hairline)]',
        'px-[var(--density-pad-x)] py-[calc(var(--density-pad-y)*1.6)]',
        'sm:flex-row sm:justify-end',
        className,
      )}
      {...props}
    />
  );
}
