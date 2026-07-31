/**
 * Registry #3 — Workflow.
 *
 * Statuses, transitions and guards for listings, deals, auctions, bookings, loans and claims. The
 * server sends a `category`; the design system maps that to a `StatusPill` tone. The server never
 * sends a colour and the client never invents a state.
 */

import { z } from 'zod';
import { IdSchema, IsoDateTimeSchema } from './common';

/**
 * The tone vocabulary. Six values, each with one meaning:
 *   draft            — not yet submitted, the user still owns it
 *   awaiting_action  — waiting on somebody; usually carries an SLA
 *   in_progress      — moving, nobody is blocked
 *   blocked          — stopped by a hold, a dispute, or a missing document
 *   succeeded        — terminal, the good end
 *   failed           — terminal, the bad end
 * An unknown category renders as `in_progress` rather than as nothing.
 */
// These six are the backend's StateCategory enum (AutoHub.Platform/Workflow/WorkflowDefinition.cs)
// and nothing else is valid. `.catch` keeps an unrecognised value from failing the whole parse —
// a state the client has not heard of should render neutrally, not blank the page.
export const StateCategorySchema = z
  .enum(['draft', 'awaiting_action', 'in_progress', 'succeeded', 'failed', 'cancelled'])
  .catch('in_progress');
export type StateCategory = z.infer<typeof StateCategorySchema>;

export const WorkflowStateSchema = z.object({
  code: z.string(),
  name: z.string(),
  category: StateCategorySchema,
  description: z.string().nullish(),
  isInitial: z.boolean().default(false),
  isTerminal: z.boolean().default(false),
  /** Minutes allowed in this state before it is overdue. Drives the "due in 3h" line. */
  slaMinutes: z.number().int().positive().nullish(),
});
export type WorkflowState = z.infer<typeof WorkflowStateSchema>;

export const WorkflowTransitionSchema = z.object({
  actionCode: z.string(),
  label: z.string(),
  from: z.string(),
  to: z.string(),
  requiredPermission: z.string().nullish(),
  requiresReason: z.boolean().default(false),
  /** When set, the client renders this form from the form registry before firing the action. */
  formKey: z.string().nullish(),
});
export type WorkflowTransition = z.infer<typeof WorkflowTransitionSchema>;

export const WorkflowDefinitionSchema = z.object({
  key: z.string(),
  version: z.number().int(),
  /** `listings.listing`, `deals.deal`, `auction.lot`. */
  entityType: z.string(),
  states: z.array(WorkflowStateSchema),
  transitions: z.array(WorkflowTransitionSchema),
});
export type WorkflowDefinition = z.infer<typeof WorkflowDefinitionSchema>;

/**
 * The actions *this* user may fire right now. Guards and permissions were evaluated server-side;
 * the client renders buttons from this array and never re-derives authorisation.
 */
export const AvailableActionSchema = z.object({
  actionCode: z.string(),
  label: z.string(),
  requiresReason: z.boolean().default(false),
  requiresConfirmation: z.boolean().default(false),
  formKey: z.string().nullish(),
  /** `primary` marks the single consequential action — the one that may wear the signal yellow. */
  emphasis: z.enum(['primary', 'default', 'destructive']).catch('default').optional(),
});
export type AvailableAction = z.infer<typeof AvailableActionSchema>;

export const WorkflowHistoryEntrySchema = z.object({
  from: z.string().nullable(),
  to: z.string(),
  actionCode: z.string().nullable(),
  occurredAt: IsoDateTimeSchema,
  actor: z.string().nullish(),
  reason: z.string().nullish(),
  /** ISO-8601 duration, e.g. `PT17H10M`. Rendered as "17h 10m in Draft". */
  timeInPreviousState: z.string().nullish(),
});
export type WorkflowHistoryEntry = z.infer<typeof WorkflowHistoryEntrySchema>;

/** Live state of one entity. Returned by both the GET and the action POST — same shape, always. */
export const WorkflowSnapshotSchema = z.object({
  workflowKey: z.string(),
  currentState: WorkflowStateSchema,
  enteredAt: IsoDateTimeSchema,
  dueAt: IsoDateTimeSchema.nullish(),
  availableActions: z.array(AvailableActionSchema).default([]),
  history: z.array(WorkflowHistoryEntrySchema).default([]),
});
export type WorkflowSnapshot = z.infer<typeof WorkflowSnapshotSchema>;

export const WorkflowActionRequestSchema = z.object({
  actionCode: z.string(),
  reason: z.string().optional(),
  /** Payload for the transition's `formKey`, when it has one. */
  payload: z.record(z.unknown()).optional(),
});
export type WorkflowActionRequest = z.infer<typeof WorkflowActionRequestSchema>;

/** Identifies the entity whose workflow is being read or advanced. */
export const WorkflowTargetSchema = z.object({
  /** `listings`, `deals`, `auction`. */
  module: z.string(),
  /** `listings`, `deals`, `lots` — plural, kebab-case. */
  resource: z.string(),
  id: IdSchema,
});
export type WorkflowTarget = z.infer<typeof WorkflowTargetSchema>;
