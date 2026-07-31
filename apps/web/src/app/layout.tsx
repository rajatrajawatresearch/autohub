import type { Metadata, Viewport } from 'next';
import { QueryProvider } from '@autohub/api';
import { THEME_SCRIPT } from '@autohub/ui';
import { fontVariables } from '@autohub/ui/fonts';
import { SiteHeader } from '@/components/site/site-header';
import { SiteFooter } from '@/components/site/site-footer';
import './globals.css';

export const metadata: Metadata = {
  title: {
    default: 'AutoHub — buy, sell, rent and auction vehicles in India',
    template: '%s · AutoHub',
  },
  description:
    'Check what your car is worth, buy an inspected vehicle, or sell it to a verified dealer. ' +
    'RC-verified listings, 200-point inspections, escrow-protected payments.',
  metadataBase: new URL('https://autohub.in'),
  openGraph: {
    type: 'website',
    locale: 'en_IN',
    siteName: 'AutoHub',
  },
  robots: { index: true, follow: true },
};

export const viewport: Viewport = {
  // The plate-white page colour, so the browser chrome does not flash a different background
  // before first paint.
  themeColor: [
    { media: '(prefers-color-scheme: light)', color: '#f6f5f1' },
    { media: '(prefers-color-scheme: dark)', color: '#0f1512' },
  ],
};

export default function RootLayout({ children }: { children: React.ReactNode }) {
  return (
    // lang is en-IN, not en: it changes number and date formatting, and this product is full of both.
    <html lang="en-IN" className={fontVariables} suppressHydrationWarning>
      <head>
        {/* Before first paint, so a returning visitor on dark never sees a white flash. */}
        <script dangerouslySetInnerHTML={{ __html: THEME_SCRIPT }} />
      </head>
      <body className="min-h-dvh antialiased">
        <QueryProvider>
          <a
            href="#main"
            className="sr-only focus:not-sr-only focus:absolute focus:left-4 focus:top-4 focus:z-50 focus:rounded-[var(--radius-control)] focus:bg-[color:var(--accent-primary)] focus:px-4 focus:py-2 focus:text-[color:var(--text-on-primary)]"
          >
            Skip to content
          </a>
          <div className="flex min-h-dvh flex-col">
            <SiteHeader />
            <main id="main" className="flex-1">
              {children}
            </main>
            <SiteFooter />
          </div>
        </QueryProvider>
      </body>
    </html>
  );
}
