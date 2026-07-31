'use client';

import Link from 'next/link';
import { usePathname } from 'next/navigation';

import { cn } from '@autohub/ui';

import { CONSOLE_NAV, CONSOLE_NAV_ITEMS } from './nav-model';

/**
 * The rail.
 *
 * Active state is a 2px green rule on the left edge plus a weight change — never colour alone. The
 * only interactivity in the shell is knowing which route you are on, which is the only reason this
 * file is a client component.
 */
export function ConsoleNav({ className }: { className?: string }) {
  const pathname = usePathname();

  return (
    <nav aria-label="Console" className={cn('flex flex-col gap-6', className)}>
      {CONSOLE_NAV.map((group) => (
        <div key={group.code} className="flex flex-col gap-1">
          <span className="ah-label px-3 pb-1">{group.label}</span>

          <ul className="flex flex-col">
            {group.items.map((item) => {
              const active =
                item.href === '/' ? pathname === '/' : pathname.startsWith(item.href);

              return (
                <li key={item.href}>
                  <Link
                    href={item.href}
                    aria-current={active ? 'page' : undefined}
                    className={cn(
                      'flex h-[var(--density-control-h)] items-center border-l-2 px-3',
                      'text-[length:var(--text-body-sm)] leading-none',
                      'outline-none focus-visible:outline-2 focus-visible:-outline-offset-2 focus-visible:outline-[var(--focus-ring)]',
                      active
                        ? 'border-[var(--accent-primary)] bg-[var(--surface-sunken)] font-semibold text-[color:var(--text-primary)]'
                        : 'border-transparent text-[color:var(--text-secondary)] hover:bg-[var(--surface-sunken)] hover:text-[color:var(--text-primary)]',
                    )}
                  >
                    {item.label}
                  </Link>
                </li>
              );
            })}
          </ul>
        </div>
      ))}
    </nav>
  );
}

/** The same map as a horizontal strip, for widths where a 15rem rail is most of the screen. */
export function ConsoleNavStrip({ className }: { className?: string }) {
  const pathname = usePathname();
  const items = CONSOLE_NAV_ITEMS;

  return (
    <nav
      aria-label="Console"
      className={cn(
        'flex w-full gap-1 overflow-x-auto border-b border-[color:var(--border-hairline)] bg-[var(--surface-card)] px-2',
        className,
      )}
    >
      {items.map((item) => {
        const active = item.href === '/' ? pathname === '/' : pathname.startsWith(item.href);

        return (
          <Link
            key={item.href}
            href={item.href}
            aria-current={active ? 'page' : undefined}
            className={cn(
              '-mb-px inline-flex shrink-0 items-center border-b-2 px-3 py-2.5 whitespace-nowrap',
              'text-[length:var(--text-label)] leading-[1.2] font-semibold tracking-[0.08em] uppercase',
              'outline-none focus-visible:outline-2 focus-visible:-outline-offset-2 focus-visible:outline-[var(--focus-ring)]',
              active
                ? 'border-[var(--accent-primary)] text-[color:var(--text-primary)]'
                : 'border-transparent text-[color:var(--text-secondary)] hover:text-[color:var(--text-primary)]',
            )}
          >
            {item.label}
          </Link>
        );
      })}
    </nav>
  );
}
