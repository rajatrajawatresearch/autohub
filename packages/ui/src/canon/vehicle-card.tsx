import { Camera } from 'lucide-react';
import type { ReactNode } from 'react';
import { cn } from '../lib/cn';
import { MoneyValue } from './money-value';
import { PlateBlock, type PlateClass } from './plate-block';
import { SpecStrip, type SpecItem } from './spec-strip';
import { StatusPill, type StateCategory } from './status-pill';

/**
 * The canonical vehicle object.
 *
 * The same card renders a search result, an auction lot, a dealer's inventory row and an admin
 * moderation queue item — because in the model they are the same thing: one vehicle wearing a
 * different listing intent. Anything intent-specific arrives as a slot (`badges`, `footer`), so
 * adding a rental intent needs no new card.
 *
 * Media always reserves its aspect ratio, so a slow image on a 3G connection never moves the
 * price the user was about to read.
 */

export interface VehicleCardProps {
  title: string;
  registration: { canonical: string; display?: string; plateClass?: PlateClass };
  href?: string;
  /** Integer paise. Omit while a lot has no bids and let `priceSlot` say so. */
  priceMinor?: number | null;
  /** What the money means here — "Asking price", "Current bid", "Monthly rent". Comes from the caller. */
  priceLabel?: string;
  priceCompact?: boolean;
  /** Replaces the money entirely — an on-request line, a countdown, a fee note. */
  priceSlot?: ReactNode;
  specs?: SpecItem[];
  media?: { coverUrl?: string | null; aspectRatio?: number; count?: number; alt?: string };
  /** Registry-labelled badges, rendered by the caller so no code maps a badge code to a word. */
  badges?: ReactNode;
  /** Workflow state, for the surfaces where a vehicle has one — inventory, moderation. */
  status?: { category: StateCategory; label: string };
  location?: string;
  footer?: ReactNode;
  className?: string;
}

export function VehicleCard({
  title,
  registration,
  href,
  priceMinor,
  priceLabel,
  priceCompact = true,
  priceSlot,
  specs,
  media,
  badges,
  status,
  location,
  footer,
  className,
}: VehicleCardProps) {
  const aspectRatio = media?.aspectRatio ?? 1.5;

  return (
    <article
      className={cn(
        'relative flex min-w-0 flex-col overflow-hidden rounded-card border border-[color:var(--border-hairline)]',
        'bg-[var(--surface-card)] transition-shadow duration-150 ease-instrument',
        href && 'hover:border-[color:var(--border-strong)] hover:shadow-raise',
        className,
      )}
    >
      <div
        className="relative w-full overflow-hidden border-b border-[color:var(--border-hairline)] bg-[var(--surface-sunken)]"
        style={{ aspectRatio }}
      >
        {media?.coverUrl ? (
          <img
            src={media.coverUrl}
            alt={media.alt ?? title}
            loading="lazy"
            decoding="async"
            className="h-full w-full object-cover"
          />
        ) : null}

        {typeof media?.count === 'number' && media.count > 0 ? (
          <span className="absolute bottom-2 right-2 inline-flex items-center gap-1 rounded-control bg-tarmac/85 px-1.5 py-[0.125rem] font-mono text-[length:0.6875rem] font-medium text-[color:var(--color-plate)]">
            <Camera aria-hidden="true" className="size-3" />
            {media.count}
          </span>
        ) : null}

        {status ? (
          <StatusPill category={status.category} label={status.label} size="sm" className="absolute left-2 top-2" />
        ) : null}
      </div>

      <div className="flex min-w-0 flex-1 flex-col gap-3 p-4">
        <div className="flex flex-wrap items-start justify-between gap-2">
          <PlateBlock
            registration={registration.canonical}
            display={registration.display}
            plateClass={registration.plateClass ?? 'private'}
            size="xs"
          />
          {location ? (
            <span className="font-mono text-[length:var(--text-body-sm)] text-[color:var(--text-secondary)]">
              {location}
            </span>
          ) : null}
        </div>

        <h3 className="min-w-0 font-display text-[length:var(--text-subheading)] font-semibold leading-[1.3] text-[color:var(--text-primary)]">
          {href ? (
            <a href={href} className="after:absolute after:inset-0 after:content-['']">
              {title}
            </a>
          ) : (
            title
          )}
        </h3>

        <div className="flex min-w-0 flex-col gap-1">
          {priceLabel ? <span className="ah-label">{priceLabel}</span> : null}
          {priceSlot ??
            (typeof priceMinor === 'number' ? (
              <MoneyValue minorUnits={priceMinor} compact={priceCompact} size="lg" />
            ) : null)}
        </div>

        {specs && specs.length > 0 ? (
          <SpecStrip
            items={specs}
            size="sm"
            className="border-t border-[color:var(--border-hairline)] pt-3"
          />
        ) : null}

        {badges ? <div className="flex flex-wrap items-center gap-1.5">{badges}</div> : null}

        {footer ? (
          <div className="relative z-10 mt-auto border-t border-[color:var(--border-hairline)] pt-3">{footer}</div>
        ) : null}
      </div>
    </article>
  );
}
