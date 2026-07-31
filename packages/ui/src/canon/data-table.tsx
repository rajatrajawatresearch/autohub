import type { KeyboardEvent, ReactNode } from 'react';
import { cn } from '../lib/cn';

/**
 * The dense hairline table every operational surface is built from — dealer inventory, admin
 * moderation queues, ledger lines, bid histories.
 *
 * Columns are a plain typed contract rather than a table library's, so a column set can be
 * assembled from the column registry at runtime and this package stays free of a data-grid
 * dependency. The apps own the head-side concerns — sorting, pagination, TanStack Table if they
 * want it — and hand the resolved rows down.
 *
 * Row height, cell padding and gap all come from the density tokens, so `<html data-density="compact">`
 * turns a customer table into a dealer table without touching a component.
 */

export interface DataTableColumn<TRow> {
  id: string;
  header: ReactNode;
  cell: (row: TRow, rowIndex: number) => ReactNode;
  align?: 'start' | 'end';
  /** Any CSS width — "12rem", "1%", "40ch". */
  width?: string;
  /** Ids, plates, money, kms, timestamps — anything read, compared or transcribed. */
  mono?: boolean;
  /** Drops the column below a breakpoint so a wide table stays readable on a phone. */
  hideBelow?: 'sm' | 'md' | 'lg';
}

export interface DataTableProps<TRow> {
  columns: Array<DataTableColumn<TRow>>;
  rows: TRow[];
  getRowId?: (row: TRow, index: number) => string;
  /** Passing this makes rows keyboard-activatable; it requires a client component. */
  onRowClick?: (row: TRow, index: number) => void;
  isRowSelected?: (row: TRow, index: number) => boolean;
  /** Rendered in place of the body when there are no rows — an EmptyState belongs here. */
  empty?: ReactNode;
  /** Screen-reader description of what the table holds. */
  caption?: string;
  /** Pins the header row. Needs a bounded height on the wrapper — pass `max-h-*` in `className`. */
  stickyHeader?: boolean;
  className?: string;
}

const HIDE_BELOW = {
  sm: 'hidden sm:table-cell',
  md: 'hidden md:table-cell',
  lg: 'hidden lg:table-cell',
} as const;

export function DataTable<TRow>({
  columns,
  rows,
  getRowId,
  onRowClick,
  isRowSelected,
  empty,
  caption,
  stickyHeader = false,
  className,
}: DataTableProps<TRow>) {
  const clickable = Boolean(onRowClick);

  function handleKeyDown(event: KeyboardEvent<HTMLTableRowElement>, row: TRow, index: number) {
    if (!onRowClick || (event.key !== 'Enter' && event.key !== ' ')) return;
    event.preventDefault();
    onRowClick(row, index);
  }

  if (rows.length === 0 && empty) {
    return <div className={className}>{empty}</div>;
  }

  return (
    <div
      className={cn(
        'w-full overflow-x-auto rounded-card border border-[color:var(--border-hairline)] bg-[var(--surface-card)]',
        className,
      )}
    >
      <table className="w-full border-collapse text-left">
        {caption ? <caption className="sr-only">{caption}</caption> : null}

        <thead>
          <tr className="border-b border-[color:var(--border-hairline)]">
            {columns.map((column) => (
              <th
                key={column.id}
                scope="col"
                style={column.width ? { width: column.width } : undefined}
                className={cn(
                  'ah-label whitespace-nowrap bg-[var(--surface-card)] px-[var(--density-pad-x)] py-[var(--density-pad-y)] align-middle',
                  column.align === 'end' ? 'text-right' : 'text-left',
                  column.hideBelow && HIDE_BELOW[column.hideBelow],
                  stickyHeader && 'sticky top-0 z-10 border-b border-[color:var(--border-hairline)]',
                )}
              >
                {column.header}
              </th>
            ))}
          </tr>
        </thead>

        <tbody>
          {rows.map((row, rowIndex) => {
            const selected = isRowSelected?.(row, rowIndex) ?? false;

            return (
              <tr
                key={getRowId?.(row, rowIndex) ?? rowIndex}
                tabIndex={clickable ? 0 : undefined}
                onClick={onRowClick ? () => onRowClick(row, rowIndex) : undefined}
                onKeyDown={clickable ? (event) => handleKeyDown(event, row, rowIndex) : undefined}
                aria-selected={isRowSelected ? selected : undefined}
                className={cn(
                  'h-[var(--density-row-h)] border-b border-[color:var(--border-hairline)] last:border-b-0',
                  clickable && 'cursor-pointer hover:bg-[var(--surface-sunken)]',
                  selected && 'bg-[var(--surface-sunken)]',
                )}
              >
                {columns.map((column) => (
                  <td
                    key={column.id}
                    className={cn(
                      'px-[var(--density-pad-x)] py-[var(--density-pad-y)] align-middle text-[length:var(--text-body-sm)] leading-[1.45] text-[color:var(--text-primary)]',
                      column.align === 'end' ? 'text-right' : 'text-left',
                      column.mono && 'font-mono text-[length:var(--text-data)] font-medium',
                      column.hideBelow && HIDE_BELOW[column.hideBelow],
                    )}
                  >
                    {column.cell(row, rowIndex)}
                  </td>
                ))}
              </tr>
            );
          })}
        </tbody>
      </table>
    </div>
  );
}
