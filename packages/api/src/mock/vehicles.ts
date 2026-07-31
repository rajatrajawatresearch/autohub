/**
 * The vehicle corpus every mock endpoint reads from — search, listing detail, valuation, auction
 * and deals all project from these fifteen vehicles, so the demo stays internally consistent: the
 * Swift you value is the Swift you find in search and the Swift a dealer bids on.
 *
 * Real Indian plates, real model names, real price points, real cities. Prices are integer paise.
 */

import type { SearchItem, SpecItem } from '../schemas';
import { ALL_ATTRIBUTES, MOCK_CITIES, mockAttributes } from './registry';

export interface MockVehicle {
  listingId: string;
  intent: 'sale' | 'auction' | 'rental' | 'lease' | 'attachment' | 'exchange';
  plate: string;
  plateClass: 'private' | 'commercial' | 'ev';
  title: string;
  /** Integer paise. `54900000` is ₹5,49,000. */
  priceMinor: number;
  /** Set for rental and lease: `day`, `month`. */
  pricePeriod?: string;
  cityCode: string;
  locality: string;
  categoryCode: string;
  makeCode: string;
  modelCode: string;
  variantCode?: string;
  year: number;
  attributes: Record<string, string | number | boolean>;
  badges: string[];
  photoCount: number;
  description: string;
  sellerId: string;
  sellerName: string;
  sellerType: 'individual' | 'dealer';
  sellerVerified: boolean;
  /** Workflow state code from the `listing` workflow. */
  state: string;
  createdDaysAgo: number;
  viewCount: number;
}

