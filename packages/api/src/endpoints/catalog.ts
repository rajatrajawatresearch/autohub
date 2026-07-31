/**
 * Search. The query grammar is open by design — attribute codes come from the taxonomy registry,
 * so this module serialises whatever the caller was told exists rather than a fixed field list.
 */

import { apiGet } from '../http';
import type { QueryParams, RequestContext } from '../http';
import { SearchResponseSchema } from '../schemas';
import type { AttributeFilter, RangeFilter, SearchParams, SearchResponse } from '../schemas';

/** `0..40000`, `..40000`, `300000..` — an open end is an open end. */
function rangeToken(range: RangeFilter): string | undefined {
  if (range.min === undefined && range.max === undefined) return undefined;
  return `${range.min ?? ''}..${range.max ?? ''}`;
}

function filterToken(filter: AttributeFilter): string | undefined {
  if (Array.isArray(filter)) return filter.length > 0 ? filter.join(',') : undefined;
  if (typeof filter === 'boolean') return String(filter);
  return rangeToken(filter);
}

/** Exposed so a page can build a shareable URL that round-trips through the same grammar. */
export function toSearchQuery(params: SearchParams): QueryParams {
  const query: QueryParams = {
    q: params.q,
    city: params.city,
    intent: params.intent,
    sort: params.sort,
    cursor: params.cursor ?? undefined,
    limit: params.limit,
  };

  if (params.price) query.price = rangeToken(params.price);

  for (const [code, filter] of Object.entries(params.attributes ?? {})) {
    const token = filterToken(filter);
    if (token !== undefined) query[`attr.${code}`] = token;
  }

  return query;
}

export function search(params: SearchParams, ctx: RequestContext = {}): Promise<SearchResponse> {
  return apiGet('/catalog/search', SearchResponseSchema, {
    ...ctx,
    query: toSearchQuery(params),
  });
}
