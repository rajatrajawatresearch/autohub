/**
 * Fixtures for the registry plane — taxonomy, geography, features, localisation.
 *
 * These stand in for the ten registries while the backend is being built in parallel. They are
 * shaped exactly like the contract's responses, so the day the API comes up the only thing that
 * changes is `NEXT_PUBLIC_API_MOCK`.
 *
 * Fixture data is allowed to contain business constants — that is what a fixture *is*. Application
 * code still reads every one of them from these responses at runtime.
 */

import type { z } from 'zod';
import type {
  AttributesResponse,
  CitiesResponseSchema,
  FeaturesSchema,
  I18nBundleSchema,
  TaxonomyAttribute,
  TaxonomyNodesResponseSchema,
  TaxonomyTreeSchema,
} from '../schemas';

export const REGISTRY_VERSION = 41;

/* ── Geography ────────────────────────────────────────────────────────────── */

export const MOCK_CITIES: z.input<typeof CitiesResponseSchema> = {
  version: 9,
  items: [
    {
      code: 'pune',
      name: 'Pune',
      stateCode: 'MH',
      lat: 18.5204,
      lng: 73.8567,
      serviceable: true,
      modules: ['buy', 'sell', 'auction', 'rental', 'finance', 'insurance', 'attachment'],
    },
    {
      code: 'mumbai',
      name: 'Mumbai',
      stateCode: 'MH',
      lat: 19.076,
      lng: 72.8777,
      serviceable: true,
      modules: ['buy', 'sell', 'auction', 'rental', 'lease', 'finance', 'insurance', 'attachment'],
    },
    {
      code: 'bengaluru',
      name: 'Bengaluru',
      stateCode: 'KA',
      lat: 12.9716,
      lng: 77.5946,
      serviceable: true,
      modules: ['buy', 'sell', 'auction', 'rental', 'lease', 'finance', 'insurance'],
    },
    {
      code: 'delhi',
      name: 'Delhi',
      stateCode: 'DL',
      lat: 28.6139,
      lng: 77.209,
      serviceable: true,
      modules: ['buy', 'sell', 'auction', 'finance', 'insurance', 'attachment'],
    },
    {
      code: 'hyderabad',
      name: 'Hyderabad',
      stateCode: 'TG',
      lat: 17.385,
      lng: 78.4867,
      serviceable: true,
      modules: ['buy', 'sell', 'auction', 'finance'],
    },
    {
      code: 'ahmedabad',
      name: 'Ahmedabad',
      stateCode: 'GJ',
      lat: 23.0225,
      lng: 72.5714,
      serviceable: true,
      modules: ['buy', 'sell', 'finance'],
    },
    {
      code: 'nagpur',
      name: 'Nagpur',
      stateCode: 'MH',
      lat: 21.1458,
      lng: 79.0882,
      serviceable: false,
      modules: [],
    },
  ],
};

/* ── Feature & module registry ───────────────────────────────────────────── */

export const MOCK_FEATURES: z.input<typeof FeaturesSchema> = {
  version: 12,
  modules: {
    buy: true,
    sell: true,
    auction: true,
    rental: true,
    lease: false,
    attachment: true,
    finance: true,
    insurance: true,
    inspection: true,
  },
  flags: {
    instant_offer: true,
    ai_damage_detection: false,
    whatsapp_chat: true,
    proxy_bidding: true,
    challan_check: true,
    home_test_drive: true,
    bulk_upload: true,
  },
};

/* ── Localisation ────────────────────────────────────────────────────────── */