export const MOCK_VEHICLES: MockVehicle[] = [
  {
    listingId: '019fab0c-0001-7000-8000-000000000001',
    intent: 'sale',
    plate: 'MH12AB1234',
    plateClass: 'private',
    title: '2021 Maruti Suzuki Swift VXi',
    priceMinor: 54900000,
    cityCode: 'pune',
    locality: 'Baner',
    categoryCode: 'hatchback',
    makeCode: 'maruti-suzuki',
    modelCode: 'swift',
    variantCode: 'swift-vxi',
    year: 2021,
    attributes: {
      year: 2021,
      fuel_type: 'petrol',
      transmission: 'manual',
      owners: '1',
      kms_driven: 38400,
      seats: 5,
      insurance_validity: '2026-11-14',
      rc_status: 'original',
    },
    badges: ['inspected', 'verified_rc', 'accident_free'],
    photoCount: 14,
    description:
      'Single owner, serviced at the Maruti dealership every 10,000 km with the book to show for it. Tyres replaced at 34,000 km. No accident history.',
    sellerId: '019fab0c-9001-7000-8000-000000000001',
    sellerName: 'Rohit Kulkarni',
    sellerType: 'individual',
    sellerVerified: true,
    state: 'live',
    createdDaysAgo: 6,
    viewCount: 412,
  },
  {
    listingId: '019fab0c-0002-7000-8000-000000000002',
    intent: 'sale',
    plate: 'KA05MN7788',
    plateClass: 'private',
    title: '2020 Hyundai Creta SX 1.5 Diesel',
    priceMinor: 128500000,
    cityCode: 'bengaluru',
    locality: 'Indiranagar',
    categoryCode: 'suv',
    makeCode: 'hyundai',
    modelCode: 'creta',
    variantCode: 'creta-sx',
    year: 2020,
    attributes: {
      year: 2020,
      fuel_type: 'diesel',
      transmission: 'manual',
      owners: '2',
      kms_driven: 52100,
      seats: 5,
      insurance_validity: '2026-03-02',
      rc_status: 'original',
    },
    badges: ['inspected', 'verified_rc', 'finance_available'],
    photoCount: 21,
    description:
      'Second owner, bought from the first owner in 2023. Full service history at Advaith Hyundai. Sunroof, ventilated seats, all four tyres at 70%.',
    sellerId: '019fab0c-9002-7000-8000-000000000002',
    sellerName: 'Whitefield Auto Mart',
    sellerType: 'dealer',
    sellerVerified: true,
    state: 'live',
    createdDaysAgo: 12,
    viewCount: 1284,
  },
  {
    listingId: '019fab0c-0003-7000-8000-000000000003',
    intent: 'auction',
    plate: 'DL8CAF5031',
    plateClass: 'private',
    title: '2019 Mahindra Scorpio S11',
    priceMinor: 112000000,
    cityCode: 'delhi',
    locality: 'Okhla Phase II',
    categoryCode: 'suv',
    makeCode: 'mahindra',
    modelCode: 'scorpio',
    variantCode: 'scorpio-s11',
    year: 2019,
    attributes: {
      year: 2019,
      fuel_type: 'diesel',
      transmission: 'manual',
      owners: '1',
      kms_driven: 78300,
      seats: 7,
      insurance_validity: '2026-08-19',
      rc_status: 'original',
    },
    badges: ['inspected', 'verified_rc'],
    photoCount: 18,
    description:
      'Fleet-maintained, all records digitised. Clutch replaced at 71,000 km. Sold as-is at auction; inspection report attached to the lot.',
    sellerId: '019fab0c-9003-7000-8000-000000000003',
    sellerName: 'Capital Fleet Services',
    sellerType: 'dealer',
    sellerVerified: true,
    state: 'live',
    createdDaysAgo: 2,
    viewCount: 233,
  },
  {
    listingId: '019fab0c-0004-7000-8000-000000000004',
    intent: 'sale',
    plate: 'MH14QR2091',
    plateClass: 'ev',
    title: '2023 Tata Nexon EV Max XZ+',
    priceMinor: 154000000,
    cityCode: 'pune',
    locality: 'Wakad',
    categoryCode: 'suv',
    makeCode: 'tata',
    modelCode: 'nexon-ev',
    variantCode: 'nexon-ev-xz-plus',
    year: 2023,
    attributes: {
      year: 2023,
      fuel_type: 'ev',
      transmission: 'automatic',
      owners: '1',
      kms_driven: 21800,
      seats: 5,
      battery_health_pct: 93,
      insurance_validity: '2027-01-08',
      rc_status: 'original',
    },
    badges: ['inspected', 'verified_rc', 'single_owner'],
    photoCount: 16,
    description:
      'Under Tata battery warranty until 2031. Home charger included. Battery state of health measured at 93% during inspection.',
    sellerId: '019fab0c-9004-7000-8000-000000000004',
    sellerName: 'Anjali Deshpande',
    sellerType: 'individual',
    sellerVerified: true,
    state: 'live',
    createdDaysAgo: 3,
    viewCount: 806,
  },
  {
    listingId: '019fab0c-0005-7000-8000-000000000005',
    intent: 'auction',
    plate: 'MH04CD9911',
    plateClass: 'commercial',
    title: '2018 Ashok Leyland Dost+ 1.5 T',
    priceMinor: 42500000,
    cityCode: 'mumbai',
    locality: 'Bhiwandi',
    categoryCode: 'pickup',
    makeCode: 'ashok-leyland',
    modelCode: 'dost',
    year: 2018,
    attributes: {
      year: 2018,
      fuel_type: 'diesel',
      owners: '2',
      kms_driven: 121400,
      payload_kg: 1250,
      axle_config: '4x2',
      permit_type: 'goods',
      insurance_validity: '2026-05-30',
      rc_status: 'original',
    },
    badges: ['verified_rc'],
    photoCount: 11,
    description:
      'Worked a fixed Bhiwandi–Nashik route. Engine overhauled at 1,10,000 km. Permit and fitness valid; papers with the lot.',
    sellerId: '019fab0c-9005-7000-8000-000000000005',
    sellerName: 'Konkan Roadlines',
    sellerType: 'dealer',
    sellerVerified: true,
    state: 'live',
    createdDaysAgo: 1,
    viewCount: 147,
  },
  {
    listingId: '019fab0c-0006-7000-8000-000000000006',
    intent: 'sale',
    plate: 'KA51HJ4402',
    plateClass: 'private',
    title: '2022 Maruti Suzuki Baleno Zeta',
    priceMinor: 79500000,
    cityCode: 'bengaluru',
    locality: 'HSR Layout',
    categoryCode: 'hatchback',
    makeCode: 'maruti-suzuki',
    modelCode: 'baleno',
    year: 2022,
    attributes: {
      year: 2022,
      fuel_type: 'petrol',
      transmission: 'manual',
      owners: '1',
      kms_driven: 24600,
      seats: 5,
      insurance_validity: '2026-09-21',
      rc_status: 'original',
    },
    badges: ['inspected', 'verified_rc', 'single_owner'],
    photoCount: 19,
    description:
      'Company-maintained, driven only within Bengaluru. Head-up display, 360 camera. Original tyres with 60% left.',
    sellerId: '019fab0c-9002-7000-8000-000000000002',
    sellerName: 'Whitefield Auto Mart',
    sellerType: 'dealer',
    sellerVerified: true,
    state: 'live',
    createdDaysAgo: 9,
    viewCount: 655,
  },
  {
    listingId: '019fab0c-0007-7000-8000-000000000007',
    intent: 'sale',
    plate: 'DL3CBA7745',
    plateClass: 'private',
    title: '2017 Honda City VX CVT',
    priceMinor: 68000000,
    cityCode: 'delhi',
    locality: 'Rohini',
    categoryCode: 'sedan',
    makeCode: 'honda',
    modelCode: 'city',
    year: 2017,
    attributes: {
      year: 2017,
      fuel_type: 'petrol',
      transmission: 'automatic',
      owners: '2',
      kms_driven: 89200,
      seats: 5,
      insurance_validity: '2026-02-11',
      rc_status: 'original',
    },
    badges: ['verified_rc', 'finance_available'],
    photoCount: 12,
    description:
      'CVT gearbox serviced at 80,000 km. Sunroof, leather seats fitted in 2019. Minor scratch on the rear bumper, photographed.',
    sellerId: '019fab0c-9007-7000-8000-000000000007',
    sellerName: 'Vikram Sethi',
    sellerType: 'individual',
    sellerVerified: false,
    state: 'live',
    createdDaysAgo: 21,
    viewCount: 391,
  },
  {
    listingId: '019fab0c-0008-7000-8000-000000000008',
    intent: 'sale',
    plate: 'MH12KL8834',
    plateClass: 'private',
    title: '2021 Kia Seltos HTX 1.5',
    priceMinor: 132500000,
    cityCode: 'pune',
    locality: 'Kharadi',
    categoryCode: 'suv',
    makeCode: 'kia',
    modelCode: 'seltos',
    year: 2021,
    attributes: {
      year: 2021,
      fuel_type: 'petrol',
      transmission: 'manual',
      owners: '1',
      kms_driven: 41800,
      seats: 5,
      insurance_validity: '2026-12-04',
      rc_status: 'hypothecated',
    },
    badges: ['inspected', 'verified_rc'],
    photoCount: 17,
    description:
      'Loan with HDFC closing this month; NOC applied for. Bose audio, ventilated seats. Never used outside Pune.',
    sellerId: '019fab0c-9008-7000-8000-000000000008',
    sellerName: 'Sameer Joshi',
    sellerType: 'individual',
    sellerVerified: true,
    state: 'live',
    createdDaysAgo: 15,
    viewCount: 528,
  },
  {
    listingId: '019fab0c-0009-7000-8000-000000000009',
    intent: 'auction',
    plate: 'TS09EF3321',
    plateClass: 'commercial',
    title: '2020 Toyota Innova Crysta 2.4 ZX',
    priceMinor: 196000000,
    cityCode: 'hyderabad',
    locality: 'Gachibowli',
    categoryCode: 'muv',
    makeCode: 'toyota',
    modelCode: 'innova-crysta',
    year: 2020,
    attributes: {
      year: 2020,
      fuel_type: 'diesel',
      transmission: 'automatic',
      owners: '1',
      kms_driven: 96400,
      seats: 7,
      permit_type: 'contract_carriage',
      insurance_validity: '2026-06-27',
      rc_status: 'original',
    },
    badges: ['inspected', 'verified_rc'],
    photoCount: 15,
    description:
      'Corporate taxi fleet exit. Serviced only at Toyota. Contract carriage permit valid to 2027. Interior professionally refurbished before listing.',
    sellerId: '019fab0c-9009-7000-8000-000000000009',
    sellerName: 'Deccan Mobility',
    sellerType: 'dealer',
    sellerVerified: true,
    state: 'live',
    createdDaysAgo: 1,
    viewCount: 178,
  },
  {
    listingId: '019fab0c-0010-7000-8000-000000000010',
    intent: 'sale',
    plate: 'GJ01PQ6677',
    plateClass: 'private',
    title: '2024 Tata Punch Accomplished',
    priceMinor: 84500000,
    cityCode: 'ahmedabad',
    locality: 'Satellite',
    categoryCode: 'suv',
    makeCode: 'tata',
    modelCode: 'punch',
    year: 2024,
    attributes: {
      year: 2024,
      fuel_type: 'petrol',
      transmission: 'amt',
      owners: '1',
      kms_driven: 12300,
      seats: 5,
      insurance_validity: '2027-03-30',
      rc_status: 'original',
    },
    badges: ['inspected', 'verified_rc', 'single_owner'],
    photoCount: 20,
    description:
      'Fourteen months old, still under manufacturer warranty until March 2027. Extended warranty transferable to the buyer.',
    sellerId: '019fab0c-9010-7000-8000-000000000010',
    sellerName: 'Meera Patel',
    sellerType: 'individual',
    sellerVerified: true,
    state: 'live',
    createdDaysAgo: 4,
    viewCount: 297,
  },
  {
    listingId: '019fab0c-0011-7000-8000-000000000011',
    intent: 'sale',
    plate: 'MH12TR5566',
    plateClass: 'private',
    title: '2021 Royal Enfield Classic 350',
    priceMinor: 14200000,
    cityCode: 'pune',
    locality: 'Koregaon Park',
    categoryCode: 'motorcycle',
    makeCode: 'royal-enfield',
    modelCode: 'classic-350',
    year: 2021,
    attributes: {
      year: 2021,
      fuel_type: 'petrol',
      owners: '1',
      kms_driven: 18700,
      engine_cc: 349,
      insurance_validity: '2026-07-18',
      rc_status: 'original',
    },
    badges: ['verified_rc', 'single_owner'],
    photoCount: 9,
    description:
      'Halcyon Green, stock apart from a Zana crash guard. Serviced at Royal Enfield Kalyani Nagar. Chain and sprocket replaced last month.',
    sellerId: '019fab0c-9011-7000-8000-000000000011',
    sellerName: 'Aditya Rane',
    sellerType: 'individual',
    sellerVerified: true,
    state: 'live',
    createdDaysAgo: 8,
    viewCount: 214,
  },
  {
    listingId: '019fab0c-0012-7000-8000-000000000012',
    intent: 'sale',
    plate: 'MH12WZ4410',
    plateClass: 'commercial',
    title: '2019 JCB 3DX Backhoe Loader',
    priceMinor: 187500000,
    cityCode: 'pune',
    locality: 'Chakan MIDC',
    categoryCode: 'backhoe',
    makeCode: 'jcb',
    modelCode: '3dx',
    year: 2019,
    attributes: {
      year: 2019,
      fuel_type: 'diesel',
      operating_hours: 6400,
      boom_length_m: 5.9,
      lifting_capacity_kg: 3200,
      rc_status: 'original',
    },
    badges: ['verified_rc'],
    photoCount: 10,
    description:
      'Site machine from a Chakan infrastructure contractor. Hydraulics rebuilt at 5,800 hours. Bucket and breaker attachments included.',
    sellerId: '019fab0c-9012-7000-8000-000000000012',
    sellerName: 'Sahyadri Infra Equipment',
    sellerType: 'dealer',
    sellerVerified: true,
    state: 'live',
    createdDaysAgo: 17,
    viewCount: 96,
  },
  {
    listingId: '019fab0c-0013-7000-8000-000000000013',
    intent: 'sale',
    plate: 'MH12GH3399',
    plateClass: 'private',
    title: '2019 Maruti Suzuki Ertiga VXi CNG',
    priceMinor: 71000000,
    cityCode: 'pune',
    locality: 'Hadapsar',
    categoryCode: 'muv',
    makeCode: 'maruti-suzuki',
    modelCode: 'ertiga',
    year: 2019,
    attributes: {
      year: 2019,
      fuel_type: 'cng',
      transmission: 'manual',
      owners: '2',
      kms_driven: 68900,
      seats: 7,
      insurance_validity: '2026-04-09',
      rc_status: 'original',
    },
    badges: ['inspected', 'verified_rc'],
    photoCount: 13,
    description:
      'Factory-fitted CNG, endorsed on the RC. Runs 26 km/kg on the highway. Used for family travel, never commercially.',
    sellerId: '019fab0c-9013-7000-8000-000000000013',
    sellerName: 'Prakash Bhosale',
    sellerType: 'individual',
    sellerVerified: false,
    state: 'live',
    createdDaysAgo: 26,
    viewCount: 344,
  },
  {
    listingId: '019fab0c-0014-7000-8000-000000000014',
    intent: 'sale',
    plate: 'KA03NB2255',
    plateClass: 'private',
    title: '2018 Hyundai i20 Asta',
    priceMinor: 53500000,
    cityCode: 'bengaluru',
    locality: 'Jayanagar',
    categoryCode: 'hatchback',
    makeCode: 'hyundai',
    modelCode: 'i20',
    year: 2018,
    attributes: {
      year: 2018,
      fuel_type: 'petrol',
      transmission: 'manual',
      owners: '1',
      kms_driven: 61200,
      seats: 5,
      insurance_validity: '2026-10-02',
      rc_status: 'original',
    },
    badges: ['verified_rc', 'single_owner'],
    photoCount: 11,
    description:
      'One owner from new. Clutch and battery replaced in 2025. Air-conditioning serviced before listing.',
    sellerId: '019fab0c-9014-7000-8000-000000000014',
    sellerName: 'Lakshmi Narayan',
    sellerType: 'individual',
    sellerVerified: true,
    state: 'live',
    createdDaysAgo: 31,
    viewCount: 468,
  },
  {
    listingId: '019fab0c-0015-7000-8000-000000000015',
    intent: 'rental',
    plate: 'MH12JK7712',
    plateClass: 'commercial',
    title: '2023 Mahindra XUV700 AX7',
    priceMinor: 450000,
    pricePeriod: 'day',
    cityCode: 'pune',
    locality: 'Viman Nagar',
    categoryCode: 'suv',
    makeCode: 'mahindra',
    modelCode: 'xuv700',
    year: 2023,
    attributes: {
      year: 2023,
      fuel_type: 'diesel',
      transmission: 'automatic',
      owners: '1',
      kms_driven: 34500,
      seats: 7,
      permit_type: 'contract_carriage',
      insurance_validity: '2026-12-31',
      rc_status: 'original',
    },
    badges: ['inspected', 'verified_rc'],
    photoCount: 12,
    description:
      'Self-drive rental. ₹4,500 per day including 250 km; ₹14 per extra km. Security deposit ₹10,000, refunded on return.',
    sellerId: '019fab0c-9015-7000-8000-000000000015',
    sellerName: 'Pune Drive Rentals',
    sellerType: 'dealer',
    sellerVerified: true,
    state: 'live',
    createdDaysAgo: 5,
    viewCount: 129,
  },
];

