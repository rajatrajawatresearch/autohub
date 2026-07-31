import type { ReactNode } from 'react';
import { cn } from '../lib/cn';

/**
 * A hairline-divided row of mono data — Year · Kms · Fuel · Owners.
 *
 * The order and the content come from the registry (`specStrip` on a search result), never from
 * this component: a category that exposes `payload_kg` and `axle_config` renders through exactly
 * the same strip as a hatchback with no code change. Used in cards, in lot headers and in PDFs.
 */

export interface SpecItem {
  label: string;
  value: ReactNode;
  /** Full-precision value for the title attribute when the visible one is abbreviated. */
  title?: string;
}

export interface SpecStripProps {
  items: SpecItem[];
  size?: 'sm' | 'md';
  /**
   * Labels above values is the default and the house style. Drop them only where the row is
   * already captioned — a dense table cell, a PDF footer.
   */
  labels?: boolean;
  className?: string;
}

export function SpecStrip({ items, size = 'md', labels = true, className }: SpecStripProps) {
  if (items.length === 0) return null;

  return (
    <dl className={cn('flex flex-wrap items-stretch gap-y-2', className)}>
      {items.map((item, index) => (
        <div
          key={`${item.label}-${index}`}
          title={item.title}
          className={cn(
            'flex min-w-0 flex-col justify-center border-l border-[color:var(--border-hairline)] first:border-l-0',
            size === 'sm' ? 'gap-0 px-2 first:pl-0' : 'gap-1 px-3 first:pl-0',
          )}
        >
          {labels ? <dt className="ah-label">{item.label}</dt> : <dt className="sr-only">{item.label}</dt>}
          <dd
            className={cn(
              'truncate font-mono font-medium text-[color:var(--text-primary)]',
              size === 'sm'
                ? 'text-[length:var(--text-body-sm)] leading-[1.35]'
                : 'text-[length:var(--text-data)] leading-[1.4]',
            )}
          >
            {item.value}
          </dd>
        </div>
      ))}
    </dl>
  );
}
