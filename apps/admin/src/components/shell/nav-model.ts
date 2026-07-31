/**
 * The console's route map.
 *
 * Declared `as const` so every `href` stays a literal and Next's typed routes can check it. The
 * three groups are the three jobs this console does: work the queues, edit the registries, run the
 * platform. Nothing else belongs in the rail.
 */
export const CONSOLE_NAV = [
  {
    code: 'operations',
    label: 'Operations',
    items: [
      { href: '/', label: 'Dashboard' },
      { href: '/moderation', label: 'Moderation' },
      { href: '/dealers', label: 'Dealers' },
      { href: '/disputes', label: 'Disputes' },
    ],
  },
  {
    code: 'registry',
    label: 'Registries',
    items: [
      { href: '/registry/taxonomy', label: 'Taxonomy' },
      { href: '/registry/forms', label: 'Forms' },
      { href: '/registry/workflows', label: 'Workflows' },
      { href: '/registry/pricing', label: 'Pricing' },
      { href: '/registry/notifications', label: 'Notifications' },
      { href: '/registry/features', label: 'Features' },
      { href: '/registry/i18n', label: 'Localisation' },
      { href: '/registry/geo', label: 'Geography' },
    ],
  },
  {
    code: 'system',
    label: 'System',
    items: [
      { href: '/system/roles', label: 'Roles' },
      { href: '/system/audit', label: 'Audit log' },
      { href: '/system/integrations', label: 'Integrations' },
    ],
  },
] as const;

export type ConsoleNavHref = (typeof CONSOLE_NAV)[number]['items'][number]['href'];

export interface ConsoleNavItem {
  href: ConsoleNavHref;
  label: string;
}

/**
 * The same map, flattened, for the horizontal strip.
 *
 * Built with an explicit accumulator rather than `flatMap`: because every group's `items` is a
 * distinct readonly tuple under `as const`, `flatMap` widens the callback's return to a union of
 * tuples and loses the element type entirely. Pushing into a typed array keeps each item's `href`
 * literal, which is what Next's typed routes check against.
 */
export const CONSOLE_NAV_ITEMS: readonly ConsoleNavItem[] = CONSOLE_NAV.reduce<ConsoleNavItem[]>(
  (all, group) => {
    all.push(...group.items);
    return all;
  },
  [],
);
