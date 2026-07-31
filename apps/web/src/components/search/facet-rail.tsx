'use client';

import type { SearchFacet } from '@autohub/api';
import { Checkbox, Slider, cn, formatIndianNumber } from '@autohub/ui';
import type { SearchFilters } from './use-search-filters';

/**
 * The filter rail knows nothing about vehicles.
 *
 * Every facet — its code, its label, whether it renders as chips or a range — arrives from the
 * taxonomy registry inside the search response. Adding `battery_health_pct` as a filter is an
 * attribute row marked searchable; this file does not change. That is the whole point of the
 * facet contract, and it is why there is no list of fuel types anywhere in this app.
 */
export function FacetRail({
  facets,
  filters,
  onChange,
  onClear,
  activeCount,
}: {
  facets: SearchFacet[];
  filters: SearchFilters;
  onChange: (next: Partial<SearchFilters>) => void;
  onClear: () => void;
  activeCount: number;
}) {
  if (facets.length === 0) {
    return null;
  }

  return (
    <aside className="lg:sticky lg:top-24 lg:self-start" aria-label="Filters">
      <div className="flex items-center justify-between border-b border-[color:var(--border-hairline)] pb-3">
        <p className="ah-label">Filters</p>
        {activeCount > 0 ? (
          <button
            type="button"
            onClick={onClear}
            className="text-body-sm text-[color:var(--accent-primary)] underline underline-offset-4"
          >
            Clear {activeCount}
          </button>
        ) : null}
      </div>

      <div className="divide-y divide-[color:var(--border-hairline)]">
        {facets.map((facet) => (
          <section key={facet.code} className="py-5">
            <p className="ah-label">{facet.label}</p>

            {facet.style === 'range' ? (
              <RangeFacet facet={facet} filters={filters} onChange={onChange} />
            ) : (
              <OptionFacet facet={facet} filters={filters} onChange={onChange} />
            )}
          </section>
        ))}
      </div>
    </aside>
  );
}

function OptionFacet({
  facet,
  filters,
  onChange,
}: {
  facet: Extract<SearchFacet, { style: 'chips' | 'list' | 'toggle' }>;
  filters: SearchFilters;
  onChange: (next: Partial<SearchFilters>) => void;
}) {
  const selected = asStringArray(filters.attributes[facet.code]);

  function toggle(value: string) {
    const next = selected.includes(value)
      ? selected.filter((v) => v !== value)
      : [...selected, value];

    const attributes = { ...filters.attributes };
    if (next.length === 0) {
      delete attributes[facet.code];
    } else {
      attributes[facet.code] = next;
    }
    onChange({ attributes });
  }

  if (facet.style === 'chips') {
    return (
      <div className="mt-3 flex flex-wrap gap-2">
        {facet.options.map((option) => {
          const active = selected.includes(option.value);
          return (
            <button
              key={option.value}
              type="button"
              onClick={() => toggle(option.value)}
              aria-pressed={active}
              className={cn(
                'rounded-[var(--radius-control)] border px-2.5 py-1 text-body-sm transition-colors',
                active
                  ? 'border-[color:var(--accent-primary)] bg-[color:var(--accent-primary)] text-[color:var(--text-on-primary)]'
                  : 'border-[color:var(--border-hairline)] text-[color:var(--text-primary)] hover:border-[color:var(--border-strong)]',
              )}
            >
              {option.label}
              {typeof option.count === 'number' ? (
                <span className={cn('ml-1.5 font-mono text-[0.6875rem]', active ? 'opacity-80' : 'text-[color:var(--text-secondary)]')}>
                  {option.count}
                </span>
              ) : null}
            </button>
          );
        })}
      </div>
    );
  }

  return (
    <ul className="mt-3 space-y-2">
      {facet.options.map((option) => (
        <li key={option.value} className="flex items-center gap-2.5">
          <Checkbox
            id={`${facet.code}-${option.value}`}
            checked={selected.includes(option.value)}
            onCheckedChange={() => toggle(option.value)}
          />
          <label
            htmlFor={`${facet.code}-${option.value}`}
            className="flex flex-1 cursor-pointer items-baseline justify-between gap-2 text-body-sm"
          >
            <span>{option.label}</span>
            {typeof option.count === 'number' ? (
              <span className="font-mono text-[0.6875rem] text-[color:var(--text-secondary)]">
                {option.count}
              </span>
            ) : null}
          </label>
        </li>
      ))}
    </ul>
  );
}

function RangeFacet({
  facet,
  filters,
  onChange,
}: {
  facet: Extract<SearchFacet, { style: 'range' }>;
  filters: SearchFilters;
  onChange: (next: Partial<SearchFilters>) => void;
}) {
  const current = filters.attributes[facet.code];
  const range =
    current && typeof current === 'object' && !Array.isArray(current)
      ? current
      : { min: facet.min, max: facet.max };

  const value = [range.min ?? facet.min, range.max ?? facet.max] as [number, number];

  return (
    <div className="mt-4">
      <Slider
        min={facet.min}
        max={facet.max}
        step={facet.step ?? Math.max(1, Math.round((facet.max - facet.min) / 100))}
        value={value}
        onValueChange={([min, max]) => {
          const attributes = { ...filters.attributes };
          if (min === facet.min && max === facet.max) {
            delete attributes[facet.code];
          } else {
            attributes[facet.code] = { min, max };
          }
          onChange({ attributes });
        }}
        aria-label={facet.label}
      />
      <p className="mt-2 font-mono text-[0.75rem] text-[color:var(--text-secondary)]">
        {formatIndianNumber(value[0])} – {formatIndianNumber(value[1])}
        {facet.unit ? ` ${facet.unit}` : ''}
      </p>
    </div>
  );
}

// `noUncheckedIndexedAccess` is on, so a record lookup is legitimately `T | undefined` — a facet
// the user has not touched has no entry at all. Nothing selected is the honest answer.
function asStringArray(value: SearchFilters['attributes'][string] | undefined): string[] {
  return Array.isArray(value) ? value : [];
}
