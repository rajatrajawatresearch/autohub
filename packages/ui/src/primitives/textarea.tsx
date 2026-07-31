import type * as React from 'react';

import { cn } from '../lib/cn';

export interface TextareaProps extends React.ComponentPropsWithRef<'textarea'> {
  /** Draws the field in the flag colour and sets `aria-invalid`. Pair it with a message. */
  invalid?: boolean;
  /** For pasted data blocks — VIN lists, bulk registration numbers, ledger references. */
  mono?: boolean;
}

export function Textarea({ className, invalid, mono, rows = 4, ...props }: TextareaProps) {
  return (
    <textarea
      rows={rows}
      aria-invalid={invalid === true ? true : props['aria-invalid']}
      className={cn(
        'w-full rounded-control border border-[var(--border-strong)] bg-[var(--surface-card)]',
        'px-[var(--density-pad-x)] py-[var(--density-pad-y)]',
        'text-[color:var(--text-primary)] placeholder:text-[color:var(--text-secondary)]',
        'transition-colors duration-100 ease-instrument',
        'outline-none focus-visible:outline-2 focus-visible:outline-offset-2 focus-visible:outline-[var(--focus-ring)]',
        'aria-[invalid=true]:border-flag',
        'disabled:cursor-not-allowed disabled:bg-[var(--surface-sunken)] disabled:opacity-60',
        'read-only:bg-[var(--surface-sunken)]',
        // Vertical only: horizontal resize breaks a form column on a 360px screen.
        'resize-y',
        mono === true ? 'font-mono text-data' : 'font-sans text-body',
        className,
      )}
      {...props}
    />
  );
}
