import type { Metadata } from 'next';
import { serverApi } from '@autohub/api/server';
import { Badge, EmptyState, FieldBlock, StatusPill } from '@autohub/ui';
import { RegistryPage } from '@/components/registry/registry-page';

export const metadata: Metadata = { title: 'Workflows' };
export const revalidate = 60;

/** The lifecycles the platform runs. Each is a key the engine loads by. */
const WORKFLOW_KEYS = [
  'listing',
  'deal',
  'auction_lot',
  'booking',
  'kyc_case',
  'loan_application',
] as const;

/**
 * The workflow designer.
 *
 * This is the page that makes "there is no status column anywhere" a fact rather than a slogan.
 * States and transitions are rows; the engine executes them; adding a "Legal Hold" state before
 * payout is done here and takes effect on the next published version.
 *
 * Publishing runs the engine's own validation first — unreachable state, dead end, missing terminal
 * state, unknown effect handler — because every one of those failures is otherwise discovered at
 * 2am with a customer's money in escrow.
 */
export default async function WorkflowRegistryPage({
  searchParams,
}: {
  searchParams: Promise<{ key?: string }>;
}) {
  const { key } = await searchParams;
  const selected = key ?? WORKFLOW_KEYS[0];
  const workflow = await serverApi.workflowDefinition(selected).catch(() => null);

  return (
    <RegistryPage
      eyebrow="Registry 3 of 10"
      title="Workflows"
      description="States, transitions, guards and effects. Published versions are immutable — in-flight entities keep running on the version they started."
      version={workflow?.version ?? '—'}
      publishedBy="ops@autohub.in"
    >
      <div className="grid gap-8 lg:grid-cols-[14rem_1fr]">
        <nav aria-label="Workflows">
          <p className="ah-label pb-2">Lifecycles</p>
          <ul className="divide-y divide-[color:var(--border-hairline)] border-y border-[color:var(--border-hairline)]">
            {WORKFLOW_KEYS.map((workflowKey) => (
              <li key={workflowKey}>
                <a
                  href={`/registry/workflows?key=${workflowKey}`}
                  aria-current={workflowKey === selected ? 'true' : undefined}
                  className={[
                    'flex h-[var(--density-control-h)] items-center border-l-2 px-3 font-mono text-body-sm transition-colors',
                    workflowKey === selected
                      ? 'border-[color:var(--accent-primary)] bg-[color:var(--surface-sunken)] font-semibold'
                      : 'border-transparent text-[color:var(--text-secondary)] hover:bg-[color:var(--surface-sunken)]',
                  ].join(' ')}
                >
                  {workflowKey}
                </a>
              </li>
            ))}
          </ul>
        </nav>

        {!workflow ? (
          <EmptyState
            title={`No published '${selected}' workflow`}
            body="Either the registry is unreachable or this lifecycle has never been published. An unpublished workflow is invisible to the engine."
            align="center"
          />
        ) : (
          <div>
            <section>
              <div className="flex flex-wrap items-baseline gap-3 pb-3">
                <h2 className="font-display text-heading">States</h2>
                <span className="font-mono text-[0.75rem] text-[color:var(--text-secondary)]">
                  {workflow.states.length} · entity {workflow.entityType}
                </span>
              </div>

              <ul className="grid gap-2 sm:grid-cols-2 xl:grid-cols-3">
                {workflow.states.map((state) => (
                  <li
                    key={state.code}
                    className="rounded-[var(--radius-card)] border border-[color:var(--border-hairline)] p-3"
                  >
                    <div className="flex items-center justify-between gap-2">
                      <StatusPill category={state.category} label={state.name} size="sm" />
                      <span className="flex gap-1">
                        {state.isInitial ? (
                          <Badge variant="info" size="sm">
                            initial
                          </Badge>
                        ) : null}
                        {state.isTerminal ? (
                          <Badge variant="neutral" size="sm">
                            terminal
                          </Badge>
                        ) : null}
                      </span>
                    </div>
                    <p className="mt-2 font-mono text-[0.6875rem] text-[color:var(--text-secondary)]">
                      {state.code}
                      {state.slaMinutes ? ` · SLA ${state.slaMinutes}m` : ''}
                    </p>
                  </li>
                ))}
              </ul>
            </section>

            <section className="mt-8">
              <h2 className="font-display text-heading pb-3">Transitions</h2>
              {/* Guards are deliberately absent from GET /platform/workflows/{key}: the condition
                  that decides whether a dealer may accept a deal is business logic, and shipping it
                  to a customer's phone hands it to anyone with devtools. Editing guards runs
                  against the admin registry endpoints, which require registry.workflow.publish. */}
              <p className="pb-3 text-body-sm text-[color:var(--text-secondary)]">
                Guard conditions are not served on the public workflow endpoint. Open a transition to
                edit its guard and effects.
              </p>
              <div className="overflow-x-auto">
                <table className="w-full border-collapse text-body-sm">
                  <thead>
                    <tr className="border-y border-[color:var(--border-hairline)]">
                      <th className="ah-label py-2 text-left">Action</th>
                      <th className="ah-label py-2 text-left">From</th>
                      <th className="ah-label py-2 text-left">To</th>
                      <th className="ah-label py-2 text-left">Permission</th>
                      <th className="ah-label py-2 text-left">Collects</th>
                      <th className="ah-label py-2 text-left">Reason</th>
                    </tr>
                  </thead>
                  <tbody className="divide-y divide-[color:var(--border-hairline)]">
                    {workflow.transitions.map((transition) => (
                      <tr key={`${transition.from}-${transition.actionCode}`}>
                        <td className="py-2 pr-3">
                          <span className="font-mono">{transition.actionCode}</span>
                          <span className="block text-[color:var(--text-secondary)]">
                            {transition.label}
                          </span>
                        </td>
                        <td className="py-2 pr-3 font-mono text-[0.75rem]">{transition.from}</td>
                        <td className="py-2 pr-3 font-mono text-[0.75rem]">→ {transition.to}</td>
                        <td className="py-2 pr-3 font-mono text-[0.75rem] text-[color:var(--text-secondary)]">
                          {transition.requiredPermission ?? '—'}
                        </td>
                        <td className="py-2 pr-3">
                          {transition.formKey ? (
                            <Badge variant="info" size="sm">
                              {transition.formKey}
                            </Badge>
                          ) : (
                            <span className="text-[color:var(--text-secondary)]">—</span>
                          )}
                        </td>
                        <td className="py-2">
                          {transition.requiresReason ? (
                            <Badge variant="caution" size="sm">
                              required
                            </Badge>
                          ) : (
                            <span className="text-[color:var(--text-secondary)]">—</span>
                          )}
                        </td>
                      </tr>
                    ))}
                  </tbody>
                </table>
              </div>
            </section>

            <section className="mt-8 border-t border-[color:var(--border-hairline)] pt-6">
              <h2 className="font-display text-heading pb-3">Publish checks</h2>
              <dl className="grid gap-6 sm:grid-cols-2 lg:grid-cols-4">
                <FieldBlock
                  label="Initial state"
                  value={workflow.states.filter((s) => s.isInitial).length === 1 ? 'Exactly one' : 'Invalid'}
                  mono
                />
                <FieldBlock
                  label="Terminal states"
                  value={String(workflow.states.filter((s) => s.isTerminal).length)}
                  mono
                  hint="Zero means entities could never finish."
                />
                <FieldBlock
                  label="Unreachable states"
                  value={String(countUnreachable(workflow))}
                  mono
                  hint="A state nobody can reach is dead configuration someone is relying on."
                />
                <FieldBlock
                  label="Dead ends"
                  value={String(countDeadEnds(workflow))}
                  mono
                  hint="Non-terminal states with no way out."
                />
              </dl>
            </section>
          </div>
        )}
      </div>
    </RegistryPage>
  );
}

type Workflow = NonNullable<Awaited<ReturnType<typeof serverApi.workflowDefinition>>>;

/** Mirrors the engine's own reachability check so the console agrees with the server. */
function countUnreachable(workflow: Workflow): number {
  const initial = workflow.states.find((state) => state.isInitial);
  if (!initial) return workflow.states.length;

  const reachable = new Set<string>([initial.code]);
  const frontier = [initial.code];

  while (frontier.length > 0) {
    const current = frontier.pop();
    for (const transition of workflow.transitions.filter((t) => t.from === current)) {
      if (!reachable.has(transition.to)) {
        reachable.add(transition.to);
        frontier.push(transition.to);
      }
    }
  }

  return workflow.states.filter((state) => !reachable.has(state.code)).length;
}

function countDeadEnds(workflow: Workflow): number {
  return workflow.states.filter(
    (state) => !state.isTerminal && !workflow.transitions.some((t) => t.from === state.code),
  ).length;
}
