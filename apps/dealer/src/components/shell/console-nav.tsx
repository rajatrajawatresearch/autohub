'use client';

import Link from 'next/link';
import { usePathname } from 'next/navigation';
import { useFeatures } from '@autohub/api';
import { cn } from '@autohub/ui';

/**
 * Sections come from the feature registry, so a dealer in a city where Lease has not launched
 * simply does not see it — no empty page, no "coming soon", no code change when it does launch.
 */
const SECTIONS = [
  {
    heading: 'Stock',
    items: [
      { href: '/', label: 'Dashboard', module: null },
      { href: '/inventory', label: 'Inventory', module: null },
      { href: '/requirements', label: 'Requirements', module: null },
    ],
  },
  {
    heading: 'Trade',
    items: [
      { href: '/auction', label: 'Auction floor', module: 'auction' },
      { href: '/leads', label: 'Leads', module: null },
    ],
  },
  {
    heading: 'Money',
    items: [
      { href: '/wallet', label: 'Wallet & payouts', module: null },
      { href: '/settings', label: 'Settings', module: null },
    ],
  },
] as const;

export function ConsoleNav() {
  const pathname = usePathname();
  const { data: features } = useFeatures();

  return (
    <nav className="flex-1 overflow-y-auto py-4" aria-label="Console">
      {SECTIONS.map((section) => {
        const items = section.items.filter(
          (item) => item.module === null || features?.modules[item.module] !== false,
        );
        if (items.length === 0) return null;

        return (
          <div key={section.heading} className="mb-5">
            <p className="ah-label px-4 pb-2">{section.heading}</p>
            <ul>
              {items.map((item) => {
                const active =
                  item.href === '/' ? pathname === '/' : pathname.startsWith(item.href);
                return (
                  <li key={item.href}>
                    <Link
                      href={item.href}
                      aria-current={active ? 'page' : undefined}
                      className={cn(
                        'flex h-8 items-center border-l-2 px-4 text-body-sm transition-colors',
                        active
                          ? 'border-[color:var(--accent-primary)] bg-[color:var(--surface-sunken)] font-medium text-[color:var(--text-primary)]'
                          : 'border-transparent text-[color:var(--text-secondary)] hover:bg-[color:var(--surface-sunken)] hover:text-[color:var(--text-primary)]',
                      )}
                    >
                      {item.label}
                    </Link>
                  </li>
                );
              })}
            </ul>
          </div>
        );
      })}
    </nav>
  );
}