const EN_IN: Record<string, string> = {
  'app.name': 'AutoHub',
  'nav.buy': 'Buy',
  'nav.sell': 'Sell',
  'nav.auction': 'Auction',
  'nav.rent': 'Rent',
  'nav.finance': 'Finance',
  'nav.insurance': 'Insurance',
  'listing.cta.get_valuation': 'Get valuation',
  'listing.cta.view_details': 'View details',
  'listing.cta.contact_seller': 'Contact seller',
  'listing.cta.book_test_drive': 'Book test drive',
  'auction.cta.place_bid': 'Place bid',
  'auction.state.live': 'Live',
  'auction.state.closed': 'Closed',
  'auction.label.current_bid': 'Current bid',
  'auction.label.min_next_bid': 'Minimum next bid',
  'auction.empty.no_bids': 'No bids yet. Bidding opens when the reserve is set.',
  'valuation.hero.title': 'What is your car worth today?',
  'valuation.hero.subtitle': 'Free · 30 seconds · no phone number needed',
  'valuation.label.market_value': 'Market value',
  'valuation.label.as_of': 'As of',
  'search.empty.no_results': 'No vehicles match these filters. Widen the price range or clear a filter.',
  'error.registration.malformed': 'Check the digits — example MH12AB1234.',
  'error.catalog.vahan.record_not_found':
    "This registration number isn't in the RTO records. Check the digits, or enter details manually.",
  'error.common.network.unreachable': 'Cannot reach AutoHub. Check your connection and try again.',
  'field.kms_driven': 'Kms driven',
  'field.owners': 'Owners',
  'field.fuel_type': 'Fuel',
  'field.insurance_validity': 'Insurance validity',
  'field.rc_status': 'RC status',
};

const HI_IN: Record<string, string> = {
  'nav.buy': 'खरीदें',
  'nav.sell': 'बेचें',
  'nav.auction': 'नीलामी',
  'nav.rent': 'किराया',
  'listing.cta.get_valuation': 'कीमत जानें',
  'auction.cta.place_bid': 'बोली लगाएँ',
  'auction.label.current_bid': 'मौजूदा बोली',
  'valuation.hero.title': 'आज आपकी कार की कीमत क्या है?',
  'valuation.label.market_value': 'बाज़ार मूल्य',
  'error.registration.malformed': 'अंक जाँचें — उदाहरण MH12AB1234',
  'field.kms_driven': 'चली हुई किलोमीटर',
  'field.owners': 'मालिक',
  'field.fuel_type': 'ईंधन',
};

const MR_IN: Record<string, string> = {
  'nav.buy': 'खरेदी',
  'nav.sell': 'विक्री',
  'nav.auction': 'लिलाव',
  'listing.cta.get_valuation': 'किंमत पहा',
  'auction.cta.place_bid': 'बोली लावा',
  'valuation.hero.title': 'आज तुमच्या गाडीची किंमत किती?',
  'field.kms_driven': 'चाललेले किलोमीटर',
  'field.owners': 'मालक',
};

export const MOCK_I18N: Record<string, z.input<typeof I18nBundleSchema>> = {
  'en-IN': { locale: 'en-IN', version: 88, resources: EN_IN },
  'hi-IN': { locale: 'hi-IN', version: 88, resources: HI_IN },
  'mr-IN': { locale: 'mr-IN', version: 88, resources: MR_IN },
};

/* ── Taxonomy ────────────────────────────────────────────────────────────── */

export const MOCK_TAXONOMY_TREE: z.input<typeof TaxonomyTreeSchema> = {
  version: REGISTRY_VERSION,
  categories: [
    {
      code: 'car',
      name: 'Car',
      icon: 'car',
      sortOrder: 1,
      attributes: ['fuel_type', 'transmission', 'owners', 'kms_driven', 'year', 'seats'],
      children: [
        { code: 'hatchback', name: 'Hatchback', sortOrder: 1, children: [], attributes: [] },
        { code: 'sedan', name: 'Sedan', sortOrder: 2, children: [], attributes: [] },
        { code: 'suv', name: 'SUV', sortOrder: 3, children: [], attributes: [] },
        { code: 'muv', name: 'MUV', sortOrder: 4, children: [], attributes: [] },
      ],
    },
    {
      code: 'bike',
      name: 'Bike',
      icon: 'bike',
      sortOrder: 2,
      attributes: ['fuel_type', 'engine_cc', 'owners', 'kms_driven', 'year'],
      children: [
        { code: 'motorcycle', name: 'Motorcycle', sortOrder: 1, children: [], attributes: [] },
        { code: 'scooter', name: 'Scooter', sortOrder: 2, children: [], attributes: [] },
      ],
    },
    {
      code: 'commercial',
      name: 'Commercial',
      icon: 'truck',
      sortOrder: 3,
      attributes: ['fuel_type', 'payload_kg', 'axle_config', 'permit_type', 'kms_driven', 'owners'],
      children: [
        { code: 'pickup', name: 'Pickup', sortOrder: 1, children: [], attributes: [] },
        { code: 'truck', name: 'Truck', sortOrder: 2, children: [], attributes: [] },
        { code: 'tempo', name: 'Tempo traveller', sortOrder: 3, children: [], attributes: [] },
        { code: 'bus', name: 'Bus', sortOrder: 4, children: [], attributes: [] },
      ],
    },
    {
      /* Added to the registry, not to the codebase — the claim in North Star §8, demonstrated. */
      code: 'construction',
      name: 'Construction equipment',
      icon: 'construction',
      sortOrder: 4,
      attributes: ['fuel_type', 'operating_hours', 'boom_length_m', 'lifting_capacity_kg'],
      children: [
        { code: 'backhoe', name: 'Backhoe loader', sortOrder: 1, children: [], attributes: [] },
        { code: 'excavator', name: 'Excavator', sortOrder: 2, children: [], attributes: [] },
        { code: 'crane', name: 'Crane', sortOrder: 3, children: [], attributes: [] },
      ],
    },
  ],
};

