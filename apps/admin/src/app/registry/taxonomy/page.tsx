import type { Metadata } from 'next';
import { serverApi } from '@autohub/api/server';
import { Badge, DataTable, EmptyState, FieldBlock, type DataTableColumn } from '@autohub/ui';
import type { TaxonomyAttribute, TaxonomyCategory } from '@autohub/api';
import { RegistryPage } from '@/components/registry/registry-page';

export const metadata: Metadata = { title: 'Taxonomy' };
export const revalidate = 60;

/**
 * The registry the whole architecture is named after.
 *
 * A category's attributes are what become sell-form fields, search facets and spec rows. So adding
 * "Construction Equipment" with a `boom_length` attribute — here, in this UI — puts a new field on
 * the sell flow, a new facet on search, and a new row on every spec strip, on web and on both
 * mobile apps, with nothing deployed. That claim is either true on this page or it is marketing.
 */
export default async function TaxonomyRegistryPage({
  searchParams,
}: {
  searchParams: Promise<{ category?: string }>;
}) {
  const { category } = await searchParams;
  const tree = await serverApi.taxonomyTree().catch(() => null);

  const categories = tree?.categories ?? [];
  const selectedCode = category ?? categories[0]?.code;
  const attributes = selectedCode
    ? await serverApi.attributes(selectedCode).catch(() => null)
    : null;

  return (
    <RegistryPage
      eyebrow="Registry 1 of 10"
      title="Taxonomy"
      description="Categories, makes, models, variants — and the attributes each category exposes."
      version={tree?.version ?? '—'}
      publishedBy="ops@autohub.in"
    >
      {categories.length === 0 ? (
        <EmptyState
          title="The taxonomy registry is unreachable"
          body="Start the API, or check the registry cache. Nothing here is editable while it is down."
          align="center"
        />
      ) : (
        <div className="grid gap-8 lg:grid-cols-[16rem_1fr]">
          <nav aria-label="Categories">
            <p className="ah-label pb-2">Categories</p>
            <ul className="divide-y divide-[color:var(--border-hairline)] border-y border-[color:var(--border-hairline)]">
              {categories.map((node) => (
                <CategoryRow key={node.code} node={node} selected={selectedCode} depth={0} />
              ))}
            </ul>
            <p className="mt-3 text-body-sm text-[color:var(--text-secondary)]">
              A child inherits its parent&rsquo;s attributes and may add its own.
            </p>
          </nav>

          <section>
            <div className="flex flex-wrap items-baseline gap-3 pb-3">
              <h2 className="font-display text-heading">Attributes on {selectedCode}</h2>
              <span className="font-mono text-[0.75rem] text-[color:var(--text-secondary)]">
                {attributes?.attributes.length ?? 0} defined
              </span>
            </div>

            <AttributeTable attributes={attributes?.attributes ?? []} />

            <dl className="mt-8 grid gap-6 sm:grid-cols-3">
              <FieldBlock
                label="Searchable"
                value={`${(attributes?.attributes ?? []).filter((a) => a.searchable).length} of ${attributes?.attributes.length ?? 0}`}
                mono
                hint="A searchable attribute becomes an OpenSearch facet automatically."
              />
              <FieldBlock
                label="Required"
                value={String((attributes?.attributes ?? []).filter((a) => a.required).length)}
                mono
                hint="Enforced by the sell form and by bulk upload alike."
              />
              <FieldBlock
                label="Registry version"
                value={String(tree?.version ?? '—')}
                mono
                hint="Clients cache against this and refetch when it moves."
              />
            </dl>
          </section>
        </div>
      )}
    </RegistryPage>
  );
}

function CategoryRow({
  node,
  selected,
  depth,
}: {
  node: TaxonomyCategory;
  selected?: string;
  depth: number;
}) {
  const active = node.code === selected;

  return (
    <>
      <li>
        <a
          href={`/registry/taxonomy?category=${node.code}`}
          aria-current={active ? 'true' : undefined}
          className={[
            'flex h-[var(--density-control-h)] items-center border-l-2 pr-3 text-body-sm transition-colors',
            active
              ? 'border-[color:var(--accent-primary)] bg-[color:var(--surface-sunken)] font-semibold'
              : 'border-transparent text-[color:var(--text-secondary)] hover:bg-[color:var(--surface-sunken)]',
          ].join(' ')}
          style={{ paddingLeft: `${0.75 + depth * 0.875}rem` }}
        >
          {node.name}
          <span className="ml-auto font-mono text-[0.6875rem] opacity-70">
            {node.attributes.length || ''}
          </span>
        </a>
      </li>
      {node.children.map((child) => (
        <CategoryRow key={child.code} node={child} selected={selected} depth={depth + 1} />
      ))}
    </>
  );
}

function AttributeTable({ attributes }: { attributes: TaxonomyAttribute[] }) {
  const columns: Array<DataTableColumn<TaxonomyAttribute>> = [
    { id: 'code', header: 'Code', mono: true, width: '12rem', cell: (row) => row.code },
    { id: 'label', header: 'Label', cell: (row) => row.label },
    { id: 'type', header: 'Type', mono: true, cell: (row) => row.dataType },
    { id: 'unit', header: 'Unit', mono: true, hideBelow: 'md', cell: (row) => row.unit ?? '—' },
    {
      id: 'facet',
      header: 'Facet',
      hideBelow: 'md',
      cell: (row) =>
        row.searchable ? (
          <Badge variant="clear" size="sm">
            {row.facetStyle ?? 'list'}
          </Badge>
        ) : (
          <span className="text-[color:var(--text-secondary)]">—</span>
        ),
    },
    {
      id: 'required',
      header: 'Required',
      align: 'end',
      cell: (row) =>
        row.required ? (
          <Badge variant="caution" size="sm">
            Yes
          </Badge>
        ) : (
          <span className="text-[color:var(--text-secondary)]">No</span>
        ),
    },
    {
      id: 'options',
      header: 'Options',
      align: 'end',
      mono: true,
      hideBelow: 'lg',
      cell: (row) => (row.options?.length ? String(row.options.length) : '—'),
    },
  ];

  return (
    <DataTable
      columns={columns}
      rows={attributes}
      getRowId={(row) => row.code}
      caption="Attributes exposed by the selected category"
      empty={
        <EmptyState
          title="This category defines no attributes of its own"
          body="It still inherits everything its parent exposes. Add one to give it a field of its own."
          align="center"
        />
      }
    />
  );
}
