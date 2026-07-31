import type { Metadata, Viewport } from 'next';
import { QueryProvider } from '@autohub/api';
import { THEME_SCRIPT } from '@autohub/ui';
import { fontVariables } from '@autohub/ui/fonts';
import { ConsoleShell } from '@/components/shell/console-shell';
import './globals.css';

export const metadata: Metadata = {
  title: { default: 'AutoHub Dealer', template: '%s · AutoHub Dealer' },
  description: 'Inventory, leads, auctions and payouts for AutoHub dealers.',
  // A dealer console has nothing to gain from a search engine and something to lose.
  robots: { index: false, follow: false },
};

export const viewport: Viewport = {
  themeColor: [
    { media: '(prefers-color-scheme: light)', color: '#f6f5f1' },
    { media: '(prefers-color-scheme: dark)', color: '#0f1512' },
  ],
};

export default function DealerLayout({ children }: { children: React.ReactNode }) {
  return (
    // data-density="compact" is the whole difference between this app and the customer portal:
    // same components, tighter rows and controls. A dealer works a monitor for six hours and
    // needs stock on screen, not whitespace.
    <html lang="en-IN" className={fontVariables} data-density="compact" suppressHydrationWarning>
      <head>
        {/* Dealers work this console at night. A white flash on every navigation is not acceptable. */}
        <script dangerouslySetInnerHTML={{ __html: THEME_SCRIPT }} />
      </head>
      <body className="min-h-dvh antialiased">
        <QueryProvider>
          <ConsoleShell>{children}</ConsoleShell>
        </QueryProvider>
      </body>
    </html>
  );
}
