/**
 * Mock valuation.
 *
 * A plate in the corpus values against its own asking price. A plate the fixtures have never seen
 * still returns a number — derived deterministically from the registration so the same plate always
 * gives the same answer, which is what makes a demo repeatable.
 */

import type { z } from 'zod';
import type { ValuationQuoteSchema } from '../schemas';
import { cityName, displayPlate, findVehicleByPlate } from './vehicles';

function hash(input: string): number {
  let h = 2166136261;
  for (let i = 0; i < input.length; i += 1) {
    h ^= input.charCodeAt(i);
    h = Math.imul(h, 16777619);
  }
  return Math.abs(h);
}

/** Round to the nearest ₹1,000 — a valuation quoted to the rupee is a valuation nobody believes. */
function roundToThousand(minor: number): number {
  return Math.round(minor / 100000) * 100000;
}

const FALLBACK_MODELS = [
  { title: 'Maruti Suzuki Swift VXi', baseMinor: 52000000, fuel: 'Petrol' },
  { title: 'Hyundai i20 Sportz', baseMinor: 61000000, fuel: 'Petrol' },
  { title: 'Tata Nexon XZ+', baseMinor: 89000000, fuel: 'Petrol' },
  { title: 'Mahindra Bolero B6', baseMinor: 78000000, fuel: 'Diesel' },
  { title: 'Honda City SV', baseMinor: 67000000, fuel: 'Petrol' },
];

export function mockValuationQuote(body: {
  registrationNumber?: string;
  kmsDriven?: number;
  city?: string;
  condition?: string;
}): z.input<typeof ValuationQuoteSchema> {
  const canonical = (body.registrationNumber ?? '').replace(/[^A-Za-z0-9]/g, '').toUpperCase();
  const vehicle = findVehicleByPlate(canonical);
  const seed = hash(canonical || 'MH12AB1234');

  const fallback = FALLBACK_MODELS[seed % FALLBACK_MODELS.length] ?? FALLBACK_MODELS[0];
  const conditionFactor =
    body.condition === 'excellent'
      ? 1.06
      : body.condition === 'fair'
        ? 0.92
        : body.condition === 'poor'
          ? 0.82
          : 1;

  const base = vehicle
    ? vehicle.priceMinor
    : ((fallback?.baseMinor ?? 52000000) * (90 + (seed % 25))) / 100;

  /* Every 10,000 km beyond 40,000 takes about 1.5% off. Crude, and honest about being crude. */
  const kmsFactor =
    body.kmsDriven === undefined
      ? 1
      : Math.min(1.08, Math.max(0.8, 1 - ((body.kmsDriven - 40000) / 10000) * 0.015));

  const market = roundToThousand(base * conditionFactor * kmsFactor);
  const year = vehicle?.year ?? 2018 + (seed % 7);
  const title = vehicle?.title ?? `${year} ${fallback?.title ?? 'Maruti Suzuki Swift VXi'}`;
  const fuelCode = vehicle ? String(vehicle.attributes.fuel_type ?? 'petrol') : null;
  const fuel = fuelCode
    ? fuelCode === 'ev'
      ? 'Electric'
      : fuelCode.toUpperCase() === 'CNG'
        ? 'CNG'
        : fuelCode.charAt(0).toUpperCase() + fuelCode.slice(1)
    : (fallback?.fuel ?? 'Petrol');

  const cityCode = body.city ?? vehicle?.cityCode ?? 'pune';
  const comparables = 180 + (seed % 400);

  return {
    quoteId: `019fabbb-${(seed % 9999).toString().padStart(4, '0')}-7000-8000-000000000001`,
    vehicle: {
      title,
      registration: {
        canonical: canonical || 'MH12AB1234',
        display: displayPlate(canonical || 'MH12AB1234'),
        plateClass: vehicle?.plateClass ?? 'private',
        stateCode: (canonical || 'MH12AB1234').slice(0, 2),
      },
      year,
      fuel,
      transmission: vehicle ? String(vehicle.attributes.transmission ?? 'Manual') : 'Manual',
      source: vehicle ? 'vahan' : 'estimate',
    },
    currency: 'INR',
    valuations: [
      { code: 'market', label: 'Market value', amountMinor: market, isPrimary: true },
      {
        code: 'trade',
        label: 'Trade value',
        amountMinor: roundToThousand(market * 0.9),
        helpText: 'What a dealer pays when they take it into stock.',
      },
      { code: 'dealer', label: 'Dealer offer', amountMinor: roundToThousand(market * 0.93) },
      { code: 'auction', label: 'Auction estimate', amountMinor: roundToThousand(market * 0.91) },
      {
        code: 'quick_sale',
        label: 'Quick sale',
        amountMinor: roundToThousand(market * 0.87),
        helpText: 'Sold within 48 hours, money the same day.',
      },
    ],
    range: {
      lowMinor: roundToThousand(market * 0.87),
      highMinor: roundToThousand(market * 1.06),
    },
    confidence: {
      score: vehicle ? 0.86 : 0.68,
      band: vehicle ? 'high' : 'medium',
      basedOn: `${comparables} comparable sales in ${cityName(cityCode)} over 90 days`,
    },
    asOf: new Date().toISOString().slice(0, 10),
    expiresAt: new Date(Date.now() + 7 * 86_400_000).toISOString(),
    modelVersion: 'valuation-ensemble@2026.07.3',
  };
}
