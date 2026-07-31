/**
 * The one place in the product that knows a backend exists.
 *
 * Responsibilities, and nothing beyond them:
 *   · build the request — headers, query grammar, idempotency key, deadline
 *   · parse the response — zod on success, RFC 9457 Problem Details into a typed `ApiError`
 *   · recover once — a single refresh on 401, and the fixtures when the API is unreachable
 *
 * Retries happen on `GET` only. A retried bid is a duplicated bid; a retried payment is a
 * duplicated payment. The `Idempotency-Key` protects the server, but the client still refuses to
 * replay a write on its own initiative.
 */

import type { z } from 'zod';
import { getApiConfig } from './config';
import { resolveMock } from './mock';
import { CLIENT_ERROR_CODES, ProblemDetailsSchema } from './schemas/problem';
import type { ProblemDetails } from './schemas/problem';

export type HttpMethod = 'GET' | 'POST' | 'PUT' | 'PATCH' | 'DELETE';

const IDEMPOTENT_METHODS: ReadonlySet<string> = new Set(['GET']);

export type QueryValue = string | number | boolean | string[] | null | undefined;
export type QueryParams = Record<string, QueryValue>;

/* ── Errors ──────────────────────────────────────────────────────────────── */

/**
 * One error type for every failure — server, network or deadline — so callers branch on one
 * vocabulary: `error.code`. Never on `error.message`, which is human text and will be reworded.
 */
export class ApiError extends Error {
  readonly status: number;
  readonly code: string;
  readonly problem: ProblemDetails | null;
  readonly metadata: Record<string, unknown>;
  /** Keyed by field key (JSON pointer with the leading slash stripped). */
  readonly fieldErrors: Record<string, string[]>;
  readonly retryAfterSeconds: number | null;
  readonly traceId: string | null;

  constructor(status: number, code: string, message: string, problem: ProblemDetails | null = null) {
    super(message);
    this.name = 'ApiError';
    this.status = status;
    this.code = code;
    this.problem = problem;
    this.metadata = problem?.metadata ?? {};
    this.fieldErrors = collectFieldErrors(problem);
    this.retryAfterSeconds = problem?.retryAfterSeconds ?? null;
    this.traceId = problem?.traceId ?? null;
  }

  /** `error.is('auction.bid.below_increment')` reads the way the check is meant to be made. */
  is(code: string): boolean {
    return this.code === code;
  }

  /** True for a whole module or resource: `startsWith('auction.bid.')`. */
  startsWith(prefix: string): boolean {
    return this.code.startsWith(prefix);
  }

  static isApiError(value: unknown): value is ApiError {
    return value instanceof ApiError;
  }
}

