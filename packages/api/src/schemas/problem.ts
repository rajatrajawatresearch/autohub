/**
 * RFC 9457 Problem Details plus the AutoHub extension members.
 *
 * `code` is the only member a client may branch on. `title` and `detail` are human text resolved
 * through the i18n registry and may be reworded at any time.
 */

import { z } from 'zod';

/** Field-level failure, addressed by JSON Pointer into the request body. */
export const ProblemFieldErrorSchema = z.object({
  pointer: z.string().optional(),
  code: z.string(),
  message: z.string().optional(),
  messageKey: z.string().optional(),
  params: z.record(z.unknown()).optional(),
});
export type ProblemFieldError = z.infer<typeof ProblemFieldErrorSchema>;

export const ProblemDetailsSchema = z.object({
  type: z.string().optional(),
  title: z.string().optional(),
  status: z.number().int().optional(),
  /** Stable machine code, `{module}.{resource}.{condition}`. Branch on this and nothing else. */
  code: z.string().optional(),
  detail: z.string().optional(),
  instance: z.string().optional(),
  traceId: z.string().optional(),
  requestId: z.string().optional(),
  timestamp: z.string().optional(),
  /**
   * Present on 400/403/409/422. `SchemaForm` attaches each entry to a field by `pointer`,
   * including dynamic attributes (`/attributes/boom_length_m`).
   */
  errors: z.array(ProblemFieldErrorSchema).optional(),
  /**
   * Carries whatever the condition needs the client to know: `currentState` on a workflow
   * conflict, `fieldErrors` on a form validation failure, `retryAfterSeconds` on a throttle.
   */
  metadata: z.record(z.unknown()).optional(),
  retryable: z.boolean().optional(),
  retryAfterSeconds: z.number().nullish(),
  helpUrl: z.string().optional(),
  supportReference: z.string().optional(),
});
export type ProblemDetails = z.infer<typeof ProblemDetailsSchema>;

/** `422` from `POST /platform/forms/{key}/validate` carries this in `metadata`. */
export const FieldErrorMapSchema = z.record(z.array(z.string()));
export type FieldErrorMap = z.infer<typeof FieldErrorMapSchema>;

/**
 * Codes this package raises itself, so callers branch on one vocabulary whether the failure came
 * from the server, the network, or the client's own deadline.
 */
export const CLIENT_ERROR_CODES = {
  timeout: 'common.network.timeout',
  unreachable: 'common.network.unreachable',
  aborted: 'common.network.aborted',
  malformedResponse: 'common.response.malformed',
  unauthenticated: 'identity.token.expired',
} as const;

export type ClientErrorCode = (typeof CLIENT_ERROR_CODES)[keyof typeof CLIENT_ERROR_CODES];