/* ── Projections ─────────────────────────────────────────────────────────── */

const CITY_NAMES: Record<string, string> = Object.fromEntries(
  MOCK_CITIES.items.map((c) => [c.code, c.name]),
);

export function cityName(code: string): string {
  return CITY_NAMES[code] ?? code;
}

/** `MH12AB1234` → `MH 12 AB 1234`. Grouped the way the plate itself is stamped. */
export function displayPlate(canonical: string): string {
  const match = /^([A-Z]{2})([0-9]{1,2})([A-Z]{0,3})([0-9]{1,4})$/.exec(canonical);
  if (!match) return canonical;
  return [match[1], match[2], match[3], match[4]].filter(Boolean).join(' ');
}

const indianNumber = new Intl.NumberFormat('en-IN');

function ordinal(n: number): string {
  if (n === 1) return '1st';
  if (n === 2) return '2nd';
  if (n === 3) return '3rd';
  return `${n}th`;
}

/** One formatter for cards and detail rows alike, so the same value never reads two ways. */
export function formatAttributeValue(code: string, raw: string | number | boolean): string {
  const attribute = ALL_ATTRIBUTES.find((a) => a.code === code);
  if (code === 'owners') return ordinal(Number(raw));
  /* A year is an identifier, not a quantity. `2,023` is a typo the eye has to correct. */
  if (code === 'year') return String(raw);
  if (attribute?.options) {
    const option = attribute.options.find((o) => o.value === String(raw));
    if (option) return option.label;
  }
  if (typeof raw === 'number') {
    const formatted = Number.isInteger(raw) ? indianNumber.format(raw) : String(raw);
    return attribute?.unit ? `${formatted} ${attribute.unit}` : formatted;
  }
  if (typeof raw === 'boolean') return raw ? 'Yes' : 'No';
  return String(raw);
}