type Attribute = TaxonomyAttribute;

const FUEL_TYPE: Attribute = {
  code: 'fuel_type',
  label: 'Fuel',
  dataType: 'enum',
  unit: null,
  required: true,
  searchable: true,
  facetStyle: 'chips',
  displayGroup: 'engine',
  sortOrder: 15,
  options: [
    { value: 'petrol', label: 'Petrol', sortOrder: 1 },
    { value: 'diesel', label: 'Diesel', sortOrder: 2 },
    { value: 'cng', label: 'CNG', sortOrder: 3 },
    { value: 'ev', label: 'Electric', sortOrder: 4 },
    { value: 'hybrid', label: 'Hybrid', sortOrder: 5 },
  ],
};

const TRANSMISSION: Attribute = {
  code: 'transmission',
  label: 'Transmission',
  dataType: 'enum',
  required: true,
  searchable: true,
  facetStyle: 'chips',
  displayGroup: 'engine',
  sortOrder: 30,
  options: [
    { value: 'manual', label: 'Manual', sortOrder: 1 },
    { value: 'automatic', label: 'Automatic', sortOrder: 2 },
    { value: 'amt', label: 'AMT', sortOrder: 3 },
    { value: 'dct', label: 'DCT', sortOrder: 4 },
  ],
};

const OWNERS: Attribute = {
  code: 'owners',
  label: 'Owners',
  dataType: 'enum',
  required: true,
  searchable: true,
  facetStyle: 'chips',
  displayGroup: 'usage',
  sortOrder: 20,
  options: [
    { value: '1', label: '1st owner', sortOrder: 1 },
    { value: '2', label: '2nd owner', sortOrder: 2 },
    { value: '3', label: '3rd owner', sortOrder: 3 },
    { value: '4', label: '4th owner or more', sortOrder: 4 },
  ],
};

const KMS_DRIVEN: Attribute = {
  code: 'kms_driven',
  label: 'Kms driven',
  dataType: 'integer',
  unit: 'km',
  required: true,
  searchable: true,
  facetStyle: 'range',
  range: { min: 0, max: 500000, step: 1000 },
  displayGroup: 'usage',
  sortOrder: 10,
};

const YEAR: Attribute = {
  code: 'year',
  label: 'Year',
  dataType: 'integer',
  required: true,
  searchable: true,
  facetStyle: 'range',
  range: { min: 2005, max: 2026, step: 1 },
  displayGroup: 'identity',
  sortOrder: 5,
};

const SEATS: Attribute = {
  code: 'seats',
  label: 'Seats',
  dataType: 'integer',
  required: false,
  searchable: true,
  facetStyle: 'chips',
  displayGroup: 'body',
  sortOrder: 50,
  options: [
    { value: '2', label: '2', sortOrder: 1 },
    { value: '4', label: '4', sortOrder: 2 },
    { value: '5', label: '5', sortOrder: 3 },
    { value: '7', label: '7', sortOrder: 4 },
    { value: '8', label: '8+', sortOrder: 5 },
  ],
};

const INSURANCE_VALIDITY: Attribute = {
  code: 'insurance_validity',
  label: 'Insurance validity',
  dataType: 'date',
  required: false,
  searchable: false,
  displayGroup: 'documents',
  sortOrder: 70,
};

const RC_STATUS: Attribute = {
  code: 'rc_status',
  label: 'RC status',
  dataType: 'enum',
  required: false,
  searchable: true,
  facetStyle: 'list',
  displayGroup: 'documents',
  sortOrder: 80,
  options: [
    { value: 'original', label: 'Original RC', sortOrder: 1 },
    { value: 'duplicate', label: 'Duplicate RC', sortOrder: 2 },
    { value: 'hypothecated', label: 'Hypothecation active', sortOrder: 3 },
  ],
};

