import type * as React from 'react';

import { cn } from '../lib/cn';

/**
 * The document card, taken from the RC smart card: a hairline-bordered rectangle with a titled
 * head, a field body, and an action foot. No shadow at rest — a card sits *on* the page, it does
 * not float above it. `--shadow-raise` exists for hover on an interactive card and nowhere else.
 */
export function Card({
  className,
  interactive = false,
  ...props
}: React.ComponentPropsWithRef<'div'> & { interactive?: boolean }) {
  return (
    <div
      className={cn(
        'rounded-card border border-[var(--border-hairline)] bg-[var(--surface-card)]',
        'text-[color:var(--text-primary)]',
        interactive
          ? 'transition-shadow duration-100 ease-instrument hover:shadow-raise focus-within:shadow-raise'
          : '',
        className,
      )}
      {...props}
    />
  );
}

/** Titled head, closed off by a hairline — the header band on a printed document. */
export function CardHeader({ className, ...props }: React.ComponentPropsWithRef<'div'>) {
  return (
    <div
      className={cn(
        'flex flex-col gap-1 border-b border-[var(--border-hairline)]',
        'px-[var(--density-pad-x)] py-[calc(var(--density-pad-y)*1.6)]',
        className,
      )}
      {...props}
    />
  );
}

export function CardTitle({ className, ...props }: React.ComponentPropsWithRef<'h3'>) {
  return (
    <h3
      className={cn('font-display text-subheading text-[color:var(--text-primary)]', className)}
      {...props}
    />
  );
}

export function CardDescription({ className, ...props }: React.ComponentPropsWithRef<'p'>) {
  return (
    <p className={cn('text-body-sm text-[color:var(--text-secondary)]', className)} {...props} />
  );
}

export function CardContent({ className, ...props }: React.ComponentPropsWithRef<'div'>) {
  return (
    <div
      className={cn(
        'px-[var(--density-pad-x)] py-[calc(var(--density-pad-y)*1.6)]',
        className,
      )}
      {...props}
    />
  );
}

export function CardFooter({ className, ...props }: React.ComponentPropsWithRef<'div'>) {
  return (
    <div
      className={cn(
        'flex flex-wrap items-center gap-[var(--density-gap)] border-t border-[var(--border-hairline)]',
        'px-[var(--density-pad-x)] py-[calc(var(--density-pad-y)*1.6)]',
        className,
      )}
      {...props}
    />
  );
}
