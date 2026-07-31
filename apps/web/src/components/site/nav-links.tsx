'use client';

import Link from 'next/link';
import { usePathname } from 'next/navigation';
import { useFeatures } from '@autohub/api';
import { cn } from '@autohub/ui';

/**
 * The nav is not a constant.
 *
 * Which intents exist, and where, comes from the feature registry — so launching Lease in
 * Bengaluru only is a toggle in the admin console, not a deploy. The labels below are the
 * fallback for the moment before the registry answers; the registry decides what is shown.
 */
const NAV = [
  { href: '/buy', label: 'Buy', module: 'buy' },
  { href: '/sell', label: 'Sell', module: 'sell' },
  { href: '/rent', label: 'Rent', module: 'rental' },
  { href: '/lease', label: 'Lease', module: 'lease' },
  { href: '/attach', label: 'Attach', module: 'attachment' },
] as const;

export function NavLinks() {
  const pathname = usePathname();
  const { data: features } = useFeatures();

  // Before the registry answers, show the intents that are always on rather than an empty bar.
  const visible = NAV.filter((item) =>
    features ? features.modules[item.module] === true : item.module === 'buy' || item.module === 'sell',
  );

  return (
    <nav className="hidden items-center gap-1 md:flex" aria-label="Main">
      {visible.map((item) => {
        const active = pathname === item.href || pathname.startsWith(`${item.href}/`);
        return (
          <Link
            key={item.href}
            href={item.href}
            aria-current={active ? 'page' : undefined}
            className={cn(
              'relative px-3 py-2 text-body-sm font-medium transition-colors',
              active
                ? 'text-[color:var(--text-primary)]'
                : 'text-[color:var(--text-secondary)] hover:text-[color:var(--text-primary)]',
            )}
          >
            {item.label}
            {active ? (
              // A 2px rule under the active item — the same device the whole design uses for
              // structure. No pill, no background fill.
              <span className="absolute inset-x-2 -bottom-px h-0.5 bg-[color:var(--accent-primary)]" />
            ) : null}
          </Link>
        );
      })}
    </nav>
  );
}
