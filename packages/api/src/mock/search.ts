/**
 * Mock search — filters, sorts and facets computed over the fixture corpus.
 *
 * The facets are *derived*, not written down: the taxonomy registry says which attributes are
 * searchable and how they are styled, and the counts come from the data. That is the same
 * derivation the real search service performs, which is what makes the filter rail truthful in a
 * demo rather than decorative.
 */

import type { SearchFacet, SearchResponse } from '../schemas';
import { mockAttributes } from './registry';
import type { MockVehicle } from './vehicles';
import { MOCK_VEHICLES, cityName, toSearchItem } from './vehicles';

function parseRange(raw: string | undefined): { min?: number; max?: number } | null {
  if (!raw) return null;
  const [lo, hi] = raw.split('..');
  const min = lo === undefined || lo === '' ? undefined : Number(lo);
  const max = hi === undefined || hi === '' ? undefined : Number(hi);
  if (min === undefined && max === undefined) return null;
  return { min, max };
}

function withinRange(value: number, range: { min?: number; max?: number }): boolean {
  if (range.min !== undefined && value < range.min) return false;
  if (range.max !== undefined && value > range.max) return false;
  return true;
}

function matchesAttribute(vehicle: MockVehicle, code: string, raw: string): boolean {
  const value = vehicle.attributes[code];
  if (value === undefined) return false;

  if (raw.includes('..')) {
    const range = parseRange(raw);
    if (!range) return true;
    return typeof value === 'number' && withinRange(value, range);
  }

  const accepted = raw.split(',').filter(Boolean);
  return accepted.includes(String(value));
}

function sortVehicles(vehicles: MockVehicle[], sort: string | undefined): MockVehicle[] {
  const sorted = [...vehicles];
  switch (sort) {
    case 'price_asc':
      return sorted.sort((a, b) => a.priceMinor - b.priceMinor);
    case 'price_desc':
      return sorted.sort((a, b) => b.priceMinor - a.priceMinor);
    case 'newest':
      return sorted.sort((a, b) => a.createdDaysAgo - b.createdDaysAgo);
    case 'kms_asc':
      return sorted.sort(
        (a, b) => Number(a.attributes.kms_driven ?? 0) - Number(b.attributes.kms_driven ?? 0),
      );
    default:
      /* Relevance in the fixtures: freshest first, verified sellers ahead of unverified. */
      return sorted.sort(
        (a, b) =>
          Number(b.sellerVerified) - Number(a.sellerVerified) || a.createdDaysAgo - b.createdDaysAgo,
      );
  }
}

function buildFacets(pool: MockVehicle[], categoryHint: string | null): SearchFacet[] {
  const { attributes } = mockAttributes(categoryHint);
  const facets: SearchFacet[] = [];

  for (const attribute of attributes) {
    if (!attribute.searchable) continue;
    const style = attribute.facetStyle ?? 'list';

    if (style === 'range') {
      const values = pool
        .map((v) => v.attributes[attribute.code])
        .filter((v): v is number => typeof v === 'number');
      if (values.length === 0) continue;
      facets.push({
        code: attribute.code,
        label: attribute.label,
        style: 'range',
        min: Math.min(...values),
        max: Math.max(...values),
        step: attribute.range?.step,
        unit: attribute.unit ?? null,
      });
      continue;
    }

    const counts = new Map<string, number>();
    for (const vehicle of pool) {
      const value = vehicle.attributes[attribute.code];
      if (value === undefined) continue;
      const key = String(value);
      counts.set(key, (counts.get(key) ?? 0) + 1);
    }
    if (counts.size === 0) continue;

    const options = (attribute.options ?? [])
      .filter((option) => counts.has(option.value))
      .map((option) => ({
        value: option.value,
        label: option.label,
        sortOrder: option.sortOrder,
        count: counts.get(option.value) ?? 0,
      }));
    if (options.length === 0) continue;

    facets.push({
      code: attribute.code,
      label: attribute.label,
      style: style === 'chips' ? 'chips' : style === 'toggle' ? 'toggle' : 'list',
      options,
    });
  }

  return facets;
}

const DEFAULT_LIMIT = 24;

export function mockSearch(query: Record<string, string>): SearchResponse {
  const q = query.q?.trim().toLowerCase();
  const city = query.city;
  const intent = query.intent ?? 'sale';
  const priceRange = parseRange(query.price);

  /* Everything except the attribute filters — the pool facet counts are computed over. */
  const pool = MOCK_VEHICLES.filter((vehicle) => {
    if (vehicle.intent !== intent) return false;
    if (city && vehicle.cityCode !== city) return false;
    if (q) {
      const haystack =
        `${vehicle.title} ${vehicle.plate} ${vehicle.makeCode} ${vehicle.modelCode} ${cityName(vehicle.cityCode)}`.toLowerCase();
      if (!haystack.includes(q)) return false;
    }
    return true;
  });

  const attributeFilters = Object.entries(query)
    .filter(([key]) => key.startsWith('attr.'))
    .map(([key, value]) => [key.slice('attr.'.length), value] as const);

  const filtered = pool.filter((vehicle) => {
    if (priceRange && !withinRange(vehicle.priceMinor, priceRange)) return false;
    for (const [code, raw] of attributeFilters) {
      if (code === 'category') {
        if (!raw.split(',').includes(vehicle.categoryCode)) return false;
        continue;
      }
      if (!matchesAttribute(vehicle, code, raw)) return false;
    }
    return true;
  });

  const sorted = sortVehicles(filtered, query.sort);
  const limit = Number.parseInt(query.limit ?? '', 10) || DEFAULT_LIMIT;
  const offset = Number.parseInt((query.cursor ?? '').replace(/^c/, ''), 10) || 0;
  const page = sorted.slice(offset, offset + limit);
  const nextOffset = offset + page.length;

  const categoryHint =
    attributeFilters.find(([code]) => code === 'category')?.[1]?.split(',')[0] ??
    page[0]?.categoryCode ??
    null;

  return {
    items: page.map(toSearchItem),
    facets: buildFacets(pool, categoryHint),
    nextCursor: nextOffset < sorted.length ? `c${nextOffset}` : null,
    totalEstimate: sorted.length,
  };
}
