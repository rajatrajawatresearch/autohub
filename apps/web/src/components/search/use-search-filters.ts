'use client';

import { useCallback, useMemo } from 'react';
import { usePathname, useRouter, useSearchParams } from 'next/navigation';
import type { AttributeFilter, SearchSort } from '@autohub/api';

export interface SearchFilters {
  q: string;
  sort: SearchSort;
  /** Keyed by taxonomy attribute code. This app never knows what the codes are. */
  attributes: Record<string, AttributeFilter>;
}

const DEFAULT_SORT: SearchSort = 'relevance';

/**
 * Filters live in the URL, not in React state.
 *
 * A filtered result set is the thing people send each other on WhatsApp — "look at this one" is
 * half the traffic on a marketplace like this. Holding the filters in component state would make
 * that link go to an unfiltered page, and would lose the user's filters on a back-navigation.
 *
 * The encoding matches the API contract's query grammar so the URL a user shares and the query the
 * server runs are the same thing:
 *     ?q=swift&sort=price_asc&attr.fuel_type=petrol,cng&attr.kms_driven=0..40000
 */
export function useSearchFilters(): {
  filters: SearchFilters;
  setFilters: (next: Partial<SearchFilters>) => void;
  clear: () => void;
  activeCount: number;
} {
  const router = useRouter();
  const pathname = usePathname();
  const params = useSearchParams();

  const filters = useMemo<SearchFilters>(() => {
    const attributes: Record<string, AttributeFilter> = {};

    for (const [key, raw] of params.entries()) {
      if (!key.startsWith('attr.')) continue;
      const code = key.slice('attr.'.length);

      if (raw.includes('..')) {
        const [min, max] = raw.split('..');
        attributes[code] = {
          min: min ? Number(min) : undefined,
          max: max ? Number(max) : undefined,
        };
      } else {
        attributes[code] = raw.split(',').filter(Boolean);
      }
    }

    return {
      q: params.get('q') ?? '',
      sort: (params.get('sort') as SearchSort | null) ?? DEFAULT_SORT,
      attributes,
    };
  }, [params]);

  const setFilters = useCallback(
    (next: Partial<SearchFilters>) => {
      const merged = { ...filters, ...next };
      const search = new URLSearchParams();

      if (merged.q) search.set('q', merged.q);
      if (merged.sort !== DEFAULT_SORT) search.set('sort', merged.sort);

      for (const [code, value] of Object.entries(merged.attributes)) {
        if (Array.isArray(value)) {
          if (value.length > 0) search.set(`attr.${code}`, value.join(','));
        } else if (typeof value === 'object' && value !== null) {
          search.set(`attr.${code}`, `${value.min ?? ''}..${value.max ?? ''}`);
        } else if (typeof value === 'boolean' && value) {
          search.set(`attr.${code}`, 'true');
        }
      }

      const query = search.toString();
      // scroll:false — a filter change should not throw the user back to the top of the results
      // they are reading.
      //
      // The cast is the one place typed routes cannot help: the path is the page we are already on
      // and the query string is built from registry-defined attribute codes, so neither is knowable
      // at compile time. Navigating to where we already are is safe by construction.
      router.replace((query ? `${pathname}?${query}` : pathname) as Parameters<typeof router.replace>[0], {
        scroll: false,
      });
    },
    [filters, pathname, router],
  );

  const clear = useCallback(() => {
    router.replace(pathname as Parameters<typeof router.replace>[0], { scroll: false });
  }, [pathname, router]);

  const activeCount = Object.keys(filters.attributes).length + (filters.q ? 1 : 0);

  return { filters, setFilters, clear, activeCount };
}
