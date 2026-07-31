/**
 * The mock router.
 *
 * `http.ts` consults this before (or instead of) the network. It answers every endpoint in
 * `docs/contracts/registry-api.md` with the same shapes the real API returns, so the three portals
 * can be built, reviewed and demoed while the backend is still being written — and so the switch
 * to the real thing is an environment variable, not a refactor.
 *
 * State that a demo needs to *move* — bids, workflow transitions — is held in memory for the life
 * of the tab. Everything else is deterministic.
 */

import { mockMe, mockOtpRequest, mockOtpVerify, mockSession } from './auth';
import {
  mockAuctionLot,
  mockAuctionLots,
  mockLotBids,
  mockPlaceBid,
  subscribeMockAuction,
} from './auction';
import { mockDealDetail, mockDealList } from './deals';
import { MOCK_FORMS } from './forms';
import { mockListingDetail, mockListingList } from './listings';
import { mockPricingQuote } from './pricing';
import {
  MOCK_CITIES,
  MOCK_FEATURES,
  MOCK_I18N,
  MOCK_TAXONOMY_TREE,
  mockAttributes,
  mockMakes,
  mockModels,
  mockVariants,
} from './registry';
import { mockSearch } from './search';
import { mockValuationQuote } from './valuation';
import { MOCK_WORKFLOWS, WORKFLOW_BY_RESOURCE, mockWorkflowSnapshot } from './workflows';
import { findVehicle } from './vehicles';
import { PricingQuoteRequestSchema } from '../schemas';
import type { ProblemDetails } from '../schemas';

export { subscribeMockAuction };

export interface MockResponse {
  status: number;
  body: unknown;
}

function ok(body: unknown): MockResponse {
  return { status: 200, body };
}

function problem(status: number, code: string, title: string, detail?: string): MockResponse {
  const payload: ProblemDetails = {
    type: `https://autohub.in/errors/${code}`,
    title,
    status,
    code,
    detail,
  };
  return { status, body: payload };
}

function notFound(code: string, title: string, detail?: string): MockResponse {
  return problem(404, code, title, detail);
}

/* ── Workflow state that a demo can actually move ─────────────────────────── */

const stateOverrides = new Map<string, string>();

function currentStateFor(module: string, resource: string, id: string): string | null {
  const key = `${module}/${resource}/${id}`;
  const override = stateOverrides.get(key);
  if (override) return override;
  if (resource === 'listings') return findVehicle(id)?.state ?? null;
  if (resource === 'lots') return mockAuctionLot(id)?.state.code ?? null;
  if (resource === 'deals') return mockDealDetail(id)?.state.code ?? null;
  return null;
}

function workflowSnapshotFor(module: string, resource: string, id: string): MockResponse {
  const workflowKey = WORKFLOW_BY_RESOURCE[`${module}/${resource}`];
  const state = currentStateFor(module, resource, id);
  if (!workflowKey || !state) {
    return notFound('common.resource.not_found', 'Not found', 'No workflow for that resource.');
  }
  const snapshot = mockWorkflowSnapshot(workflowKey, state);
  return snapshot ? ok(snapshot) : notFound('platform.registry.key_not_found', 'Workflow not found');
}

