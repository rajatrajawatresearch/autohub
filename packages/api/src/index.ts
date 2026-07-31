/**
 * `@autohub/api` — the only module in the product that knows a backend exists.
 *
 * Apps import from here. Nothing in `apps/*` calls `fetch`, builds a URL, sets a header, or writes
 * a response type by hand. When the backend changes, this package changes and the three portals
 * do not.
 *
 * Server Components import from `@autohub/api/server` instead — same schemas, no react-query.
 *
 * Mock mode: set `NEXT_PUBLIC_API_MOCK=1` to serve every call from `src/mock`. Without it, the
 * fixtures still take over automatically when the API is unreachable, so a portal is demonstrable
 * whether or not the backend is up.
 */

/* ── Configuration ───────────────────────────────────────────────────────── */
export { getApiConfig, configureApi, DEFAULT_BASE_URL } from './config';
export type { ApiConfig } from './config';

/* ── Transport ───────────────────────────────────────────────────────────── */
export {
  ApiError,
  apiRequest,
  apiGet,
  apiPost,
  apiPatch,
  apiPostVoid,
  apiDelete,
  buildQueryString,
  getDeviceId,
  getAuthTokens,
  setAuthTokens,
  onAuthChange,
  isServingMockData,
} from './http';
export type {
  ApiRequestOptions,
  AuthTokens,
  HttpMethod,
  QueryParams,
  QueryValue,
  RequestContext,
} from './http';

/* ── Contract ────────────────────────────────────────────────────────────── */
export * from './schemas';

/* ── Endpoints ───────────────────────────────────────────────────────────── */
export * from './endpoints';

/* ── Hooks ───────────────────────────────────────────────────────────────── */
export * from './hooks';

/* ── Query client ────────────────────────────────────────────────────────── */
export { createQueryClient, QueryProvider } from './query-client';
export type { QueryProviderProps } from './query-client';
