'use client';

/**
 * Workflow hooks.
 *
 * `useFireWorkflowAction` is optimistic with a real rollback: the buttons disappear the instant the
 * action is fired (so nobody approves twice on a slow connection) and, when the definition is to
 * hand, the pill moves to the destination state. If the server disagrees — most often
 * `409 platform.workflow.transition_not_allowed` because somebody else acted first — the previous
 * snapshot is put back and the query refetches, so the screen ends up showing the truth rather
 * than an apology.
 */

import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query';
import { fireWorkflowAction, getWorkflowDefinition, getWorkflowSnapshot } from '../endpoints/platform';
import { getApiConfig } from '../config';
import type {
  WorkflowActionRequest,
  WorkflowDefinition,
  WorkflowSnapshot,
  WorkflowTarget,
} from '../schemas';
import { queryKeys } from './keys';

export function useWorkflowDefinition(key: string | undefined) {
  return useQuery<WorkflowDefinition>({
    queryKey: queryKeys.workflowDefinition(key ?? ''),
    queryFn: () => getWorkflowDefinition(key as string),
    enabled: Boolean(key),
    staleTime: getApiConfig().registryStaleMs,
  });
}

/** The live state of one entity, plus the actions this user may fire right now. */
export function useWorkflow(target: WorkflowTarget | undefined) {
  return useQuery<WorkflowSnapshot>({
    queryKey: queryKeys.workflow(target ?? { module: '', resource: '', id: '' }),
    queryFn: () => getWorkflowSnapshot(target as WorkflowTarget),
    enabled: Boolean(target?.id),
    /* Somebody else can move this underneath us; fifteen seconds is close enough to live. */
    staleTime: 15_000,
  });
}

interface FireContext {
  previous: WorkflowSnapshot | undefined;
}

/**
 * @param definition Optional. Supply it — `useWorkflowDefinition` already has it cached — and the
 * optimistic update can move the state pill to the transition's destination instead of only
 * disabling the buttons.
 */
export function useFireWorkflowAction(target: WorkflowTarget, definition?: WorkflowDefinition) {
  const queryClient = useQueryClient();
  const key = queryKeys.workflow(target);

  return useMutation<WorkflowSnapshot, Error, WorkflowActionRequest, FireContext>({
    mutationFn: (request) => fireWorkflowAction(target, request),

    onMutate: async (request) => {
      await queryClient.cancelQueries({ queryKey: key });
      const previous = queryClient.getQueryData<WorkflowSnapshot>(key);

      if (previous) {
        const transition = definition?.transitions.find(
          (t) => t.actionCode === request.actionCode && t.from === previous.currentState.code,
        );
        const destination = transition
          ? definition?.states.find((s) => s.code === transition.to)
          : undefined;

        queryClient.setQueryData<WorkflowSnapshot>(key, {
          ...previous,
          currentState: destination ?? previous.currentState,
          enteredAt: destination ? new Date().toISOString() : previous.enteredAt,
          /* No buttons while it is in flight. A double-fire is the failure this prevents. */
          availableActions: [],
        });
      }

      return { previous };
    },

    onError: (_error, _request, context) => {
      if (context?.previous) queryClient.setQueryData(key, context.previous);
    },

    onSuccess: (snapshot) => {
      queryClient.setQueryData(key, snapshot);
    },

    onSettled: () => {
      void queryClient.invalidateQueries({ queryKey: key });
    },
  });
}