function fireWorkflowAction(
  module: string,
  resource: string,
  id: string,
  body: Record<string, unknown>,
): MockResponse {
  const workflowKey = WORKFLOW_BY_RESOURCE[`${module}/${resource}`];
  const state = currentStateFor(module, resource, id);
  const definition = workflowKey ? MOCK_WORKFLOWS[workflowKey] : undefined;
  if (!workflowKey || !state || !definition) {
    return notFound('common.resource.not_found', 'Not found');
  }

  const actionCode = typeof body.actionCode === 'string' ? body.actionCode : '';
  const transition = definition.transitions.find(
    (t) => t.actionCode === actionCode && t.from === state,
  );

  if (!transition) {
    const currentState = definition.states.find((s) => s.code === state);
    return {
      status: 409,
      body: {
        type: 'https://autohub.in/errors/platform.workflow.transition_not_allowed',
        title: 'That action is no longer available',
        status: 409,
        code: 'platform.workflow.transition_not_allowed',
        detail: 'The state moved while this screen was open. Refresh to see where it is now.',
        metadata: { currentState },
      } satisfies ProblemDetails,
    };
  }

  if (transition.requiresReason && !body.reason) {
    return {
      status: 422,
      body: {
        type: 'https://autohub.in/errors/platform.workflow.guard_failed',
        title: 'A reason is required',
        status: 422,
        code: 'platform.workflow.guard_failed',
        detail: 'Say why. The other party sees this.',
        errors: [{ pointer: '/reason', code: 'platform.workflow.reason_required' }],
      } satisfies ProblemDetails,
    };
  }

  stateOverrides.set(`${module}/${resource}/${id}`, transition.to);
  const snapshot = mockWorkflowSnapshot(workflowKey, transition.to);
  return snapshot ? ok(snapshot) : notFound('platform.registry.key_not_found', 'Workflow not found');
}

/* ── Router ──────────────────────────────────────────────────────────────── */

function asRecord(body: unknown): Record<string, unknown> {
  return body && typeof body === 'object' ? (body as Record<string, unknown>) : {};
}

