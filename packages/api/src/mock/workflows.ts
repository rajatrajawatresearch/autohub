/**
 * Workflow Registry fixtures — definitions plus a snapshot builder.
 *
 * Nobody greps the codebase for a status string to find out what states exist. They read this
 * registry, and in production they read the database behind it.
 */

import type { z } from 'zod';
import type { WorkflowDefinitionSchema, WorkflowSnapshotSchema } from '../schemas';

type WorkflowDefinitionInput = z.input<typeof WorkflowDefinitionSchema>;
type WorkflowSnapshotInput = z.input<typeof WorkflowSnapshotSchema>;

const LISTING: WorkflowDefinitionInput = {
  key: 'listing',
  version: 3,
  entityType: 'listings.listing',
  states: [
    { code: 'draft', name: 'Draft', category: 'draft', isInitial: true },
    {
      code: 'pending_review',
      name: 'Under review',
      category: 'awaiting_action',
      slaMinutes: 240,
      description: 'Moderation checks the photos against the registration record.',
    },
    { code: 'live', name: 'Live', category: 'in_progress' },
    { code: 'reserved', name: 'Reserved', category: 'blocked', description: 'A deal holds it.' },
    { code: 'sold', name: 'Sold', category: 'succeeded', isTerminal: true },
    { code: 'expired', name: 'Expired', category: 'failed', isTerminal: true },
    { code: 'rejected', name: 'Rejected', category: 'failed', isTerminal: true },
    { code: 'withdrawn', name: 'Withdrawn', category: 'failed', isTerminal: true },
  ],
  transitions: [
    {
      actionCode: 'submit',
      label: 'Submit for review',
      from: 'draft',
      to: 'pending_review',
      requiredPermission: 'listings.listing.submit',
    },
    {
      actionCode: 'approve',
      label: 'Approve',
      from: 'pending_review',
      to: 'live',
      requiredPermission: 'listings.listing.moderate',
    },
    {
      actionCode: 'reject',
      label: 'Reject',
      from: 'pending_review',
      to: 'rejected',
      requiredPermission: 'listings.listing.moderate',
      requiresReason: true,
    },
    {
      actionCode: 'withdraw',
      label: 'Withdraw listing',
      from: 'live',
      to: 'withdrawn',
      requiredPermission: 'listings.listing.update',
      requiresReason: true,
    },
    {
      actionCode: 'mark_sold',
      label: 'Mark sold',
      from: 'reserved',
      to: 'sold',
      requiredPermission: 'listings.listing.update',
    },
  ],
};

const AUCTION_LOT: WorkflowDefinitionInput = {
  key: 'auction_lot',
  version: 2,
  entityType: 'auction.lot',
  states: [
    { code: 'scheduled', name: 'Scheduled', category: 'draft', isInitial: true },
    { code: 'live', name: 'Live', category: 'in_progress' },
    { code: 'paused', name: 'Paused', category: 'blocked' },
    { code: 'reserve_not_met', name: 'Reserve not met', category: 'awaiting_action' },
    { code: 'sold', name: 'Sold', category: 'succeeded', isTerminal: true },
    { code: 'unsold', name: 'Unsold', category: 'failed', isTerminal: true },
  ],
  transitions: [
    {
      actionCode: 'open',
      label: 'Open lot',
      from: 'scheduled',
      to: 'live',
      requiredPermission: 'auction.auction.moderate',
    },
    {
      actionCode: 'pause',
      label: 'Pause',
      from: 'live',
      to: 'paused',
      requiredPermission: 'auction.auction.moderate',
      requiresReason: true,
    },
    {
      actionCode: 'accept_highest',
      label: 'Accept highest bid',
      from: 'reserve_not_met',
      to: 'sold',
      requiredPermission: 'auction.auction.moderate',
    },
  ],
};

