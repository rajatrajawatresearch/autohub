import type { ReactNode } from 'react';
import { cn } from '../lib/cn';

/**
 * An empty state is an instruction, never a mood.
 *
 * "No bids yet. Bidding opens when the reserve is set." tells a dealer what is true and what
 * happens next. An illustration of a sad car tells them nothing, so there is no illustration
 * slot by default — `icon` exists for a single hairline glyph where a surface genuinely needs
 * one, and the words do the work.
 */
export interface EmptyStateProps {
  title: string;
  /** The instruction: what is true now, and what makes it stop being true. */
  body?: string;
  action?: ReactNode;
  icon?: ReactNode;
  align?: 'start' | 'center';
  className?: string;
}

export function EmptyState({ title, body, action, icon, align = 'center', className }: EmptyStateProps) {
  return (
    <div
      className={cn(
        'flex flex-col gap-2 rounded-card border border-[color:var(--border-hairline)] bg-[var(--surface-sunken)] px-6 py-10',
        align === 'center' ? 'items-center text-center' : 'items-start text-left',
        className,
      )}
    >
      {icon ? <span className="mb-1 text-[color:var(--text-secondary)]">{icon}</span> : null}

      <h3 className="font-display text-[length:var(--text-subheading)] font-semibold leading-[1.3] text-[color:var(--text-primary)]">
        {title}
      </h3>

      {body ? (
        <p className="max-w-[42ch] text-[length:var(--text-body)] leading-[1.55] text-[color:var(--text-secondary)]">
          {body}
        </p>
      ) : null}

      {action ? <div className="mt-3">{action}</div> : null}
    </div>
  );
}
