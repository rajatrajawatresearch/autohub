'use client';

/**
 * Search.
 *
 * Infinite, cursor-based, and deliberately *not* cached hard — a listing that sold ten minutes ago
 * must not still be offered. Facets come back with every page; the rail reads the first page's.
 */

import { useInfiniteQuery } from '@tanstack/react-query';
import { useMemo } from 'react';
import { search } from '../endpoints/catalog';
import type { SearchFacet, SearchItem, SearchParams, SearchResponse } from '../schemas';
import { queryKeys } from './keys';

export interface UseSearchResult {
  items: SearchItem[];
  facets: SearchFacet[];
  totalEstimate: number | null;
  hasNextPage: boolean;
  isFetchingNextPage: boolean;
  fetchNextPage: () => void;
  isLoading: boolean;
  isError: boolean;
  error: Error | null;
  refetch: () => void;
}

export function useSearch(params: SearchParams, enabled = true): UseSearchResult {
  const query = useInfiniteQuery<SearchResponse, Error, SearchResponse[], readonly unknown[], string | null>({
    queryKey: queryKeys.search(params),
    queryFn: ({ pageParam }) => search({ ...params, cursor: pageParam }),
    initialPageParam: null,
    getNextPageParam: (lastPage) => lastPage.nextCursor ?? null,
    select: (data) => data.pages,
    enabled,
    staleTime: 30_000,
  });

  const pages = query.data ?? [];

  const items = useMemo(() => pages.flatMap((page) => page.items), [pages]);
  const facets = pages[0]?.facets ?? [];
  const totalEstimate = pages[0]?.totalEstimate ?? null;

  return {
    items,
    facets,
    totalEstimate,
    hasNextPage: query.hasNextPage,
    isFetchingNextPage: query.isFetchingNextPage,
    fetchNextPage: () => {
      void query.fetchNextPage();
    },
    isLoading: query.isLoading,
    isError: query.isError,
    error: query.error,
    refetch: () => {
      void query.refetch();
    },
  };
}
