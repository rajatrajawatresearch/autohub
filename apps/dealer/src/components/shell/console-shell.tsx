import Link from 'next/link';
import { ThemeToggle } from '@autohub/ui';
import { ConsoleNav } from './console-nav';
import { BranchPicker } from './branch-picker';

/**
 * A fixed rail and a thin top bar. No collapsing drawer, no command palette, no dashboard chrome.
 *
 * The dealer console is a tool someone opens at 9am and closes at 8pm. Navigation that moves,
 * animates, or hides is navigation they have to re-find every time, so it does none of those.
 */
export function ConsoleShell({ children }: { children: React.ReactNode }) {
  return (
    <div className="flex min-h-dvh">
      <aside className="hidden w-56 shrink-0 border-r border-[color:var(--border-hairline)] lg:flex lg:flex-col">
        <div className="flex h-14 items-center border-b border-[color:var(--border-hairline)] px-4">
          <Link href="/" className="font-display text-body font-bold tracking-[-0.02em]">
            AUTO<span className="text-[color:var(--accent-primary)]">HUB</span>
            <span className="ml-1.5 font-sans text-label uppercase tracking-[0.08em] text-[color:var(--text-secondary)]">
              Dealer
            </span>
          </Link>
        </div>
        <ConsoleNav />
      </aside>

      <div className="flex min-w-0 flex-1 flex-col">
        <header className="flex h-14 shrink-0 items-center gap-3 border-b border-[color:var(--border-hairline)] px-4">
          <Link href="/" className="font-display text-body font-bold tracking-[-0.02em] lg:hidden">
            AUTO<span className="text-[color:var(--accent-primary)]">HUB</span>
          </Link>
          <BranchPicker />
          <div className="ml-auto flex items-center gap-2">
            <ThemeToggle />
            <div className="flex h-8 items-center gap-2 rounded-[var(--radius-control)] border border-[color:var(--border-hairline)] px-2.5">
              <span className="font-mono text-[0.6875rem] text-[color:var(--text-secondary)]">
                Sharma Motors
              </span>
            </div>
          </div>
        </header>

        <main className="min-w-0 flex-1">{children}</main>
      </div>
    </div>
  );
}
