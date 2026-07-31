import type { ReactNode } from 'react';
import { Badge, Button, SectionHeading } from '@autohub/ui';

/**
 * Every registry editor wears the same chrome, because every registry follows the same discipline:
 *
 *     edit a draft → validate → preview → publish a NEW version → rollback if it was wrong
 *
 * Published versions are immutable. That is not a UI convention; it is what stops a listing that
 * started on workflow v3 from waking up in a state v4 deleted. Showing the version and the
 * publisher on every one of these pages is how an operator learns to trust that.
 */
export function RegistryPage({
  eyebrow,
  title,
  description,
  version,
  publishedBy,
  publishedAt,
  isDraft = false,
  actions,
  children,
}: {
  eyebrow: string;
  title: string;
  description: string;
  version: number | string;
  publishedBy?: string;
  publishedAt?: string;
  isDraft?: boolean;
  actions?: ReactNode;
  children: ReactNode;
}) {
  return (
    <div className="mx-auto max-w-[var(--container-wide)] px-4 py-6">
      <SectionHeading
        eyebrow={eyebrow}
        title={title}
        description={description}
        level={1}
        action={
          actions ?? (
            <div className="flex gap-2">
              <Button variant="outline" size="sm">
                Validate
              </Button>
              <Button size="sm">Publish new version</Button>
            </div>
          )
        }
      />

      <div className="mt-4 flex flex-wrap items-center gap-3 border-y border-[color:var(--border-hairline)] py-2.5">
        <Badge variant={isDraft ? 'caution' : 'primary'} size="sm">
          {isDraft ? 'Draft' : `v${version}`}
        </Badge>
        <span className="font-mono text-[0.6875rem] text-[color:var(--text-secondary)]">
          {isDraft
            ? 'Not live — invisible to the platform until published'
            : `Published${publishedBy ? ` by ${publishedBy}` : ''}${publishedAt ? ` · ${publishedAt}` : ''}`}
        </span>
        <button
          type="button"
          className="ml-auto text-body-sm text-[color:var(--accent-primary)] underline underline-offset-4"
        >
          Version history
        </button>
      </div>

      <div className="mt-6">{children}</div>
    </div>
  );
}
