import type { ReactNode } from 'react';
import { cn } from '../lib/cn';
import { StatusPill, type StateCategory } from './status-pill';

/**
 * Any entity's state history, rendered from the workflow registry.
 *
 * The component knows nothing about listings, deals, loans or claims. It receives the workflow's
 * declared states (code → name + category) and the entity's transition history exactly as
 * `GET /{module}/{resource}/{id}/workflow` returns them. Add a state to the registry and it
 * appears here, correctly toned, without a deploy.
 *
 * A vertical hairline rail, one pill per entry, mono timestamps, and the dwell time in the
 * previous state — because the question a dealer actually asks is "how long did it sit there".
 */

export interface WorkflowStateDefinition {
  code: string;
  name: string;
  category: StateCategory;
}

export interface WorkflowHistoryEntry {
  from?: string | null;
  to: string;
  actionCode?: string | null;
  /** ISO-8601 UTC. Displayed in IST unless a timeZone is passed. */
  occurredAt: string;
  actor?: string | null;
  /** ISO-8601 duration, e.g. "PT17H10M". */
  timeInPreviousState?: string | null;
}

export interface WorkflowTimelineLabels {
  actor: string;
  dwell: string;
}

export interface WorkflowTimelineProps {
  entries: WorkflowHistoryEntry[];
  /** `states` from `GET /platform/workflows/{key}`. The only place a code becomes a name. */
  states: WorkflowStateDefinition[];
  order?: 'newest_first' | 'oldest_first';
  locale?: string;
  timeZone?: string;
  /** en-IN defaults; pass resolved strings from the localisation registry in production. */
  labels?: Partial<WorkflowTimelineLabels>;
  /** Shown when an entity has no history yet — an instruction, supplied by the caller. */
  empty?: ReactNode;
  className?: string;
}

const DEFAULT_LABELS: WorkflowTimelineLabels = {
  actor: 'By',
  dwell: 'Held',
};

/** "PT17H10M" → "17h 10m". Units are a technical invariant, not business vocabulary. */
function formatDwell(value: string): string | null {
  const match = /^P(?:(\d+)D)?(?:T(?:(\d+)H)?(?:(\d+)M)?(?:([\d.]+)S)?)?$/.exec(value.trim().toUpperCase());
  if (!match) return null;

  const [, days, hours, minutes, seconds] = match;
  const parts: string[] = [];
  if (days) parts.push(`${Number(days)}d`);
  if (hours) parts.push(`${Number(hours)}h`);
  if (minutes) parts.push(`${Number(minutes)}m`);
  if (parts.length === 0 && seconds) parts.push(`${Math.round(Number(seconds))}s`);

  return parts.length > 0 ? parts.join(' ') : null;
}

function formatStamp(iso: string, locale: string, timeZone: string): string {
  const date = new Date(iso);
  if (Number.isNaN(date.getTime())) return iso;
  return new Intl.DateTimeFormat(locale, { dateStyle: 'medium', timeStyle: 'short', timeZone }).format(date);
}

export function WorkflowTimeline({
  entries,
  states,
  order = 'newest_first',
  locale = 'en-IN',
  timeZone = 'Asia/Kolkata',
  labels,
  empty,
  className,
}: WorkflowTimelineProps) {
  const resolved = { ...DEFAULT_LABELS, ...labels };
  const byCode = new Map(states.map((state) => [state.code, state]));
  const ordered = order === 'newest_first' ? [...entries].reverse() : entries;

  if (ordered.length === 0) {
    return empty ? <div className={className}>{empty}</div> : null;
  }

  return (
    <ol className={cn('flex flex-col border-l border-[color:var(--border-hairline)] pl-4', className)}>
      {ordered.map((entry, index) => {
        const state = byCode.get(entry.to);
        const dwell = entry.timeInPreviousState ? formatDwell(entry.timeInPreviousState) : null;
        const isLatest = order === 'newest_first' ? index === 0 : index === ordered.length - 1;

        return (
          <li
            key={`${entry.to}-${entry.occurredAt}-${index}`}
            className="relative flex flex-col gap-1.5 pb-5 last:pb-0"
          >
            <span
              aria-hidden="true"
              className={cn(
                'absolute -left-[1.3125rem] top-[0.3125rem] size-2 border',
                isLatest
                  ? 'border-[color:var(--accent-primary)] bg-[var(--accent-primary)]'
                  : 'border-[color:var(--border-strong)] bg-[var(--surface-page)]',
              )}
            />

            <div className="flex flex-wrap items-center gap-2">
              <StatusPill
                category={state?.category ?? 'draft'}
                label={state?.name ?? entry.to}
                size="sm"
                title={entry.to}
              />
              {dwell ? (
                <span className="font-mono text-[length:var(--text-body-sm)] text-[color:var(--text-secondary)]">
                  {resolved.dwell} {dwell}
                </span>
              ) : null}
            </div>

            <div className="flex flex-wrap items-center gap-x-3 gap-y-0.5">
              <time
                dateTime={entry.occurredAt}
                className="font-mono text-[length:var(--text-body-sm)] text-[color:var(--text-primary)]"
              >
                {formatStamp(entry.occurredAt, locale, timeZone)}
              </time>
              {entry.actor ? (
                <span className="text-[length:var(--text-body-sm)] text-[color:var(--text-secondary)]">
                  {resolved.actor} {entry.actor}
                </span>
              ) : null}
            </div>
          </li>
        );
      })}
    </ol>
  );
}
