/**
 * Registries #6, #9 and #10 — Feature & Module, Localisation, Geography.
 *
 * All three are small, hot, and cached hard: they gate what the shell renders before the first
 * screen paints.
 */

import { z } from 'zod';
import { RegistryVersionSchema } from './common';

/* ── #6 Feature & Module ─────────────────────────────────────────────────────
   Evaluated server-side for the calling user. The client receives booleans and never targeting
   rules — targeting logic on the client is targeting logic an attacker can read. */

export const FeaturesSchema = z.object({
  version: RegistryVersionSchema,
  /** `auction`, `lease`, `attachment` — whole modules, on or off for this user in this city. */
  modules: z.record(z.boolean()).default({}),
  /** `instant_offer`, `ai_damage_detection`, `whatsapp_chat` — behaviour inside a module. */
  flags: z.record(z.boolean()).default({}),
});
export type Features = z.infer<typeof FeaturesSchema>;

/* ── #9 Localisation ─────────────────────────────────────────────────────────
   Missing key → fall back to en-IN → fall back to the key itself. Never render an empty string. */

export const SUPPORTED_LOCALES = [
  'en-IN',
  'hi-IN',
  'mr-IN',
  'ta-IN',
  'te-IN',
  'bn-IN',
  'gu-IN',
  'kn-IN',
] as const;

export const LocaleSchema = z.enum(SUPPORTED_LOCALES).catch('en-IN');
export type Locale = z.infer<typeof LocaleSchema>;

export const I18nBundleSchema = z.object({
  locale: LocaleSchema,
  version: RegistryVersionSchema,
  resources: z.record(z.string()).default({}),
});
export type I18nBundle = z.infer<typeof I18nBundleSchema>;

/* ── #10 Geography & Master Data ─────────────────────────────────────────── */

export const CitySchema = z.object({
  code: z.string(),
  name: z.string(),
  stateCode: z.string(),
  lat: z.number().optional(),
  lng: z.number().optional(),
  serviceable: z.boolean().default(false),
  /** Which modules are live here. Lease in Bengaluru only is a registry row, not a deploy. */
  modules: z.array(z.string()).default([]),
});
export type City = z.infer<typeof CitySchema>;

export const CitiesResponseSchema = z.object({
  version: RegistryVersionSchema,
  items: z.array(CitySchema),
});
export type CitiesResponse = z.infer<typeof CitiesResponseSchema>;
