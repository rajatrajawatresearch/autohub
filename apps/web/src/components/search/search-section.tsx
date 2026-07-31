import { Suspense } from 'react';
import type { ListingIntent } from '@autohub/api';
import { Skeleton } from '@autohub/ui';
import { SearchResults } from './search-results';

/**
 * Filters live in the URL, so the results component reads `useSearchParams` — which opts the whole
 * subtree out of static prerendering unless it sits behind a Suspense boundary.
 *
 * The boundary is here rather than in each page so the three intents (buy, rent, lease) cannot
 * drift apart, and so the shell above it — heading, header, footer — still renders statically and
 * shows up immediately.
 */
export function SearchSection({ intent }: { intent: ListingIntent }) {
  return (
    <Suspense fallback={<ResultsSkeleton />}>
      <SearchResults intent={intent} />
    </Suspense>
  );
}

function ResultsSkeleton() {
  return (
    <div className="mx-auto max-w-[var(--container-content)] px-4 py-10 sm:px-6">
      <Skeleton className="h-[var(--density-control-h)] w-full max-w-sm" />
      <div className="mt-8 grid gap-10 lg:grid-cols-[15rem_1fr]">
        <div className="hidden space-y-6 lg:block">
          {Array.from({ length: 4 }, (_, i) => (
            <Skeleton key={i} className="h-24 w-full" />
          ))}
        </div>
        <div className="grid gap-5 sm:grid-cols-2 xl:grid-cols-3">
          {Array.from({ length: 6 }, (_, i) => (
            <Skeleton key={i} className="h-[19rem] w-full" />
          ))}
        </div>
      </div>
    </div>
  );
}
