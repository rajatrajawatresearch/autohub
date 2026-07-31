import { Archivo, Instrument_Sans, JetBrains_Mono } from 'next/font/google';

/**
 * Three faces, three jobs. Self-hosted by next/font — no request ever leaves the origin,
 * so there is no third-party font hop on the critical path and no layout shift on load.
 */

/**
 * Display. Archivo carries a real width axis, so one family covers signage-scale headlines
 * (expanded, 700) and ordinary interface text (normal) without dressing up as a stencil.
 */
export const archivo = Archivo({
  subsets: ['latin'],
  variable: '--font-archivo',
  display: 'swap',
  axes: ['wdth'],
});

/**
 * Body. Warmer than Inter and still readable at 13px inside a dealer's inventory table —
 * which is where most of this product's reading actually happens.
 */
export const instrumentSans = Instrument_Sans({
  subsets: ['latin'],
  variable: '--font-instrument',
  display: 'swap',
});

/**
 * Data. Plates, VIN, chassis, odometer, money, ids.
 * Unambiguous 0/O and 1/l matters when someone is transcribing a chassis number off a stamped
 * plate under a bonnet; the slashed zero is enabled in tokens.css.
 */
export const jetbrainsMono = JetBrains_Mono({
  subsets: ['latin'],
  variable: '--font-jetbrains',
  display: 'swap',
});

/** Apply to <html> so every token that references a font variable resolves. */
export const fontVariables = [archivo.variable, instrumentSans.variable, jetbrainsMono.variable].join(
  ' ',
);
