/**
 * Runtime configuration for the API layer.
 *
 * Everything here is transport configuration — hosts, timeouts, locale defaults. No business
 * constant lives in this package: fuel types, statuses, fees and facets all arrive from the
 * registry endpoints at runtime (North Star §2).
 *
 * Environment variables are read as *literal* member expressions so Next's compiler can inline
 * `NEXT_PUBLIC_*` into the client bundle. A dynamic lookup (`process.env[key]`) is not inlined and
 * would silently read `undefined` in the browser.
 */

declare const process: { env: Record<string, string | undefined> };

function readPublicEnv(): Record<string, string | undefined> {
  if (typeof process === 'undefined' || !process.env) return {};
  return {
    NEXT_PUBLIC_API_URL: process.env.NEXT_PUBLIC_API_URL,
    NEXT_PUBLIC_API_WS_URL: process.env.NEXT_PUBLIC_API_WS_URL,
    NEXT_PUBLIC_API_MOCK: process.env.NEXT_PUBLIC_API_MOCK,
    NEXT_PUBLIC_API_TIMEOUT_MS: process.env.NEXT_PUBLIC_API_TIMEOUT_MS,
    NEXT_PUBLIC_DEFAULT_LOCALE: process.env.NEXT_PUBLIC_DEFAULT_LOCALE,
    NEXT_PUBLIC_DEFAULT_CITY: process.env.NEXT_PUBLIC_DEFAULT_CITY,
    NEXT_PUBLIC_CLIENT_ID: process.env.NEXT_PUBLIC_CLIENT_ID,
  };
}

export interface ApiConfig {
  /** Absolute base, including `/api/v1`. No trailing slash. */
  readonly baseUrl: string;
  /** Websocket origin for `/realtime/*`. Derived from `baseUrl` when unset. */
  readonly wsUrl: string;
  /** Serve every request from the in-repo fixtures. `NEXT_PUBLIC_API_MOCK=1`. */
  readonly mock: boolean;
  /** When the API is unreachable, serve fixtures rather than failing the screen. */
  readonly mockFallback: boolean;
  /** Per-request deadline in milliseconds. */
  readonly timeoutMs: number;
  /** Sent as `Accept-Language`; the i18n registry resolves server-side strings with it. */
  readonly defaultLocale: string;
  /** Sent as `X-Active-City`; scopes search, pricing and feature evaluation. */
  readonly defaultCity: string | null;
  /** Sent as `X-Client`; used by the server for forced-upgrade and feature gating. */
  readonly clientId: string;
  /**
   * Registries change by the day, not by the second — cache them hard. Search does not get this.
   */
  readonly registryStaleMs: number;
}

const env = readPublicEnv();

function toBool(value: string | undefined, fallback: boolean): boolean {
  if (value === undefined || value === '') return fallback;
  return value === '1' || value.toLowerCase() === 'true';
}

function toInt(value: string | undefined, fallback: number): number {
  const parsed = Number.parseInt(value ?? '', 10);
  return Number.isFinite(parsed) ? parsed : fallback;
}

function stripTrailingSlash(value: string): string {
  return value.endsWith('/') ? value.slice(0, -1) : value;
}

function deriveWsUrl(httpBase: string): string {
  if (httpBase.startsWith('https://')) return `wss://${httpBase.slice('https://'.length)}`;
  if (httpBase.startsWith('http://')) return `ws://${httpBase.slice('http://'.length)}`;
  return httpBase;
}

export const DEFAULT_BASE_URL = 'http://localhost:5180/api/v1';

const baseUrl = stripTrailingSlash(env.NEXT_PUBLIC_API_URL || DEFAULT_BASE_URL);

let current: ApiConfig = {
  baseUrl,
  wsUrl: stripTrailingSlash(env.NEXT_PUBLIC_API_WS_URL || deriveWsUrl(baseUrl)),
  mock: toBool(env.NEXT_PUBLIC_API_MOCK, false),
  mockFallback: true,
  timeoutMs: toInt(env.NEXT_PUBLIC_API_TIMEOUT_MS, 15_000),
  defaultLocale: env.NEXT_PUBLIC_DEFAULT_LOCALE || 'en-IN',
  defaultCity: env.NEXT_PUBLIC_DEFAULT_CITY || null,
  clientId: env.NEXT_PUBLIC_CLIENT_ID || 'web/0.1.0',
  registryStaleMs: 5 * 60 * 1000,
};

export function getApiConfig(): ApiConfig {
  return current;
}

/**
 * Called once at app boot when a portal needs to override defaults (the dealer portal runs
 * `compact` density and a different client id; admin talks to the same API with a longer timeout).
 */
export function configureApi(patch: Partial<ApiConfig>): ApiConfig {
  current = { ...current, ...patch };
  return current;
}
