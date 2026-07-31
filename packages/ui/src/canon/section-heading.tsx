import type { ReactNode } from 'react';
import { cn } from '../lib/cn';

/**
 * A section header in the display face, with the same .ah-label eyebrow that sits above every
 * value in the product, and a right-side slot for the one action the section owns.
 *
 * The optional hairline rule is how sections separate — structure comes from rules, not from
 * elevation and not from a coloured band.
 */
export interface SectionHeadingProps {
  title: ReactNode;
  /** Rendered as an .ah-label above the title: "Auction", "Inventory", "Step 2 of 4". */
  eyebrow?: string;
  description?: ReactNode;
  /** One control, right-aligned. Two controls means the section is doing two jobs. */
  action?: ReactNode;
  level?: 1 | 2 | 3 | 4;
  size?: 'sm' | 'md' | 'lg';
  /** Hairline rule under the block. */
  rule?: boolean;
  className?: string;
}

const SIZES = {
  sm: 'text-[length:var(--text-subheading)] leading-[1.3] font-semibold',
  md: 'text-[length:var(--text-heading)] leading-[1.25] tracking-[-0.015em] font-semibold',
  lg: 'text-[length:var(--text-display-lg)] leading-[1.02] tracking-[-0.025em] font-bold',
} as const;

export function SectionHeading({
  title,
  eyebrow,
  description,
  action,
  level = 2,
  size = 'md',
  rule = false,
  className,
}: SectionHeadingProps) {
  const Tag = `h${level}` as 'h1' | 'h2' | 'h3' | 'h4';

  return (
    <div
      className={cn(
        'flex flex-wrap items-end justify-between gap-x-6 gap-y-3',
        rule && 'border-b border-[color:var(--border-hairline)] pb-3',
        className,
      )}
    >
      <div className="flex min-w-0 flex-col gap-1.5">
        {eyebrow ? <span className="ah-label">{eyebrow}</span> : null}
        <Tag className={cn('min-w-0 font-display text-[color:var(--text-primary)]', SIZES[size])}>{title}</Tag>
        {description ? (
          <p className="max-w-[64ch] text-[length:var(--text-body)] leading-[1.55] text-[color:var(--text-secondary)]">
            {description}
          </p>
        ) : null}
      </div>

      {action ? <div className="flex shrink-0 items-center gap-2">{action}</div> : null}
    </div>
  );
}