const BATTERY_HEALTH: Attribute = {
  code: 'battery_health_pct',
  label: 'Battery health',
  dataType: 'integer',
  unit: '%',
  required: false,
  searchable: true,
  facetStyle: 'range',
  range: { min: 0, max: 100, step: 1 },
  displayGroup: 'engine',
  sortOrder: 35,
  helpText: 'State of health reported by the vehicle, or measured at inspection.',
};

const ENGINE_CC: Attribute = {
  code: 'engine_cc',
  label: 'Engine',
  dataType: 'integer',
  unit: 'cc',
  required: true,
  searchable: true,
  facetStyle: 'range',
  range: { min: 50, max: 2000, step: 10 },
  displayGroup: 'engine',
  sortOrder: 40,
};

const PAYLOAD_KG: Attribute = {
  code: 'payload_kg',
  label: 'Payload',
  dataType: 'integer',
  unit: 'kg',
  required: true,
  searchable: true,
  facetStyle: 'range',
  range: { min: 500, max: 40000, step: 100 },
  displayGroup: 'capability',
  sortOrder: 46,
};

const AXLE_CONFIG: Attribute = {
  code: 'axle_config',
  label: 'Axle configuration',
  dataType: 'enum',
  required: false,
  searchable: true,
  facetStyle: 'list',
  displayGroup: 'capability',
  sortOrder: 47,
  options: [
    { value: '4x2', label: '4x2', sortOrder: 1 },
    { value: '6x2', label: '6x2', sortOrder: 2 },
    { value: '6x4', label: '6x4', sortOrder: 3 },
    { value: '10x2', label: '10x2', sortOrder: 4 },
  ],
};

const PERMIT_TYPE: Attribute = {
  code: 'permit_type',
  label: 'Permit',
  dataType: 'enum',
  required: true,
  searchable: true,
  facetStyle: 'list',
  displayGroup: 'documents',
  sortOrder: 75,
  options: [
    { value: 'national', label: 'National permit', sortOrder: 1 },
    { value: 'state', label: 'State permit', sortOrder: 2 },
    { value: 'contract_carriage', label: 'Contract carriage', sortOrder: 3 },
    { value: 'goods', label: 'Goods carriage', sortOrder: 4 },
  ],
};

const OPERATING_HOURS: Attribute = {
  code: 'operating_hours',
  label: 'Operating hours',
  dataType: 'integer',
  unit: 'hrs',
  required: true,
  searchable: true,
  facetStyle: 'range',
  range: { min: 0, max: 40000, step: 100 },
  displayGroup: 'usage',
  sortOrder: 12,
};

const BOOM_LENGTH: Attribute = {
  code: 'boom_length_m',
  label: 'Boom length',
  dataType: 'number',
  unit: 'm',
  required: false,
  searchable: true,
  facetStyle: 'range',
  range: { min: 2, max: 60, step: 0.5 },
  displayGroup: 'capability',
  sortOrder: 44,
};

const LIFTING_CAPACITY: Attribute = {
  code: 'lifting_capacity_kg',
  label: 'Lifting capacity',
  dataType: 'integer',
  unit: 'kg',
  required: false,
  searchable: true,
  facetStyle: 'range',
  range: { min: 500, max: 200000, step: 500 },
  displayGroup: 'capability',
  sortOrder: 45,
};

const CAR_ATTRS: Attribute[] = [
  YEAR,
  FUEL_TYPE,
  BATTERY_HEALTH,
  TRANSMISSION,
  OWNERS,
  KMS_DRIVEN,
  SEATS,
  INSURANCE_VALIDITY,
  RC_STATUS,
];

const BIKE_ATTRS: Attribute[] = [
  YEAR,
  FUEL_TYPE,
  ENGINE_CC,
  OWNERS,
  KMS_DRIVEN,
  INSURANCE_VALIDITY,
  RC_STATUS,
];

const COMMERCIAL_ATTRS: Attribute[] = [
  YEAR,
  FUEL_TYPE,
  OWNERS,
  KMS_DRIVEN,
  PAYLOAD_KG,
  AXLE_CONFIG,
  PERMIT_TYPE,
  INSURANCE_VALIDITY,
  RC_STATUS,
];

