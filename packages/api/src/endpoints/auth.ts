/**
 * Auth. OTP-first; the token pair is handed to the shared token store so every later request
 * carries it without the caller thinking about headers.
 */

import { apiGet, apiPost, apiPostVoid, getDeviceId, setAuthTokens } from '../http';
import type { RequestContext } from '../http';
import { AuthSessionSchema, AuthUserSchema, OtpChallengeSchema } from '../schemas';
import type { AuthSession, AuthUser, OtpChallenge, OtpRequest, OtpVerifyRequest } from '../schemas';

export function requestOtp(
  request: OtpRequest,
  ctx: RequestContext = {},
): Promise<OtpChallenge> {
  return apiPost('/auth/otp/request', OtpChallengeSchema, request, { ...ctx, auth: false });
}

/** On success the session is stored, so nothing downstream has to be told about the token. */
export async function verifyOtp(
  request: OtpVerifyRequest,
  ctx: RequestContext = {},
): Promise<AuthSession> {
  const session = await apiPost(
    '/auth/otp/verify',
    AuthSessionSchema,
    { ...request, deviceId: request.deviceId ?? getDeviceId() },
    { ...ctx, auth: false },
  );
  setAuthTokens({
    accessToken: session.accessToken,
    refreshToken: session.refreshToken,
    expiresAt: Date.now() + session.expiresIn * 1000,
  });
  return session;
}

export function getMe(ctx: RequestContext = {}): Promise<AuthUser> {
  return apiGet('/auth/me', AuthUserSchema, ctx);
}

export async function logout(refreshToken: string, ctx: RequestContext = {}): Promise<void> {
  try {
    await apiPostVoid('/auth/logout', { refreshToken }, ctx);
  } finally {
    /* The local session ends whether or not the server acknowledged. */
    setAuthTokens(null);
  }
}
