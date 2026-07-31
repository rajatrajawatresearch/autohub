import type { Metadata, Viewport } from 'next';
import type { ReactNode } from 'react';

import { QueryProvider } from '@autohub/api';
import { THEME_SCRIPT } from '@autohub/ui';
import { fontVariables } from '@autohub/ui/fonts';

import { ConsoleShell } from '@/components/shell/console-shell';

import './globals.css';

/**
 * The console shell.
 *
 * Density is `compact` and never changes: this is a desktop surface where an operator works a queue
 * for six hours, and the extra rows per screen are the whole point. Language is `en-IN` — the
 * localisation registry serves the strings, the document declares the locale.
 */

export const metadata: Metadata = {
  title: {
    default: 'AutoHub Console',
    template: '%s · AutoHub Console',
  },
  description:
    'Operations and registry console for AutoHub — moderation, dealer approval, disputes, and the ten registries that make the platform configurable without a deployment.',
  applicationName: 'AutoHub Console',
  openGraph: {
    type: 'website',
    siteName: 'AutoHub Console',
    title: 'AutoHub Console',
    description:
      'Moderation, dealer approval, disputes, and the registry editors behind AutoHub.',
    locale: 'en_IN',
  },
  /* An internal console has no business in an index. */
  robots: { index: false, follow: false },
};

export const viewport: Viewport = {
  themeColor: [
    { media: '(prefers-color-scheme: light)', color: '#f6f5f1' },
    { media: '(prefers-color-scheme: dark)', color: '#0f1512' },
  ],
};

export default function RootLayout({ children }: { children: ReactNode }) {
  return (
    <html lang="en-IN" data-density="compact" className={fontVariables} suppressHydrationWarning>
      <head>
        {/* Runs before first paint so an operator who chose dark never gets a white flash at 2am.
            Shared with the other two portals so all three read the same storage key. */}
        <script dangerouslySetInnerHTML={{ __html: THEME_SCRIPT }} />
      </head>
      <body>
        <QueryProvider>
          <ConsoleShell>{children}</ConsoleShell>
        </QueryProvider>
      </body>
    </html>
  );
}
