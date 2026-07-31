'use client';

import { Monitor, Moon, Sun } from 'lucide-react';
import { useEffect, useState } from 'react';
import { cn } from '../lib/cn';

/**
 * Dark is a real mode, not an inversion — dealers work at night and inspectors work in sunlight,
 * so both themes ship at parity and the choice is explicit rather than guessed.
 *
 * Three states, because "system" is a real answer: it writes nothing to `data-theme` and lets
 * `prefers-color-scheme` in tokens.css decide. Light and dark stamp `data-theme` on `<html>`,
 * which is exactly what the token file keys off.
 *
 * The app must also run the pre-hydration script (see `THEME_SCRIPT`) in `<head>`, or the first
 * paint will be the system theme before this component corrects it.
 */

type Theme = 'system' | 'light' | 'dark';

const STORAGE_KEY = 'autohub-theme';

/**
 * Inline this in `<head>` via `<script dangerouslySetInnerHTML={{ __html: THEME_SCRIPT }} />`.
 * It runs before first paint, so a dealer who chose dark never gets a white flash.
 */
export const THEME_SCRIPT = `(function(){try{var t=localStorage.getItem('${STORAGE_KEY}');if(t==='light'||t==='dark'){document.documentElement.setAttribute('data-theme',t);}}catch(e){}})();`;

function applyTheme(theme: Theme): void {
  const root = document.documentElement;
  if (theme === 'system') {
    root.removeAttribute('data-theme');
  } else {
    root.setAttribute('data-theme', theme);
  }
}

export interface ThemeToggleLabels {
  group: string;
  system: string;
  light: string;
  dark: string;
}

const DEFAULT_LABELS: ThemeToggleLabels = {
  group: 'Theme',
  system: 'System',
  light: 'Light',
  dark: 'Dark',
};

export interface ThemeToggleProps {
  /** en-IN defaults; pass resolved strings from the localisation registry in production. */
  labels?: Partial<ThemeToggleLabels>;
  className?: string;
}

const OPTIONS = [
  { value: 'system', Icon: Monitor },
  { value: 'light', Icon: Sun },
  { value: 'dark', Icon: Moon },
] as const satisfies ReadonlyArray<{ value: Theme; Icon: typeof Monitor }>;

export function ThemeToggle({ labels, className }: ThemeToggleProps) {
  const resolved = { ...DEFAULT_LABELS, ...labels };
  const [theme, setTheme] = useState<Theme>('system');
  const [mounted, setMounted] = useState(false);

  useEffect(() => {
    let stored: string | null = null;
    try {
      stored = window.localStorage.getItem(STORAGE_KEY);
    } catch {
      stored = null;
    }
    if (stored === 'light' || stored === 'dark' || stored === 'system') {
      setTheme(stored);
    }
    setMounted(true);
  }, []);

  function select(next: Theme) {
    setTheme(next);
    applyTheme(next);
    try {
      window.localStorage.setItem(STORAGE_KEY, next);
    } catch {
      /* Private browsing refuses the write; the DOM attribute still holds for this session. */
    }
  }

  return (
    <div
      role="group"
      aria-label={resolved.group}
      className={cn(
        'inline-flex items-center rounded-control border border-[color:var(--border-hairline)] bg-[var(--surface-card)] p-[2px]',
        className,
      )}
    >
      {OPTIONS.map(({ value, Icon }) => {
        const active = mounted && theme === value;

        return (
          <button
            key={value}
            type="button"
            onClick={() => select(value)}
            aria-pressed={active}
            title={resolved[value]}
            className={cn(
              'inline-flex size-8 items-center justify-center rounded-control',
              active
                ? 'bg-[var(--accent-primary)] text-[color:var(--text-on-primary)]'
                : 'text-[color:var(--text-secondary)] hover:bg-[var(--surface-sunken)] hover:text-[color:var(--text-primary)]',
            )}
          >
            <Icon aria-hidden="true" className="size-4" />
            <span className="sr-only">{resolved[value]}</span>
          </button>
        );
      })}
    </div>
  );
}
