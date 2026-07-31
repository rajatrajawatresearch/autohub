/**
 * Auth — OTP-first, because India is phone-first.
 *
 * `GET /auth/me` returns resolved *permissions*, never roles-as-authorisation. The client shows
 * and hides on permissions; the server enforces on permissions. Role is a grouping, not a check.
 */

import { z } from 'zod';
import { IdSchema, IsoDateTimeSchema } from './common';

/** Why the OTP is being sent. Determines the SMS template and the throttle bucket. */
export const OtpPurposeSchema = z
  .enum(['login', 'signup', 'verify_phone', 'step_up', 'reset'])
  .catch('login');
export type OtpPurpose = z.infer<typeof OtpPurposeSchema>;

export const OtpRequestSchema = z.object({
  /** E.164. `+919812345678`. */
  phone: z.string().min(8),
  purpose: OtpPurposeSchema.optional(),
  channel: z.enum(['sms', 'whatsapp', 'voice']).optional(),
});
export type OtpRequest = z.infer<typeof OtpRequestSchema>;

export const OtpChallengeSchema = z.object({
  challengeId: IdSchema,
  expiresInSeconds: z.number().int().positive().default(300),
  resendAfterSeconds: z.number().int().nonnegative().default(30),
  /** `+91 98200 1****5` — shown so the user can confirm the number before waiting. */
  maskedTo: z.string().nullish(),
  /** Four characters echoed in the SMS body. If it does not match the screen, it is a scam call. */
  hint: z.string().nullish(),
  deliveryChannel: z.enum(['sms', 'whatsapp', 'voice']).catch('sms').optional(),
  attemptsAllowed: z.number().int().positive().default(5),
});
export type OtpChallenge = z.infer<typeof OtpChallengeSchema>;

export const OtpVerifyRequestSchema = z.object({
  challengeId: IdSchema,
  code: z.string().min(4),
  deviceId: z.string().optional(),
});
export type OtpVerifyRequest = z.infer<typeof OtpVerifyRequestSchema>;

export const AuthUserSchema = z.object({
  id: IdSchema,
  displayName: z.string(),
  phone: z.string().nullish(),
  email: z.string().nullish(),
  /** Grouping only — for display and audit. Never the basis of a client-side gate. */
  roles: z.array(z.string()).default([]),
  /** `listings.listing.create`, `auction.bid.place`. Show and hide on these. */
  permissions: z.array(z.string()).default([]),
  activeCity: z.string().nullish(),
  dealerId: IdSchema.nullish(),
  avatarUrl: z.string().nullish(),
  kycLevel: z.number().int().nonnegative().default(0),
});
export type AuthUser = z.infer<typeof AuthUserSchema>;

export const TokenPairSchema = z.object({
  accessToken: z.string(),
  refreshToken: z.string(),
  /** Seconds. 900 — fifteen minutes. */
  expiresIn: z.number().int().positive().default(900),
  tokenType: z.string().default('Bearer'),
});
export type TokenPair = z.infer<typeof TokenPairSchema>;

export const AuthSessionSchema = TokenPairSchema.extend({
  user: AuthUserSchema,
  isNewUser: z.boolean().default(false),
  /** `complete_profile`, `kyc_required` — the shell routes on this instead of guessing. */
  nextStep: z.string().nullish(),
});
export type AuthSession = z.infer<typeof AuthSessionSchema>;

export const RefreshRequestSchema = z.object({
  refreshToken: z.string(),
  deviceId: z.string().optional(),
});
export type RefreshRequest = z.infer<typeof RefreshRequestSchema>;

export const SessionInfoSchema = z.object({
  id: IdSchema,
  deviceId: z.string(),
  platform: z.string().nullish(),
  model: z.string().nullish(),
  city: z.string().nullish(),
  lastSeenAt: IsoDateTimeSchema,
  isCurrent: z.boolean().default(false),
});
export type SessionInfo = z.infer<typeof SessionInfoSchema>;
