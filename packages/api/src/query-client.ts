'use client';

/**
 * The shared QueryClient.
 *
 * Two decisions worth stating. First, a 4xx is never retried: a `422` will fail identically the
 * second time and a `403` is not a network blip. Second, focus refetching is off — a dealer leaves
 * the inventory tab open all day and every alt-tab must not restart the page.
 */

import { QueryClient, QueryClientProvider } from '@tanstack/react-query';
import { createElement, useState } from 'react';
import type { ReactNode } from 'react';
import { getApiConfig } from './config';
import { ApiError } from './http';

export function createQueryClient(): QueryClient {
  const config = getApiConfig();

  return new QueryClient({
    defaultOptions: {
      queries: {
        /* Registry hooks raise this to 5 minutes individually; everything else is fresh-ish. */
        staleTime: 30_000,
        gcTime: 5 * 60_000,
        refetchOnWindowFocus: false,
        refetchOnReconnect: true,
        retry: (failureCount, error) => {
          if (config.mock) return false;
          if (ApiError.isApiError(error) && error.status >= 400 && error.status < 500) return false;
          return failureCount < 2;
        },
        retryDelay: (attempt) => Math.min(1000 * 3 ** attempt, 8000),
      },
      mutations: {
        /* Writes are never retried by the client. The server's Idempotency-Key is for the network,
           not for us to lean on. */
        retry: false,
      },
    },
  });
}

export interface QueryProviderProps {
  children: ReactNode;
  /** Pass a client when the app needs one shared with something else (tests, storybook). */
  client?: QueryClient;
}

/**
 * Wraps a portal. Created inside `useState` so a re-render never mints a second client and throws
 * the cache away.
 */
export function QueryProvider({ children, client }: QueryProviderProps) {
  const [fallbackClient] = useState(() => createQueryClient());
  return createElement(QueryClientProvider, { client: client ?? fallbackClient }, children);
}
