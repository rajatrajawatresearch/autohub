import type { Metadata } from 'next';
import { serverApi } from '@autohub/api/server';
import { EmptyState, MoneyValue, SectionHeading, StatusPill } from '@autohub/ui';

export const metadata: Metadata = { title: 'Leads' };
export const dynamic = 'force-dynamic';

/**
 * The pipeline stages are workflow-registry states, not columns hardcoded here.
 *
 * That is the difference between a CRM a dealer can shape to how they actually sell and one they
 * have to work around: adding "Awaiting finance approval" between two stages is a transition row,
 * and this page picks it up without a deploy.
 */
export default async function LeadsPage() {
  const [workflow, deals] = await Promise.all([
    serverApi.workflowDefinition('deal').catch(() => null),
    serverApi.deals({ limit: 60 }).catch(() => null),
  ]);

  const stages = (workflow?.states ?? []).filter((state) => !state.isTerminal);
  const items = deals?.items ?? [];

  return (
    <div className="mx-auto max-w-[var(--container-wide)] px-4 py-6">
      <SectionHeading
        eyebrow="Trade"
        title="Leads"
        description="Stages come from the deal workflow, so the pipeline matches how your branch actually sells."
        level={1}
      />

      {stages.length === 0 ? (
        <div className="mt-6">
          <EmptyState
            title="No pipeline configured"
            body="The deal workflow has not been published yet. An admin publishes it from the registry console."
            align="center"
          />
        </div>
      ) : (
        <div className="mt-6 grid gap-px overflow-x-auto border border-[color:var(--border-hairline)] bg-[color:var(--border-hairline)] [grid-template-columns:repeat(auto-fit,minmax(15rem,1fr))]">
          {stages.map((stage) => {
            const inStage = items.filter((deal) => deal.state?.code === stage.code);

            return (
              <section key={stage.code} className="min-w-[15rem] bg-[color:var(--surface-card)] p-3">
                <header className="flex items-center justify-between gap-2 pb-2">
                  <StatusPill category={stage.category} label={stage.name} size="sm" />
                  <span className="font-mono text-[0.6875rem] text-[color:var(--text-secondary)]">
                    {inStage.length}
                  </span>
                </header>

                <ul className="space-y-2">
                  {inStage.map((deal) => (
                    <li
                      key={deal.dealId}
                      className="rounded-[var(--radius-control)] border border-[color:var(--border-hairline)] p-2.5"
                    >
                      <p className="truncate text-body-sm text-[color:var(--text-primary)]">
                        {deal.listing.title}
                      </p>
                      <p className="mt-0.5 flex items-baseline justify-between gap-2">
                        <span className="font-mono text-[0.6875rem] text-[color:var(--text-secondary)]">
                          {deal.reference}
                        </span>
                        <MoneyValue
                          minorUnits={deal.amountMinor}
                          compact
                          size="xs"
                          tone="secondary"
                        />
                      </p>
                      {deal.counterparty ? (
                        <p className="mt-1 truncate font-mono text-[0.6875rem] text-[color:var(--text-secondary)]">
                          {deal.counterparty.displayName}
                        </p>
                      ) : null}
                    </li>
                  ))}

                  {inStage.length === 0 ? (
                    <li className="py-3 text-center text-body-sm text-[color:var(--text-secondary)]">
                      Nothing here
                    </li>
                  ) : null}
                </ul>
              </section>
            );
          })}
        </div>
      )}
    </div>
  );
}
