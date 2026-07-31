/**
 * Value coercion for controls.
 *
 * Form values arrive from three directions — a registry `defaultValue`, a saved draft off the API,
 * and the user's own typing — and only the third has a type we chose. Every control therefore
 * treats its value as `unknown` and narrows it here, once, in one place.
 */

import type { MediaItem } from '../types';

export function asString(value: unknown): string {
  if (value === undefined || value === null) return '';
  if (typeof value === 'string') return value;
  if (typeof value === 'number' || typeof value === 'boolean') return String(value);
  return '';
}

export function asNumber(value: unknown): number | undefined {
  if (typeof value === 'number') return Number.isFinite(value) ? value : undefined;
  if (typeof value === 'string' && value.trim() !== '') {
    const parsed = Number(value);
    return Number.isFinite(parsed) ? parsed : undefined;
  }
  return undefined;
}

export function asBoolean(value: unknown): boolean {
  if (typeof value === 'boolean') return value;
  if (typeof value === 'string') return value === 'true';
  return false;
}

export function asStringArray(value: unknown): string[] {
  if (!Array.isArray(value)) return [];
  return value.filter((entry): entry is string => typeof entry === 'string');
}

export function asRecord(value: unknown): Record<string, unknown> {
  if (value !== null && typeof value === 'object' && !Array.isArray(value)) {
    return value as Record<string, unknown>;
  }
  return {};
}

export function asMediaItems(value: unknown): MediaItem[] {
  if (!Array.isArray(value)) return [];
  return value.filter(
    (entry): entry is MediaItem =>
      entry !== null && typeof entry === 'object' && typeof (entry as MediaItem).id === 'string',
  );
}

/** Digits only. Used wherever a user is transcribing a number off a document. */
export function digitsOnly(value: string): string {
  return value.replace(/\D/g, '');
}

/**
 * Rupees typed by a human → paise as an integer.
 *
 * String arithmetic, not `Math.round(value * 100)`: `8.4 * 100` is `840.0000000000001` in binary
 * floating point, and money that is off by a paise is money that fails a ledger reconciliation.
 */
export function rupeesToMinor(input: string): number | undefined {
  const cleaned = input.replace(/[^\d.-]/g, '');
  if (cleaned === '' || cleaned === '-' || cleaned === '.') return undefined;
  const negative = cleaned.startsWith('-');
  const [whole = '', fraction = ''] = cleaned.replace(/-/g, '').split('.');
  const paise = `${fraction}00`.slice(0, 2);
  const digits = `${whole === '' ? '0' : whole}${paise}`;
  const parsed = Number(digits);
  if (!Number.isFinite(parsed)) return undefined;
  return negative ? -parsed : parsed;
}

/** Paise → the rupee string a human edits. Trailing `.00` is dropped; a vehicle price has no paise. */
export function minorToRupees(minorUnits: number | undefined): string {
  if (minorUnits === undefined || !Number.isFinite(minorUnits)) return '';
  const negative = minorUnits < 0;
  const absolute = Math.abs(Math.round(minorUnits));
  const whole = Math.floor(absolute / 100);
  const remainder = absolute % 100;
  const body = remainder === 0 ? String(whole) : `${whole}.${String(remainder).padStart(2, '0')}`;
  return negative ? `-${body}` : body;
}

/** A client-side id for a media item. Stable across reorders; never sent as an entity id. */
export function localId(): string {
  const globalCrypto = globalThis.crypto as Crypto | undefined;
  if (globalCrypto && typeof globalCrypto.randomUUID === 'function') return globalCrypto.randomUUID();
  return `m-${Date.now().toString(36)}-${Math.random().toString(36).slice(2, 10)}`;
}
