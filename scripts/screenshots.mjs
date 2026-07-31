/**
 * Captures every screen of every AutoHub surface into docs/screenshots/.
 *
 * Two viewports per page, because the product is judged on both: a dealer works a monitor and a
 * seller photographs their car one-handed. A shot that only proves the desktop layout hides exactly
 * the failure the design doc cares about — the sticky action bar that overflows at 360dp.
 *
 * Usage:
 *   node scripts/screenshots.mjs            all surfaces that are reachable
 *   node scripts/screenshots.mjs web        only the Next.js portals
 *   node scripts/screenshots.mjs mobile     only the Flutter web builds
 */

import { chromium } from 'playwright';
import { mkdir, rm } from 'node:fs/promises';
import { join } from 'node:path';

const OUT = new URL('../docs/screenshots/', import.meta.url).pathname;

/** Wide enough for the dealer console's 88rem container without a horizontal scrollbar. */
const DESKTOP = { width: 1600, height: 1000 };

/** A common Indian handset. Narrow enough to catch an overflow, tall enough to show a real page. */
const PHONE = { width: 390, height: 844, deviceScaleFactor: 2, isMobile: true, hasTouch: true };

const WEB = [
  {
    surface: 'web',
    label: 'Customer portal',
    base: 'http://localhost:3100',
    pages: [
      ['home', '/', 'The plate-input hero'],
      ['buy', '/buy', 'Search with registry-driven facets'],
      ['sell', '/sell', 'The sell flow, rendered from the form registry'],
      ['attach', '/attach', 'Taxi Attachment Centre'],
      ['rent', '/rent', 'Rental — the same shell, a different intent'],
      ['lease', '/lease', 'Lease'],
      ['login', '/login', 'OTP, no password'],
    ],
  },
  {
    surface: 'web',
    label: 'Dealer portal',
    base: 'http://localhost:3101',
    pages: [
      ['dashboard', '/', 'Stock, ageing, payouts'],
      ['inventory', '/inventory', 'The table a dealer lives in'],
      ['auction', '/auction', 'The floor — the one place yellow is loud'],
      ['leads', '/leads', 'Pipeline stages from the workflow registry'],
      ['requirements', '/requirements', 'The requirement board'],
      ['wallet', '/wallet', 'Ledger-backed balances'],
      ['settings', '/settings', 'Branches, KYC, entitlements'],
    ],
  },
  {
    surface: 'web',
    label: 'Admin console',
    base: 'http://localhost:3102',
    pages: [
      ['dashboard', '/', 'Queues first, configuration second'],
      ['moderation', '/moderation', 'Where a listing becomes public'],
      ['dealers', '/dealers', 'Approval and KYC'],
      ['disputes', '/disputes', 'Open cases hold escrow'],
      ['registry-taxonomy', '/registry/taxonomy', 'The attribute editor'],
      ['registry-forms', '/registry/forms', 'Form builder with a live preview'],
      ['registry-workflows', '/registry/workflows', 'States, transitions, publish checks'],
      ['registry-pricing', '/registry/pricing', 'Commission rules and the live tester'],
      ['registry-features', '/registry/features', 'Modules and flags'],
      ['registry-i18n', '/registry/i18n', 'Translation gaps'],
      ['registry-geo', '/registry/geo', 'Serviceable cities'],
      ['registry-notifications', '/registry/notifications', 'Event → audience → channel'],
      ['system-roles', '/system/roles', 'Permissions, and their scope'],
      ['system-audit', '/system/audit', 'Append-only'],
      ['system-integrations', '/system/integrations', 'Provider fallback chains'],
    ],
  },
];

/**
 * Mobile is captured by a golden test, not by this script.
 *
 * The obvious route — build the Flutter apps for web and screenshot them in the same browser —
 * does not exist: `flutter build web` fails with `Dart library 'dart:ffi' is not available on this
 * platform`, because the registry cache and the outbox run on drift's native SQLite. Making it
 * compile would mean a second, WASM storage path that ships to nobody, purely so a screenshot
 * script has something to point at.
 *
 * `mobile/packages/autohub_ui/test/design_sheet_golden_test.dart` renders the real widgets with
 * the real fonts at handset width, and `tool/capture_mobile.sh` copies the result here. It is
 * deterministic, it is diffed on every test run, and it caught two defects a browser shot would
 * not have: the app bundled no font assets at all, and JetBrains Mono has no rupee glyph, so every
 * price rendered as a tofu box.
 */
