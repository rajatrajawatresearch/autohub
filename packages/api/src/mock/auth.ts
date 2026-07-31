/**
 * Mock auth.
 *
 * In mock mode the caller is always signed in as a verified Pune dealer, because the dealer and
 * admin portals are unusable otherwise and a demo that starts at a login wall demos a login wall.
 * OTP still works end to end so the customer sign-in flow can be shown: any six digits verify,
 * `000000` fails with the real error code so the failure state is demonstrable too.
 */

import type { z } from 'zod';
import type { AuthSessionSchema, AuthUserSchema, OtpChallengeSchema, ProblemDetails } from '../schemas';

type AuthUserInput = z.input<typeof AuthUserSchema>;
type AuthSessionInput = z.input<typeof AuthSessionSchema>;
type OtpChallengeInput = z.input<typeof OtpChallengeSchema>;

const DEMO_USER: AuthUserInput = {
  id: '019fabee-0001-7000-8000-000000000001',
  displayName: 'Whitefield Auto Mart',
  phone: '+919820012345',
  email: 'ops@whitefieldauto.in',
  roles: ['dealer_owner', 'auction_bidder', 'platform_admin'],
  permissions: [
    'listings.listing.create',
    'listings.listing.update',
    'listings.listing.submit',
    'listings.listing.moderate',
    'listings.listing.read_any',
    'dealer.inventory.write',
    'dealer.inventory.bulk_import',
    'auction.bid.place',
    'auction.auction.moderate',
    'deals.deal.accept',
    'deals.deal.update',
    'valuation.quote.create',
    'platform.registry.publish',
    'identity.user.impersonate',
  ],
  activeCity: 'pune',
  dealerId: '019fab0c-9002-7000-8000-000000000002',
  avatarUrl: null,
  kycLevel: 2,
};

export function mockMe(): AuthUserInput {
  return DEMO_USER;
}

export function mockOtpRequest(body: { phone?: string }): OtpChallengeInput {
  const phone = body.phone ?? '+919820012345';
  const masked = `${phone.slice(0, 6)} ${phone.slice(6, 8)}***${phone.slice(-1)}`;
  return {
    challengeId: '019fabee-0002-7000-8000-000000000002',
    expiresInSeconds: 300,
    resendAfterSeconds: 30,
    maskedTo: masked,
    hint: 'AH-4821',
    deliveryChannel: 'sms',
    attemptsAllowed: 5,
  };
}

export type MockOtpResult =
  | { ok: true; body: AuthSessionInput }
  | { ok: false; status: number; problem: ProblemDetails };

export function mockOtpVerify(body: { code?: string }): MockOtpResult {
  const code = body.code ?? '';

  if (code === '000000') {
    return {
      ok: false,
      status: 422,
      problem: {
        code: 'identity.otp.incorrect',
        title: 'That code did not match',
        status: 422,
        detail: 'Check the last SMS and enter the six digits again.',
        metadata: { attemptsRemaining: 4 },
      },
    };
  }

  if (!/^\d{6}$/.test(code)) {
    return {
      ok: false,
      status: 422,
      problem: {
        code: 'identity.otp.incorrect',
        title: 'That code did not match',
        status: 422,
        detail: 'The code is six digits.',
        errors: [{ pointer: '/code', code: 'identity.otp.incorrect' }],
      },
    };
  }

  return { ok: true, body: mockSession() };
}

export function mockSession(): AuthSessionInput {
  return {
    accessToken: 'mock.access.token',
    refreshToken: 'mock.refresh.token',
    expiresIn: 900,
    tokenType: 'Bearer',
    user: DEMO_USER,
    isNewUser: false,
    nextStep: null,
  };
}