const DEAL: WorkflowDefinitionInput = {
  key: 'deal',
  version: 4,
  entityType: 'deals.deal',
  states: [
    { code: 'offer_made', name: 'Offer made', category: 'awaiting_action', isInitial: true, slaMinutes: 1440 },
    { code: 'accepted', name: 'Accepted', category: 'in_progress' },
    { code: 'payment_pending', name: 'Payment pending', category: 'awaiting_action', slaMinutes: 2880 },
    { code: 'escrow_funded', name: 'In escrow', category: 'in_progress' },
    { code: 'rc_transfer', name: 'RC transfer', category: 'in_progress' },
    { code: 'on_hold', name: 'On hold', category: 'blocked' },
    { code: 'completed', name: 'Completed', category: 'succeeded', isTerminal: true },
    { code: 'cancelled', name: 'Cancelled', category: 'failed', isTerminal: true },
  ],
  transitions: [
    {
      actionCode: 'accept',
      label: 'Accept offer',
      from: 'offer_made',
      to: 'accepted',
      requiredPermission: 'deals.deal.accept',
    },
    {
      actionCode: 'decline',
      label: 'Decline offer',
      from: 'offer_made',
      to: 'cancelled',
      requiredPermission: 'deals.deal.accept',
      requiresReason: true,
    },
    {
      actionCode: 'confirm_payment',
      label: 'Confirm payment',
      from: 'payment_pending',
      to: 'escrow_funded',
      requiredPermission: 'deals.deal.update',
    },
    {
      actionCode: 'start_rc_transfer',
      label: 'Start RC transfer',
      from: 'escrow_funded',
      to: 'rc_transfer',
      requiredPermission: 'deals.deal.update',
    },
  ],
};

export const MOCK_WORKFLOWS: Record<string, WorkflowDefinitionInput> = {
  listing: LISTING,
  auction_lot: AUCTION_LOT,
  deal: DEAL,
};

/** Maps a module + resource pair to the workflow that governs it. */
export const WORKFLOW_BY_RESOURCE: Record<string, string> = {
  'listings': 'listing',
  'auction/lots': 'auction_lot',
  'deals/deals': 'deal',
};

function minutesAgo(minutes: number): string {
  return new Date(Date.now() - minutes * 60_000).toISOString();
}

function minutesAhead(minutes: number): string {
  return new Date(Date.now() + minutes * 60_000).toISOString();
}

/**
 * Builds a live snapshot for an entity sitting in `stateCode`. Available actions are the
 * transitions out of that state — in production the server filters these by permission and guard;
 * here every transition is offered so all three portals have something to click.
 */
export function mockWorkflowSnapshot(
  workflowKey: string,
  stateCode: string,
): WorkflowSnapshotInput | null {
  const definition = MOCK_WORKFLOWS[workflowKey];
  if (!definition) return null;

  const state = definition.states.find((s) => s.code === stateCode) ?? definition.states[0];
  if (!state) return null;

  const initial = definition.states.find((s) => s.isInitial) ?? definition.states[0];
  const outgoing = definition.transitions.filter((t) => t.from === state.code);

  const history: NonNullable<WorkflowSnapshotInput['history']> = [
    {
      from: null,
      to: initial?.code ?? state.code,
      actionCode: null,
      occurredAt: minutesAgo(2880),
      actor: 'You',
    },
  ];

  if (initial && state.code !== initial.code) {
    const arriving = definition.transitions.find((t) => t.to === state.code);
    history.push({
      from: initial.code,
      to: state.code,
      actionCode: arriving?.actionCode ?? null,
      occurredAt: minutesAgo(180),
      actor: 'You',
      timeInPreviousState: 'PT45H',
    });
  }

  return {
    workflowKey,
    currentState: state,
    enteredAt: minutesAgo(180),
    dueAt: state.slaMinutes ? minutesAhead(state.slaMinutes - 180) : null,
    availableActions: outgoing.map((t, index) => ({
      actionCode: t.actionCode,
      label: t.label,
      requiresReason: t.requiresReason ?? false,
      requiresConfirmation: t.requiresReason ?? false,
      emphasis: index === 0 ? 'primary' : 'default',
    })),
    history,
  };
}