const MOBILE = [];

async function reachable(url) {
  try {
    const response = await fetch(url, { signal: AbortSignal.timeout(4000) });
    return response.status < 500;
  } catch {
    return false;
  }
}

/**
 * Waits for the page to stop moving.
 *
 * `networkidle` alone is not enough here: the portals hydrate a client island after their server
 * HTML lands, and a shot taken between the two captures a skeleton. Waiting for fonts settles the
 * remaining shift — Archivo arriving late reflows every heading in the frame.
 */
async function settle(page, { flutter = false } = {}) {
  await page.waitForLoadState('networkidle').catch(() => {});
  await page.evaluate(() => document.fonts?.ready).catch(() => {});

  if (flutter) {
    // A Flutter web build paints nothing until the engine has downloaded CanvasKit, bootstrapped
    // and run its first frame — none of which registers as network activity by the time
    // `networkidle` fires. Waiting for the glass pane and then for the frame after it is the
    // difference between a screenshot of the app and a screenshot of a white page.
    await page
      .waitForSelector('flt-glass-pane, flutter-view, flt-scene-host', { timeout: 45000 })
      .catch(() => {});
    await page.waitForTimeout(4000);
    return;
  }

  await page.waitForTimeout(900);
}

async function capture(browser, group, viewport, suffix) {
  const context = await browser.newContext({
    viewport: { width: viewport.width, height: viewport.height },
    deviceScaleFactor: viewport.deviceScaleFactor ?? 1,
    isMobile: viewport.isMobile ?? false,
    hasTouch: viewport.hasTouch ?? false,
    colorScheme: 'light',
    locale: 'en-IN',
    timezoneId: 'Asia/Kolkata',
  });

  const page = await context.newPage();
  const taken = [];

  for (const [slug, path, caption] of group.pages) {
    const url = group.base + path;
    try {
      await page.goto(url, { waitUntil: 'domcontentloaded', timeout: 60000 });
      await settle(page, { flutter: group.surface === 'mobile' });

      const dir = join(OUT, group.surface, slugify(group.label));
      await mkdir(dir, { recursive: true });

      const file = join(dir, `${slug}${suffix}.png`);
      await page.screenshot({ path: file, fullPage: suffix === '' });
      taken.push({ slug, caption, file: file.replace(OUT, ''), path });
      process.stdout.write(`  ✓ ${group.label} ${path}${suffix}\n`);
    } catch (error) {
      process.stdout.write(`  ✗ ${group.label} ${path}${suffix} — ${error.message.split('\n')[0]}\n`);
    }
  }

  await context.close();
  return taken;
}

function slugify(value) {
  return value.toLowerCase().replace(/[^a-z0-9]+/g, '-').replace(/(^-|-$)/g, '');
}

const only = process.argv[2];
const groups = [...(only === 'mobile' ? [] : WEB), ...(only === 'web' ? [] : MOBILE)];

// Clear only the surfaces this run is capturing. Wiping the whole directory meant a `mobile`
// run silently deleted every web screenshot, which is a fine way to push an empty gallery.
for (const surface of new Set(groups.map((group) => group.surface))) {
  await rm(join(OUT, surface), { recursive: true, force: true });
}
await mkdir(OUT, { recursive: true });

const browser = await chromium.launch();
const manifest = [];

for (const group of groups) {
  if (!(await reachable(group.base))) {
    process.stdout.write(`  — ${group.label} not running at ${group.base}, skipped\n`);
    continue;
  }

  // Desktop full-page for the portals; the phone shot is the viewport, because a 12,000px tall
  // full-page mobile capture is not something anyone looks at.
  const shots =
    group.surface === 'mobile'
      ? await capture(browser, group, PHONE, '')
      : [
          ...(await capture(browser, group, DESKTOP, '')),
          ...(await capture(browser, group, PHONE, '-mobile')),
        ];

  manifest.push({ ...group, shots });
}

await browser.close();

const { writeFile } = await import('node:fs/promises');
await writeFile(join(OUT, 'manifest.json'), `${JSON.stringify(manifest, null, 2)}\n`);
process.stdout.write(`\n  ${manifest.reduce((n, g) => n + g.shots.length, 0)} screenshots → docs/screenshots/\n`);
