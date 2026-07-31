import { cn } from '../lib/cn';

/**
 * Status, driven entirely by the workflow registry.
 *
 * The server sends a state's `name` and its `category` — never a colour, and never a string this
 * component is expected to recognise. Inserting a "Legal Hold" state into the deal workflow
 * therefore renders correctly on the day it is configured, with no deploy and no mapping table
 * to update here.
 *
 * The chip keeps its printed-label colouring in both themes, like a sticker on a file: the tones
 * are chosen for AA contrast against their own fill, not against the page.
 */

/** The categories the workflow registry emits. Everything else is a state code, and states are data. */
export type StateCategory =
  | 'draft'
  | 'awaiting_action'
  | 'in_progress'
  | 'succeeded'
  | 'failed'
  | 'cancelled';

export interface StatusPillProps {
  category: StateCategory;
  /** The state's `name` from the registry, already localised. */
  label: string;
  size?: 'sm' | 'md';
  className?: string;
  title?: string;
}

const TONES: Record<StateCategory, string> = {
  draft: 'border-[color:var(--color-n-300)] bg-[var(--color-n-100)] text-[color:var(--color-n-600)]',
  awaiting_action:
    'border-[color:var(--color-caution)] bg-[var(--color-caution-soft)] text-[color:var(--color-caution)]',
  in_progress: 'border-[color:var(--color-info)] bg-[var(--color-info-soft)] text-[color:var(--color-info)]',
  succeeded: 'border-[color:var(--color-clear)] bg-[var(--color-clear-soft)] text-[color:var(--color-clear)]',
  failed: 'border-[color:var(--color-flag)] bg-[var(--color-flag-soft)] text-[color:var(--color-flag)]',
  cancelled: 'border-[color:var(--color-n-400)] bg-[var(--color-n-100)] text-[color:var(--color-n-600)]',
};

const SIZES = {
  sm: 'px-1.5 py-[0.125rem] text-[length:0.625rem]',
  md: 'px-2 py-[0.1875rem] text-[length:var(--text-label)]',
} as const;

export function StatusPill({ category, label, size = 'md', className, title }: StatusPillProps) {
  return (
    <span
      title={title}
      data-category={category}
      className={cn(
        'inline-flex max-w-full items-center gap-1 truncate rounded-control border font-semibold uppercase leading-[1.2] tracking-[0.08em]',
        TONES[category],
        SIZES[size],
        className,
      )}
    >
      {label}
    </span>
  );
}