/** Returns `null` when the mock has no opinion, which lets the caller surface a real 404. */
export function resolveMock(
  method: string,
  path: string,
  query: Record<string, string>,
  body: unknown,
): MockResponse | null {
  const verb = method.toUpperCase();
  const s = path.split('?')[0]?.split('/').filter(Boolean) ?? [];
  const [a, b, c, d, e] = s;

  /* ── platform ─────────────────────────────────────────────────────────── */
  if (a === 'platform') {
    if (b === 'taxonomy') {
      if (c === 'tree') return ok(MOCK_TAXONOMY_TREE);
      if (c === 'attributes') return ok(mockAttributes(query.category ?? null));
      if (c === 'makes') return ok(mockMakes(query.category ?? null));
      if (c === 'models') return ok(mockModels(query.make ?? null));
      if (c === 'variants') return ok(mockVariants(query.model ?? null));
    }

    if (b === 'forms' && c) {
      if (verb === 'POST' && d === 'validate') return ok({ valid: true });
      const form = MOCK_FORMS[c];
      return form
        ? ok(form)
        : notFound('platform.registry.key_not_found', 'Form not found', `No form named ${c}.`);
    }

    if (b === 'workflows' && c) {
      const workflow = MOCK_WORKFLOWS[c];
      return workflow
        ? ok(workflow)
        : notFound('platform.registry.key_not_found', 'Workflow not found');
    }

    if (b === 'pricing' && c === 'quote' && verb === 'POST') {
      const parsed = PricingQuoteRequestSchema.safeParse(body);
      if (!parsed.success) {
        return problem(400, 'common.request.malformed_json', 'Pricing request is not readable');
      }
      return ok(mockPricingQuote(parsed.data));
    }

    if (b === 'features') return ok(MOCK_FEATURES);

    if (b === 'i18n' && c) {
      const bundle = MOCK_I18N[c] ?? MOCK_I18N['en-IN'];
      return bundle
        ? ok(bundle)
        : notFound('platform.i18n.locale_unsupported', 'Locale not available');
    }

    if (b === 'geo' && c === 'cities') {
      const serviceable = query.serviceable;
      if (serviceable === 'true') {
        return ok({
          version: MOCK_CITIES.version,
          items: MOCK_CITIES.items.filter((city) => city.serviceable),
        });
      }
      return ok(MOCK_CITIES);
    }
  }

  /* ── auth ─────────────────────────────────────────────────────────────── */
  if (a === 'auth') {
    if (b === 'otp' && c === 'request' && verb === 'POST') {
      const request = asRecord(body);
      return ok(mockOtpRequest({ phone: typeof request.phone === 'string' ? request.phone : undefined }));
    }
    if (b === 'otp' && c === 'verify' && verb === 'POST') {
      const request = asRecord(body);
      const result = mockOtpVerify({ code: typeof request.code === 'string' ? request.code : '' });
      return result.ok ? ok(result.body) : { status: result.status, body: result.problem };
    }
    if (b === 'refresh' && verb === 'POST') return ok(mockSession());
    if (b === 'logout' && verb === 'POST') return { status: 204, body: null };
    if (b === 'me') return ok(mockMe());
  }

  /* ── catalog ──────────────────────────────────────────────────────────── */
  if (a === 'catalog' && b === 'search') return ok(mockSearch(query));

  /* ── valuation ────────────────────────────────────────────────────────── */
  if (a === 'valuation' && b === 'quotes' && verb === 'POST') {
    const request = asRecord(body);
    const registrationNumber =
      typeof request.registrationNumber === 'string' ? request.registrationNumber : '';
    if (registrationNumber.replace(/[^A-Za-z0-9]/g, '').length < 6) {
      return {
        status: 422,
        body: {
          type: 'https://autohub.in/errors/catalog.registration.invalid_format',
          title: 'Check the registration number',
          status: 422,
          code: 'catalog.registration.invalid_format',
          detail: 'Check the digits — example MH12AB1234.',
          errors: [{ pointer: '/registrationNumber', code: 'registration.malformed' }],
        } satisfies ProblemDetails,
      };
    }
    return ok(
      mockValuationQuote({
        registrationNumber,
        kmsDriven: typeof request.kmsDriven === 'number' ? request.kmsDriven : undefined,
        city: typeof request.city === 'string' ? request.city : undefined,
        condition: typeof request.condition === 'string' ? request.condition : undefined,
      }),
    );
  }

  /* ── generic workflow plane: /{module}/{resource}/{id}/workflow ───────── */
  if (a && b && c && d === 'workflow') {
    if (verb === 'GET' && e === undefined) return workflowSnapshotFor(a, b, c);
    if (verb === 'POST' && e === 'actions') return fireWorkflowAction(a, b, c, asRecord(body));
  }

  /* ── listings ─────────────────────────────────────────────────────────── */
  if (a === 'listings' && b === 'listings') {
    if (!c) return ok(mockListingList(query));
    const detail = mockListingDetail(c);
    return detail ? ok(detail) : notFound('listings.listing.not_found', 'Listing not found');
  }

  /* ── auction ──────────────────────────────────────────────────────────── */
  if (a === 'auction' && b === 'lots') {
    if (!c) return ok(mockAuctionLots(query));
    if (d === 'bids') {
      if (verb === 'POST') {
        const request = asRecord(body);
        const result = mockPlaceBid(c, {
          amountMinor: typeof request.amountMinor === 'number' ? request.amountMinor : 0,
          seenBidSeq: typeof request.seenBidSeq === 'number' ? request.seenBidSeq : undefined,
        });
        return result.ok
          ? { status: 201, body: result.body }
          : { status: result.status, body: result.problem };
      }
      return ok(mockLotBids(c));
    }
    const lot = mockAuctionLot(c);
    return lot ? ok(lot) : notFound('auction.auction.not_found', 'Lot not found');
  }

  /* ── deals ────────────────────────────────────────────────────────────── */
  if (a === 'deals') {
    if (b === 'deals') {
      if (!c) return ok(mockDealList(query));
      const detail = mockDealDetail(c);
      return detail ? ok(detail) : notFound('deals.deal.not_found', 'Deal not found');
    }
    if (b === 'offers' && verb === 'POST') {
      const request = asRecord(body);
      const listingId = typeof request.listingId === 'string' ? request.listingId : '';
      const first = mockDealList({}).items[0];
      if (!first) return notFound('deals.deal.not_found', 'Deal not found');
      return {
        status: 201,
        body: { ...first, listing: mockListingDetail(listingId) ?? first.listing },
      };
    }
  }

  return null;
}
