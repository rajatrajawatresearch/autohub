'use client';

import { useMemo, useState } from 'react';
import { useRouter } from 'next/navigation';
import { useListings, type ListingSummary } from '@autohub/api';
import {
  Badge,
  Button,
  DataTable,
  EmptyState,
  Input,
  MoneyValue,
  PlateBlock,
  Skeleton,
  type DataTableColumn,
} from '@autohub/ui';

/**
 * The table is the product for a dealer.
 *
 * They live in this view: scanning plates, comparing ask against age, deciding what to push to
 * auction. So it is dense, monospaced where numbers are compared, keyboard-navigable, and it never
 * reflows while they are reading it.
 */
export function InventoryTable() {
  const router = useRouter();
  const [query, setQuery] = useState('');
  const listings = useListings({ limit: 60 });

  const rows = useMemo(() => {
    const items = listings.data?.items ?? [];
    if (!query.trim()) return items;

    const needle = query.trim().toLowerCase();
    return items.filter(
      (item) =>
        item.title.toLowerCase().includes(needle) ||
        item.registration.canonical.toLowerCase().includes(needle),
    );
  }, [listings.data, query]);

  const columns = useMemo<Array<DataTableColumn<ListingSummary>>>(
    () => [
      {
        id: 'plate',
        header: 'Plate',
        width: '10rem',
        cell: (row) => (
          <PlateBlock
            registration={row.registration.canonical}
            display={row.registration.display}
            plateClass={row.registration.plateClass}
            size="xs"
          />
        ),
      },
      {
        id: 'vehicle',
        header: 'Vehicle',
        cell: (row) => (
          <span className="block max-w-[26rem] truncate text-[color:var(--text-primary)]">
            {row.title}
          </span>
        ),
      },
      {
        id: 'kms',
        header: 'Kms',
        align: 'end',
        mono: true,
        hideBelow: 'md',
        cell: (row) => specValue(row, 'kms_driven') ?? '—',
      },
      {
        id: 'owners',
        header: 'Owners',
        align: 'end',
        mono: true,
        hideBelow: 'lg',
        cell: (row) => specValue(row, 'owners') ?? '—',
      },
      {
        id: 'city',
        header: 'City',
        hideBelow: 'md',
        cell: (row) => row.city,
      },
      {
        id: 'ask',
        header: 'Ask',
        align: 'end',
        mono: true,
        width: '8rem',
        cell: (row) =>
          typeof row.priceMinor === 'number' ? (
            <MoneyValue minorUnits={row.priceMinor} compact size="sm" />
          ) : (
            <span className="text-[color:var(--text-secondary)]">On request</span>
          ),
      },
      {
        id: 'badges',
        header: 'Checks',
        hideBelow: 'lg',
        cell: (row) => (
          <span className="flex flex-wrap gap-1">
            {row.badges.slice(0, 2).map((code) => (
              <Badge key={code} variant="outline" size="sm">
                {code.replace(/_/g, ' ')}
              </Badge>
            ))}
          </span>
        ),
      },
    ],
    [],
  );

  if (listings.isLoading) {
    return <Skeleton className="h-[28rem] w-full" />;
  }

  if (listings.isError) {
    return (
      <EmptyState
        title="Inventory did not load"
        body="The catalogue is unreachable. Retry — nothing has changed on your stock."
        action={
          <Button variant="outline" onClick={() => listings.refetch()}>
            Retry
          </Button>
        }
      />
    );
  }

  return (
    <>
      <div className="flex flex-wrap items-center gap-3 pb-3">
        <Input
          value={query}
          onChange={(event) => setQuery(event.target.value)}
          placeholder="Filter by plate or model"
          aria-label="Filter inventory"
          className="max-w-xs font-mono"
        />
        <p className="font-mono text-body-sm text-[color:var(--text-secondary)]">
          {rows.length} of {listings.data?.items.length ?? 0}
        </p>
        <div className="ml-auto flex gap-2">
          <Button variant="outline" size="sm">
            Bulk upload
          </Button>
          <Button size="sm">Add vehicle</Button>
        </div>
      </div>

      <DataTable
        columns={columns}
        rows={rows}
        getRowId={(row) => row.listingId}
        onRowClick={(row) => router.push(`/inventory/${row.listingId}`)}
        caption="Vehicles in stock at the active branch"
        stickyHeader
        className="max-h-[calc(100dvh-16rem)]"
        empty={
          <EmptyState
            title={query ? 'Nothing matches that filter' : 'No stock in this branch'}
            body={
              query
                ? 'Check the plate or try part of the model name.'
                : 'Add a vehicle, or use bulk upload to import your existing stock.'
            }
            align="center"
          />
        }
      />
    </>
  );
}

/**
 * The spec strip is registry-ordered and registry-labelled, so a column reads it by attribute code
 * rather than by position — inserting a new attribute upstream must not silently shift a column.
 */
function specValue(row: ListingSummary, code: string): string | undefined {
  return row.specStrip.find((item) => item.code === code)?.value;
}
