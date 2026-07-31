import type { ReactNode } from 'react';
import { cn } from '../lib/cn';

/**
 * The RC-card atom: a small uppercase letter-spaced label sitting above its value.
 *
 * Nearly everything in this product is made of these. The repetition is deliberate — it is the
 * single largest coherence lever in the design system, and it is why a valuation panel, a lot
 * header and an admin drawer read as the same document.
 */
export interface FieldBlockProps {
  label: string;
  value: ReactNode;
  /** Identifiers, money, kms, dates, VINs — anything a user reads, compares or transcribes. */
  mono?: boolean;
  align?: 'start' | 'end';
  /** A qualifier under the value: a source, a unit, an "as of" date. A number without one is a rumour. */
  hint?: ReactNode;
  className?: string;
}

/** A value that is genuinely absent reads as an em dash, never as an empty row. */
function isEmpty(value: ReactNode): boolean {
  return value === null || value === undefined || value === '' || value === false;
}

export function FieldBlock({ label, value, mono = false, align = 'start', hint, className }: FieldBlockProps) {
  const empty = isEmpty(value);

  return (
    <div className={cn('flex min-w-0 flex-col gap-1', align === 'end' && 'items-end text-right', className)}>
      <span className="ah-label">{label}</span>
      <span
        className={cn(
          'min-w-0 break-words text-[length:var(--text-body)] leading-[1.45] text-[color:var(--text-primary)]',
          mono && 'font-mono text-[length:var(--text-data)] font-medium leading-[1.4]',
          empty && 'text-[color:var(--text-secondary)]',
        )}
      >
        {empty ? '—' : value}
      </span>
      {hint ? (
        <span className="text-[length:var(--text-body-sm)] leading-[1.4] text-[color:var(--text-secondary)]">
          {hint}
        </span>
      ) : null}
    </div>
  );
}
