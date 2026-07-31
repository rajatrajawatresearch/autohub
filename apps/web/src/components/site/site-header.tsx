import Link from 'next/link';
import { ThemeToggle } from '@autohub/ui';
import { CityPicker } from './city-picker';
import { NavLinks } from './nav-links';

/**
 * A hairline rule and a wordmark. No shadow, no blur, no floating pill.
 *
 * The header's job is to say where you are and let you leave; on a product where the hero is the
 * thing that matters, anything more elaborate up here competes with it.
 */
export function SiteHeader() {
  return (
    <header className="sticky top-0 z-40 border-b border-[color:var(--border-hairline)] bg-[color:var(--surface-page)]/95 backdrop-blur-[2px]">
      <div className="mx-auto flex h-16 max-w-[var(--container-content)] items-center gap-6 px-4 sm:px-6">
        <Link
          href="/"
          className="font-display text-[1.0625rem] font-bold tracking-[-0.02em] text-[color:var(--text-primary)]"
        >
          AUTO<span className="text-[color:var(--accent-primary)]">HUB</span>
        </Link>

        <NavLinks />

        <div className="ml-auto flex items-center gap-2">
          <CityPicker />
          <ThemeToggle />
          <Link
            href="/login"
            className="hidden h-9 items-center rounded-[var(--radius-control)] border border-[color:var(--border-strong)] px-3 text-body-sm font-medium text-[color:var(--text-primary)] transition-colors hover:bg-[color:var(--surface-sunken)] sm:inline-flex"
          >
            Sign in
          </Link>
        </div>
      </div>
    </header>
  );
}