const CONSTRUCTION_ATTRS: Attribute[] = [
  YEAR,
  FUEL_TYPE,
  OPERATING_HOURS,
  BOOM_LENGTH,
  LIFTING_CAPACITY,
  RC_STATUS,
];

const ATTRIBUTES_BY_CATEGORY: Record<string, Attribute[]> = {
  car: CAR_ATTRS,
  hatchback: CAR_ATTRS,
  sedan: CAR_ATTRS,
  suv: CAR_ATTRS,
  muv: CAR_ATTRS,
  bike: BIKE_ATTRS,
  motorcycle: BIKE_ATTRS,
  scooter: BIKE_ATTRS,
  commercial: COMMERCIAL_ATTRS,
  pickup: COMMERCIAL_ATTRS,
  truck: COMMERCIAL_ATTRS,
  tempo: COMMERCIAL_ATTRS,
  bus: COMMERCIAL_ATTRS,
  construction: CONSTRUCTION_ATTRS,
  backhoe: CONSTRUCTION_ATTRS,
  excavator: CONSTRUCTION_ATTRS,
  crane: CONSTRUCTION_ATTRS,
};

/**
 * Always returned in `sortOrder`, the way the real registry serves them. Everything downstream —
 * form field order, facet order, spec-row order, which four specs land on a card — falls out of
 * this one ordering rather than out of a layout written in a component.
 */
export function mockAttributes(category: string | null): AttributesResponse {
  const attributes = (category && ATTRIBUTES_BY_CATEGORY[category]) || CAR_ATTRS;
  return {
    version: REGISTRY_VERSION,
    attributes: [...attributes].sort((a, b) => a.sortOrder - b.sortOrder),
  };
}

/** Every attribute the fixtures know about, for label lookups when building spec strips. */
export const ALL_ATTRIBUTES: Attribute[] = [
  YEAR,
  FUEL_TYPE,
  BATTERY_HEALTH,
  TRANSMISSION,
  OWNERS,
  KMS_DRIVEN,
  SEATS,
  ENGINE_CC,
  PAYLOAD_KG,
  AXLE_CONFIG,
  PERMIT_TYPE,
  OPERATING_HOURS,
  BOOM_LENGTH,
  LIFTING_CAPACITY,
  INSURANCE_VALIDITY,
  RC_STATUS,
];

/* ── Makes, models, variants ─────────────────────────────────────────────── */

type Node = z.input<typeof TaxonomyNodesResponseSchema>['items'][number];

const MAKES: Node[] = [
  { code: 'maruti-suzuki', name: 'Maruti Suzuki', categoryCode: 'car', popularity: 100 },
  { code: 'hyundai', name: 'Hyundai', categoryCode: 'car', popularity: 92 },
  { code: 'tata', name: 'Tata Motors', categoryCode: 'car', popularity: 88 },
  { code: 'mahindra', name: 'Mahindra', categoryCode: 'car', popularity: 85 },
  { code: 'toyota', name: 'Toyota', categoryCode: 'car', popularity: 74 },
  { code: 'kia', name: 'Kia', categoryCode: 'car', popularity: 68 },
  { code: 'honda', name: 'Honda', categoryCode: 'car', popularity: 62 },
  { code: 'ashok-leyland', name: 'Ashok Leyland', categoryCode: 'commercial', popularity: 70 },
  { code: 'eicher', name: 'Eicher', categoryCode: 'commercial', popularity: 48 },
  { code: 'royal-enfield', name: 'Royal Enfield', categoryCode: 'bike', popularity: 80 },
  { code: 'bajaj', name: 'Bajaj', categoryCode: 'bike', popularity: 76 },
  { code: 'honda-2w', name: 'Honda Motorcycle', categoryCode: 'bike', popularity: 72 },
  { code: 'jcb', name: 'JCB', categoryCode: 'construction', popularity: 90 },
];