/**
 * Builds the spec strip the way the server does: walk the category's attributes in registry order,
 * keep the searchable ones the vehicle actually has, take the first four. Nothing hardcodes
 * "year, kms, fuel, owner" — that ordering falls out of `sortOrder` in the taxonomy.
 */
export function buildSpecStrip(vehicle: MockVehicle, limit = 4): SpecItem[] {
  const { attributes } = mockAttributes(vehicle.categoryCode);
  return attributes
    .filter((a) => a.searchable !== false && vehicle.attributes[a.code] !== undefined)
    .slice(0, limit)
    .map((a) => {
      const raw = vehicle.attributes[a.code];
      return {
        code: a.code,
        label: a.label,
        value: raw === undefined ? '—' : formatAttributeValue(a.code, raw),
      };
    });
}

/**
 * A flat placeholder cover, generated inline so the demo has no external image dependency and no
 * broken tiles. Renders the plate the way the product does: mono, letter-spaced, hard-bordered,
 * white for private, plate-yellow for commercial, signage-green for EV.
 */
export function coverImage(vehicle: MockVehicle): string {
  const fill =
    vehicle.plateClass === 'commercial'
      ? '#F2C230'
      : vehicle.plateClass === 'ev'
        ? '#0B3D2E'
        : '#F6F5F1';
  const ink = vehicle.plateClass === 'ev' ? '#F6F5F1' : '#0F1512';
  const plate = displayPlate(vehicle.plate);
  const svg = `<svg xmlns="http://www.w3.org/2000/svg" width="1200" height="800" viewBox="0 0 1200 800">
<rect width="1200" height="800" fill="#0F1512"/>
<rect x="1" y="1" width="1198" height="798" fill="none" stroke="#1D2622" stroke-width="2"/>
<rect x="300" y="300" width="600" height="160" fill="${fill}" stroke="${ink}" stroke-width="6"/>
<text x="600" y="405" text-anchor="middle" font-family="ui-monospace, monospace" font-size="66" font-weight="700" letter-spacing="8" fill="${ink}">${plate}</text>
<text x="600" y="530" text-anchor="middle" font-family="system-ui, sans-serif" font-size="30" letter-spacing="6" fill="#64726A">${vehicle.title.toUpperCase()}</text>
</svg>`;
  return `data:image/svg+xml;charset=utf-8,${encodeURIComponent(svg)}`;
}

export function toSearchItem(vehicle: MockVehicle): SearchItem {
  return {
    listingId: vehicle.listingId,
    intent: vehicle.intent,
    registration: {
      canonical: vehicle.plate,
      display: displayPlate(vehicle.plate),
      plateClass: vehicle.plateClass,
      stateCode: vehicle.plate.slice(0, 2),
    },
    title: vehicle.title,
    priceMinor: vehicle.priceMinor,
    pricePeriod: vehicle.pricePeriod ?? null,
    city: cityName(vehicle.cityCode),
    specStrip: buildSpecStrip(vehicle),
    media: {
      coverUrl: coverImage(vehicle),
      aspectRatio: 1.5,
      count: vehicle.photoCount,
    },
    badges: vehicle.badges,
    dealerName: vehicle.sellerType === 'dealer' ? vehicle.sellerName : null,
  };
}

export function findVehicle(listingId: string): MockVehicle | undefined {
  return MOCK_VEHICLES.find((v) => v.listingId === listingId);
}

export function findVehicleByPlate(plate: string): MockVehicle | undefined {
  const canonical = plate.replace(/[^A-Za-z0-9]/g, '').toUpperCase();
  return MOCK_VEHICLES.find((v) => v.plate === canonical);
}
