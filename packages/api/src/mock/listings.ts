/**
 * Mock listing detail — projected from the corpus, with spec groups derived from the taxonomy's
 * `displayGroup` and `sortOrder` rather than from a layout written in a component.
 */

import type { z } from 'zod';
import type { ListingDetailSchema, MediaAsset, SpecGroup } from '../schemas';
import { mockAttributes } from './registry';
import type { MockVehicle } from './vehicles';
import {
  MOCK_VEHICLES,
  coverImage,
  findVehicle,
  formatAttributeValue,
  toSearchItem,
} from './vehicles';
import { mockWorkflowSnapshot } from './workflows';

const GROUP_LABELS: Record<string, string> = {
  identity: 'Identity',
  engine: 'Engine and fuel',
  usage: 'Usage',
  body: 'Body',
  capability: 'Capability',
  documents: 'Documents',
};

const GROUP_ORDER = ['identity', 'engine', 'usage', 'body', 'capability', 'documents'];

function daysAgoIso(days: number): string {
  return new Date(Date.now() - days * 86_400_000).toISOString();
}

function buildSpecGroups(vehicle: MockVehicle): SpecGroup[] {
  const { attributes } = mockAttributes(vehicle.categoryCode);
  const buckets = new Map<string, SpecGroup>();

  for (const attribute of attributes) {
    const raw = vehicle.attributes[attribute.code];
    if (raw === undefined) continue;
    const groupCode = attribute.displayGroup ?? 'identity';
    const existing = buckets.get(groupCode);
    const group: SpecGroup = existing ?? {
      code: groupCode,
      label: GROUP_LABELS[groupCode] ?? groupCode,
      items: [],
    };
    group.items.push({
      code: attribute.code,
      label: attribute.label,
      value: formatAttributeValue(attribute.code, raw),
    });
    buckets.set(groupCode, group);
  }

  return [...buckets.values()].sort(
    (a, b) => GROUP_ORDER.indexOf(a.code) - GROUP_ORDER.indexOf(b.code),
  );
}

function buildGallery(vehicle: MockVehicle): MediaAsset[] {
  const cover = coverImage(vehicle);
  return Array.from({ length: Math.min(vehicle.photoCount, 8) }, (_, index) => ({
    id: `${vehicle.listingId}-media-${index + 1}`,
    url: cover,
    aspectRatio: 1.5,
    kind: 'photo',
    caption: null,
  }));
}

export function mockListingDetail(listingId: string): z.input<typeof ListingDetailSchema> | null {
  const vehicle = findVehicle(listingId);
  if (!vehicle) return null;

  const summary = toSearchItem(vehicle);

  return {
    ...summary,
    description: vehicle.description,
    currency: 'INR',
    gallery: buildGallery(vehicle),
    attributes: vehicle.attributes,
    specGroups: buildSpecGroups(vehicle),
    seller: {
      id: vehicle.sellerId,
      displayName: vehicle.sellerName,
      type: vehicle.sellerType,
      city: summary.city,
      isVerified: vehicle.sellerVerified,
      memberSince: daysAgoIso(vehicle.createdDaysAgo + 400),
      listingsCount: vehicle.sellerType === 'dealer' ? 34 : 1,
      responseTimeMinutes: vehicle.sellerType === 'dealer' ? 18 : 95,
    },
    workflow: mockWorkflowSnapshot('listing', vehicle.state),
    categoryCode: vehicle.categoryCode,
    makeCode: vehicle.makeCode,
    modelCode: vehicle.modelCode,
    variantCode: vehicle.variantCode ?? null,
    year: vehicle.year,
    locality: vehicle.locality,
    createdAt: daysAgoIso(vehicle.createdDaysAgo),
    expiresAt: daysAgoIso(vehicle.createdDaysAgo - 60),
    viewCount: vehicle.viewCount,
  };
}

/** The dealer portal's inventory list and the customer's "my listings". */
export function mockListingList(query: Record<string, string>): {
  items: ReturnType<typeof toSearchItem>[];
  nextCursor: string | null;
  totalEstimate: number;
} {
  const intent = query.intent;
  const items = MOCK_VEHICLES.filter((v) => (intent ? v.intent === intent : true)).filter((v) =>
    query.state ? v.state === query.state : true,
  );
  return {
    items: items.map(toSearchItem),
    nextCursor: null,
    totalEstimate: items.length,
  };
}