const MODELS: Node[] = [
  { code: 'swift', name: 'Swift', makeCode: 'maruti-suzuki', categoryCode: 'hatchback', years: [2005, 2026] },
  { code: 'baleno', name: 'Baleno', makeCode: 'maruti-suzuki', categoryCode: 'hatchback', years: [2015, 2026] },
  { code: 'dzire', name: 'Dzire', makeCode: 'maruti-suzuki', categoryCode: 'sedan', years: [2008, 2026] },
  { code: 'ertiga', name: 'Ertiga', makeCode: 'maruti-suzuki', categoryCode: 'muv', years: [2012, 2026] },
  { code: 'creta', name: 'Creta', makeCode: 'hyundai', categoryCode: 'suv', years: [2015, 2026] },
  { code: 'i20', name: 'i20', makeCode: 'hyundai', categoryCode: 'hatchback', years: [2008, 2026] },
  { code: 'nexon-ev', name: 'Nexon EV', makeCode: 'tata', categoryCode: 'suv', years: [2020, 2026] },
  { code: 'punch', name: 'Punch', makeCode: 'tata', categoryCode: 'suv', years: [2021, 2026] },
  { code: 'scorpio', name: 'Scorpio', makeCode: 'mahindra', categoryCode: 'suv', years: [2002, 2026] },
  { code: 'xuv700', name: 'XUV700', makeCode: 'mahindra', categoryCode: 'suv', years: [2021, 2026] },
  { code: 'bolero', name: 'Bolero', makeCode: 'mahindra', categoryCode: 'suv', years: [2001, 2026] },
  { code: 'innova-crysta', name: 'Innova Crysta', makeCode: 'toyota', categoryCode: 'muv', years: [2016, 2026] },
  { code: 'seltos', name: 'Seltos', makeCode: 'kia', categoryCode: 'suv', years: [2019, 2026] },
  { code: 'city', name: 'City', makeCode: 'honda', categoryCode: 'sedan', years: [2003, 2026] },
  { code: 'dost', name: 'Dost', makeCode: 'ashok-leyland', categoryCode: 'pickup', years: [2011, 2026] },
  { code: 'classic-350', name: 'Classic 350', makeCode: 'royal-enfield', categoryCode: 'motorcycle', years: [2009, 2026] },
  { code: '3dx', name: '3DX Backhoe Loader', makeCode: 'jcb', categoryCode: 'backhoe', years: [2010, 2026] },
];

const VARIANTS: Record<string, Node[]> = {
  swift: [
    { code: 'swift-lxi', name: 'LXi', modelCode: 'swift', makeCode: 'maruti-suzuki' },
    { code: 'swift-vxi', name: 'VXi', modelCode: 'swift', makeCode: 'maruti-suzuki' },
    { code: 'swift-zxi', name: 'ZXi', modelCode: 'swift', makeCode: 'maruti-suzuki' },
    { code: 'swift-zxi-plus', name: 'ZXi+', modelCode: 'swift', makeCode: 'maruti-suzuki' },
  ],
  creta: [
    { code: 'creta-e', name: 'E', modelCode: 'creta', makeCode: 'hyundai' },
    { code: 'creta-sx', name: 'SX', modelCode: 'creta', makeCode: 'hyundai' },
    { code: 'creta-sx-o', name: 'SX(O)', modelCode: 'creta', makeCode: 'hyundai' },
  ],
  scorpio: [
    { code: 'scorpio-s5', name: 'S5', modelCode: 'scorpio', makeCode: 'mahindra' },
    { code: 'scorpio-s11', name: 'S11', modelCode: 'scorpio', makeCode: 'mahindra' },
  ],
  'nexon-ev': [
    { code: 'nexon-ev-xz-plus', name: 'Max XZ+', modelCode: 'nexon-ev', makeCode: 'tata' },
    { code: 'nexon-ev-xm', name: 'XM', modelCode: 'nexon-ev', makeCode: 'tata' },
  ],
};

export function mockMakes(category: string | null): z.input<typeof TaxonomyNodesResponseSchema> {
  const items = category ? MAKES.filter((m) => m.categoryCode === category) : MAKES;
  return { version: REGISTRY_VERSION, items: items.length > 0 ? items : MAKES };
}

export function mockModels(make: string | null): z.input<typeof TaxonomyNodesResponseSchema> {
  const items = make ? MODELS.filter((m) => m.makeCode === make) : MODELS;
  return { version: REGISTRY_VERSION, items };
}

export function mockVariants(model: string | null): z.input<typeof TaxonomyNodesResponseSchema> {
  // `model && …` would yield the empty string for `model=""`, which is a valid query but not a
  // valid item list. A conditional keeps the type honest.
  const items = model ? (VARIANTS[model] ?? []) : [];
  return { version: REGISTRY_VERSION, items };
}