function collectFieldErrors(problem: ProblemDetails | null): Record<string, string[]> {
  const result: Record<string, string[]> = {};
  if (!problem) return result;

  for (const entry of problem.errors ?? []) {
    const key = (entry.pointer ?? '').replace(/^\//, '').replace(/\//g, '.');
    if (!key) continue;
    const bucket = result[key] ?? [];
    bucket.push(entry.message ?? entry.code);
    result[key] = bucket;
  }

  const fromMetadata = problem.metadata?.fieldErrors;
  if (fromMetadata && typeof fromMetadata === 'object') {
    for (const [key, value] of Object.entries(fromMetadata as Record<string, unknown>)) {
      const codes = Array.isArray(value) ? value.map(String) : [String(value)];
      result[key] = [...(result[key] ?? []), ...codes];
    }
  }

  return result;
}

function toApiError(status: number, payload: unknown, fallbackMessage: string): ApiError {
  const parsed = ProblemDetailsSchema.safeParse(payload);
  const problem = parsed.success ? parsed.data : null;
  const code = problem?.code ?? `common.http.${status}`;
  const message = problem?.detail ?? problem?.title ?? fallbackMessage;
  return new ApiError(status, code, message, problem);
}

/* ── Device identity ─────────────────────────────────────────────────────── */

const DEVICE_ID_KEY = 'ah_device_id';
let memoryDeviceId: string | null = null;

function randomUuid(): string {
  const cryptoObj = globalThis.crypto as Crypto | undefined;
  if (cryptoObj && typeof cryptoObj.randomUUID === 'function') return cryptoObj.randomUUID();
  /* Not cryptographic — a correlation id, and only where the platform has no crypto. */
  return 'xxxxxxxx-xxxx-4xxx-yxxx-xxxxxxxxxxxx'.replace(/[xy]/g, (char) => {
    const rand = Math.floor(Math.random() * 16);
    const value = char === 'x' ? rand : (rand & 0x3) | 0x8;
    return value.toString(16);
  });
}

/** Stable per install. Binds refresh tokens and feeds fraud signals. */
export function getDeviceId(): string {
  if (memoryDeviceId) return memoryDeviceId;
  if (typeof localStorage !== 'undefined') {
    const stored = localStorage.getItem(DEVICE_ID_KEY);
    if (stored) {
      memoryDeviceId = stored;
      return stored;
    }
  }
  const created = randomUuid();
  memoryDeviceId = created;
  if (typeof localStorage !== 'undefined') {
    try {
      localStorage.setItem(DEVICE_ID_KEY, created);
    } catch {
      /* Private browsing. A per-session device id is still better than none. */
    }
  }
  return created;
}

/* ── Token store ─────────────────────────────────────────────────────────── */

export interface AuthTokens {
  accessToken: string;
  refreshToken: string;
  /** Epoch milliseconds. */
  expiresAt?: number;
}

let tokens: AuthTokens | null = null;
const authListeners = new Set<(tokens: AuthTokens | null) => void>();

/**
 * Access and refresh tokens live in memory only. A refresh token in `localStorage` is a refresh
 * token any injected script can read; the production web shell holds it in a `__Host-` cookie the
 * JavaScript never sees.
 */
export function setAuthTokens(next: AuthTokens | null): void {
  tokens = next;
  for (const listener of authListeners) listener(next);
}

export function getAuthTokens(): AuthTokens | null {
  return tokens;
}

export function onAuthChange(listener: (tokens: AuthTokens | null) => void): () => void {
  authListeners.add(listener);
  return () => {
    authListeners.delete(listener);
  };
}

/* ── Query serialisation ─────────────────────────────────────────────────── */

export function buildQueryString(params: QueryParams | undefined): string {
  if (!params) return '';
  const search = new URLSearchParams();
  for (const [key, value] of Object.entries(params)) {
    if (value === undefined || value === null || value === '') continue;
    /* Multi-value filters are OR-within-the-attribute and travel comma-joined, per §9. */
    search.set(key, Array.isArray(value) ? value.join(',') : String(value));
  }
  const serialised = search.toString();
  return serialised ? `?${serialised}` : '';
}

function queryToRecord(params: QueryParams | undefined): Record<string, string> {
  const record: Record<string, string> = {};
  if (!params) return record;
  for (const [key, value] of Object.entries(params)) {
    if (value === undefined || value === null || value === '') continue;
    record[key] = Array.isArray(value) ? value.join(',') : String(value);
  }
  return record;
}

/* ── Request options ─────────────────────────────────────────────────────── */

export interface RequestContext {
  /** Overrides `Accept-Language` for this call. */
  locale?: string;
  /** Overrides `X-Active-City` for this call. */
  city?: string;
  /** Server components pass the caller's token explicitly; a server has no ambient session. */
  accessToken?: string;
  signal?: AbortSignal;
  timeoutMs?: number;
  /** Forwarded to `fetch` on the server for Next's data cache. Ignored in the browser. */
  cache?: RequestCache;
  revalidate?: number | false;
  tags?: string[];
}

export interface ApiRequestOptions extends RequestContext {
  method?: HttpMethod;
  query?: QueryParams;
  body?: unknown;
  /** Response schema. Omit when the endpoint answers `204`. */
  schema?: z.ZodTypeAny;
  /** Supply your own key when one user gesture must stay one operation across a retry. */
  idempotencyKey?: string;
  /** Set false for endpoints that must not carry a bearer token (the first valuation). */
  auth?: boolean;
  headers?: Record<string, string>;
}

/* ── Mock fallback bookkeeping ───────────────────────────────────────────── */

let mockUntil = 0;
let warnedOffline = false;

function serveFromMock(
  method: HttpMethod,
  path: string,
  query: QueryParams | undefined,
  body: unknown,
): { status: number; body: unknown } | null {
  return resolveMock(method, path, queryToRecord(query), body);
}

function noteApiUnreachable(): void {
  mockUntil = Date.now() + 30_000;
  if (!warnedOffline) {
    warnedOffline = true;
    console.warn(
      '[@autohub/api] API unreachable — serving fixtures. Set NEXT_PUBLIC_API_MOCK=1 to do this deliberately.',
    );
  }
}

/** True when the last call fell back to fixtures. Surfaces a "demo data" marker in the shell. */
export function isServingMockData(): boolean {
  return getApiConfig().mock || Date.now() < mockUntil;
}

/* ── Refresh, once ───────────────────────────────────────────────────────── */

let refreshInFlight: Promise<boolean> | null = null;

async function refreshOnce(): Promise<boolean> {
  const current = tokens;
  if (!current?.refreshToken) return false;
  if (refreshInFlight) return refreshInFlight;

  refreshInFlight = (async () => {
    try {
      const config = getApiConfig();
      const response = await fetch(`${config.baseUrl}/auth/refresh`, {
        method: 'POST',
        headers: { 'Content-Type': 'application/json', 'X-Device-Id': getDeviceId() },
        body: JSON.stringify({ refreshToken: current.refreshToken, deviceId: getDeviceId() }),
      });
      if (!response.ok) {
        setAuthTokens(null);
        return false;
      }
      const payload = (await response.json()) as Partial<AuthTokens> | null;
      if (!payload?.accessToken || !payload.refreshToken) {
        setAuthTokens(null);
        return false;
      }
      setAuthTokens({
        accessToken: payload.accessToken,
        refreshToken: payload.refreshToken,
        expiresAt: payload.expiresAt,
      });
      return true;
    } catch {
      /* A network failure during refresh is not a revoked session — keep what we have. */
      return false;
    } finally {
      refreshInFlight = null;
    }
  })();

  return refreshInFlight;
}

/* ── The request ─────────────────────────────────────────────────────────── */

interface RawResponse {
  status: number;
  payload: unknown;
}

function safeJson(text: string): unknown {
  try {
    return JSON.parse(text) as unknown;
  } catch {
    return null;
  }
}

async function performFetch(
  url: string,
  method: HttpMethod,
  options: ApiRequestOptions,
  token: string | undefined,
): Promise<RawResponse> {
  const config = getApiConfig();
  const controller = new AbortController();
  const timeoutMs = options.timeoutMs ?? config.timeoutMs;

  let timedOut = false;
  const timer = setTimeout(() => {
    timedOut = true;
    controller.abort();
  }, timeoutMs);

  const external = options.signal;
  const forwardAbort = () => controller.abort();
  if (external) {
    if (external.aborted) controller.abort();
    else external.addEventListener('abort', forwardAbort);
  }

  const headers: Record<string, string> = {
    Accept: 'application/json',
    'Accept-Language': options.locale ?? config.defaultLocale,
    'X-Device-Id': getDeviceId(),
    'X-Client': config.clientId,
    'X-Request-Id': randomUuid(),
    ...options.headers,
  };

  const city = options.city ?? config.defaultCity;
  if (city) headers['X-Active-City'] = city;
  if (token) headers.Authorization = `Bearer ${token}`;

  if (!IDEMPOTENT_METHODS.has(method)) {
    /* Required on every write. A replay returns the original response rather than acting twice. */
    headers['Idempotency-Key'] = options.idempotencyKey ?? randomUuid();
  }

  let serialisedBody: string | undefined;
  if (options.body !== undefined && method !== 'GET') {
    headers['Content-Type'] = 'application/json';
    serialisedBody = JSON.stringify(options.body);
  }

  const nextOptions: { revalidate?: number | false; tags?: string[] } = {};
  if (options.revalidate !== undefined) nextOptions.revalidate = options.revalidate;
  if (options.tags) nextOptions.tags = options.tags;

  const init: RequestInit & { next?: { revalidate?: number | false; tags?: string[] } } = {
    method,
    headers,
    signal: controller.signal,
  };
  if (serialisedBody !== undefined) init.body = serialisedBody;
  if (options.cache) init.cache = options.cache;
  if (Object.keys(nextOptions).length > 0) init.next = nextOptions;

  try {
    const response = await fetch(url, init);
    if (response.status === 204) return { status: 204, payload: null };
    const text = await response.text();
    return { status: response.status, payload: text ? safeJson(text) : null };
  } catch (error) {
    if (timedOut) {
      throw new ApiError(
        408,
        CLIENT_ERROR_CODES.timeout,
        `That took longer than ${Math.round(timeoutMs / 1000)} seconds. Try again.`,
      );
    }
    if (external?.aborted) {
      throw new ApiError(0, CLIENT_ERROR_CODES.aborted, 'Request cancelled.');
    }
    throw new ApiError(
      0,
      CLIENT_ERROR_CODES.unreachable,
      'Cannot reach AutoHub. Check your connection and try again.',
      { code: CLIENT_ERROR_CODES.unreachable, status: 0, detail: String(error) },
    );
  } finally {
    clearTimeout(timer);
    external?.removeEventListener('abort', forwardAbort);
  }
}

function parseOrThrow(schema: z.ZodTypeAny | undefined, payload: unknown, path: string): unknown {
  if (!schema) return undefined;
  const result = schema.safeParse(payload);
  if (result.success) return result.data;
  throw new ApiError(
    200,
    CLIENT_ERROR_CODES.malformedResponse,
    `The response from ${path} did not match the contract.`,
    {
      code: CLIENT_ERROR_CODES.malformedResponse,
      status: 200,
      detail: result.error.issues
        .slice(0, 5)
        .map((issue) => `${issue.path.join('.') || '(root)'}: ${issue.message}`)
        .join('; '),
      metadata: { issues: result.error.issues },
    },
  );
}

const MAX_GET_ATTEMPTS = 3;

function retryDelayMs(attempt: number): number {
  return 200 * 3 ** (attempt - 1);
}

function sleep(ms: number): Promise<void> {
  return new Promise((resolve) => setTimeout(resolve, ms));
}

/**
 * Issues one API call.
 *
 * Endpoint modules are the intended callers — an app that reaches for this directly is an app that
 * will grow a second copy of the header logic.
 */
export function apiRequest<T>(
  path: string,
  options: ApiRequestOptions & { schema: z.ZodType<T, z.ZodTypeDef, unknown> },
): Promise<T>;
export function apiRequest(path: string, options?: ApiRequestOptions): Promise<undefined>;
export async function apiRequest(path: string, options: ApiRequestOptions = {}): Promise<unknown> {
  const config = getApiConfig();
  const method = options.method ?? 'GET';
  const schema = options.schema;

  const useMock = config.mock || (config.mockFallback && Date.now() < mockUntil);
  if (useMock) {
    const mocked = serveFromMock(method, path, options.query, options.body);
    if (mocked) {
      if (mocked.status >= 400) {
        throw toApiError(mocked.status, mocked.body, 'The request could not be completed.');
      }
      if (mocked.status === 204) return undefined;
      return parseOrThrow(schema, mocked.body, path);
    }
    if (config.mock) {
      throw new ApiError(
        404,
        'common.resource.not_found',
        `No fixture for ${method} ${path}. Add one in packages/api/src/mock.`,
      );
    }
  }

  const url = `${config.baseUrl}${path}${buildQueryString(options.query)}`;
  const wantsAuth = options.auth !== false;
  let token = options.accessToken ?? (wantsAuth ? tokens?.accessToken : undefined);
  let attempt = 0;

  for (;;) {
    attempt += 1;
    let raw: RawResponse;
    try {
      raw = await performFetch(url, method, options, token);
    } catch (error) {
      const offline =
        ApiError.isApiError(error) &&
        (error.code === CLIENT_ERROR_CODES.unreachable || error.code === CLIENT_ERROR_CODES.timeout);

      if (offline && config.mockFallback) {
        const mocked = serveFromMock(method, path, options.query, options.body);
        if (mocked) {
          noteApiUnreachable();
          if (mocked.status >= 400) {
            throw toApiError(mocked.status, mocked.body, 'The request could not be completed.');
          }
          if (mocked.status === 204) return undefined;
          return parseOrThrow(schema, mocked.body, path);
        }
      }

      if (offline && IDEMPOTENT_METHODS.has(method) && attempt < MAX_GET_ATTEMPTS) {
        await sleep(retryDelayMs(attempt));
        continue;
      }
      throw error;
    }

    /* One refresh, then fail. A refresh loop on a revoked token is a logout that never happens. */
    if (raw.status === 401 && wantsAuth && !options.accessToken && attempt === 1) {
      const refreshed = await refreshOnce();
      if (refreshed) {
        token = tokens?.accessToken;
        continue;
      }
      setAuthTokens(null);
      throw toApiError(raw.status, raw.payload, 'Your session has ended. Sign in again.');
    }

    if (raw.status >= 500 && IDEMPOTENT_METHODS.has(method) && attempt < MAX_GET_ATTEMPTS) {
      await sleep(retryDelayMs(attempt));
      continue;
    }

    if (raw.status >= 400) {
      throw toApiError(raw.status, raw.payload, 'The request could not be completed.');
    }

    if (raw.status === 204) return undefined;
    return parseOrThrow(schema, raw.payload, path);
  }
}

/* ── Verb helpers ────────────────────────────────────────────────────────── */

type VerbOptions = Omit<ApiRequestOptions, 'schema' | 'method' | 'body'>;

export function apiGet<TSchema extends z.ZodTypeAny>(
  path: string,
  schema: TSchema,
  options: VerbOptions = {},
): Promise<z.infer<TSchema>> {
  return apiRequest<z.infer<TSchema>>(path, { ...options, method: 'GET', schema });
}

export function apiPost<TSchema extends z.ZodTypeAny>(
  path: string,
  schema: TSchema,
  body: unknown,
  options: VerbOptions = {},
): Promise<z.infer<TSchema>> {
  return apiRequest<z.infer<TSchema>>(path, { ...options, method: 'POST', body, schema });
}

export function apiPatch<TSchema extends z.ZodTypeAny>(
  path: string,
  schema: TSchema,
  body: unknown,
  options: VerbOptions = {},
): Promise<z.infer<TSchema>> {
  return apiRequest<z.infer<TSchema>>(path, { ...options, method: 'PATCH', body, schema });
}

export function apiPostVoid(path: string, body: unknown, options: VerbOptions = {}): Promise<undefined> {
  return apiRequest(path, { ...options, method: 'POST', body });
}

export function apiDelete(path: string, options: VerbOptions = {}): Promise<undefined> {
  return apiRequest(path, { ...options, method: 'DELETE' });
}
