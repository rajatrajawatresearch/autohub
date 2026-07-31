import type { ReactNode } from 'react';
import Link from 'next/link';
import { ThemeToggle } from '@autohub/ui';
import { ConsoleNav, ConsoleNavStrip } from './console-nav';

/**
 * A fixed rail, a thin bar, and the content. Nothing animates and nothing collapses.
 *
 * This console is where dangerous things happen — publishing a commission rule, approving a dealer,
 * changing a workflow every in-flight listing runs on. Chrome that moves is chrome an operator has
 * to re-find, and re-finding is where mis-clicks live.
 */
export function ConsoleShell({ children }: { children: ReactNode }) {
  return (
    <div className="flex min-h-dvh flex-col lg:flex-row">
      <aside className="hidden w-60 shrink-0 flex-col border-r border-[color:var(--border-hairline)] lg:flex">
        <div className="flex h-14 items-center border-b border-[color:var(--border-hairline)] px-3">
          <Link href="/" className="font-display text-body font-bold tracking-[-0.02em]">
            AUTO<span className="text-[color:var(--accent-primary)]">HUB</span>
            <span className="ml-1.5 font-sans text-label tracking-[0.08em] text-[color:var(--text-secondary)] uppercase">
              Console
            </span>
          </Link>
        </div>
        <div className="flex-1 overflow-y-auto py-4">
          <ConsoleNav />
        </div>
        <div className="border-t border-[color:var(--border-hairline)] px-3 py-2">
          <p className="font-mono text-[0.6875rem] text-[color:var(--text-secondary)]">
            Every publish is audited
          </p>
        </div>
      </aside>

      <div className="flex min-w-0 flex-1 flex-col">
        <header className="flex h-14 shrink-0 items-center gap-3 border-b border-[color:var(--border-hairline)] px-4">
          <Link href="/" className="font-display text-body font-bold tracking-[-0.02em] lg:hidden">
            AUTO<span className="text-[color:var(--accent-primary)]">HUB</span>
          </Link>
          <div className="ml-auto flex items-center gap-2">
            <ThemeToggle />
            <span className="flex h-8 items-center rounded-[var(--radius-control)] border border-[color:var(--border-hairline)] px-2.5 font-mono text-[0.6875rem] text-[color:var(--text-secondary)]">
              Super Admin
            </span>
          </div>
        </header>

        <ConsoleNavStrip className="lg:hidden" />

        <main className="min-w-0 flex-1">{children}</main>
      </div>
    </div>
  );
}
