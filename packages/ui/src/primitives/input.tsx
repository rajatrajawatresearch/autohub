import type * as React from 'react';

import { cn } from '../lib/cn';

export interface InputProps extends React.ComponentPropsWithRef<'input'> {
  /** Draws the field in the flag colour and sets `aria-invalid`. Pair it with a message. */
  invalid?: boolean;
  /**
   * Data, not prose: registration, VIN, chassis, engine number, amounts, ids.
   * Mono + tabular figures + slashed zero, because someone is transcribing this off a stamping.
   */
  mono?: boolean;
  /**
   * The unit the field is measured in — `km`, `%`, `bhp`. Comes from the taxonomy attribute or
   * the form schema, never from the component. Rendered inside the field, never as a placeholder.
   */
  unit?: string;
  inputSize?: 'sm' | 'md' | 'lg';
}

const sizeClasses: Record<NonNullable<InputProps['inputSize']>, string> = {
  sm: 'h-[calc(var(--density-control-h)*0.8)] px-[calc(var(--density-pad-x)*0.75)] text-body-sm',
  md: 'h-[var(--density-control-h)] px-[var(--density-pad-x)] text-body',
  lg: 'h-[calc(var(--density-control-h)*1.15)] px-[var(--density-pad-x)] text-body',
};

export function Input({
  className,
  invalid,
  mono,
  unit,
  inputSize = 'md',
  ...props
}: InputProps) {
  const field = (
    <input
      aria-invalid={invalid === true ? true : props['aria-invalid']}
      className={cn(
        'w-full rounded-control border border-[var(--border-strong)] bg-[var(--surface-card)]',
        'text-[color:var(--text-primary)] placeholder:text-[color:var(--text-secondary)]',
        'transition-colors duration-100 ease-instrument',
        'outline-none focus-visible:outline-2 focus-visible:outline-offset-2 focus-visible:outline-[var(--focus-ring)]',
        'aria-[invalid=true]:border-flag',
        'disabled:cursor-not-allowed disabled:bg-[var(--surface-sunken)] disabled:opacity-60',
        'read-only:bg-[var(--surface-sunken)]',
        'file:mr-3 file:border-0 file:bg-transparent file:text-body-sm file:font-semibold',
        sizeClasses[inputSize],
        mono === true ? 'font-mono text-data' : 'font-sans',
        unit !== undefined ? 'pr-12' : '',
        className,
      )}
      {...props}
    />
  );

  if (unit === undefined) return field;

  return (
    <div className="relative w-full">
      {field}
      <span
        aria-hidden="true"
        className="ah-label pointer-events-none absolute top-1/2 right-3 -translate-y-1/2"
      >
        {unit}
      </span>
    </div>
  );
}
