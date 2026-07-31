import type { Metadata } from 'next';
import { serverApi } from '@autohub/api/server';
import { Badge, DataTable, EmptyState, type DataTableColumn } from '@autohub/ui';
import { RegistryPage } from '@/components/registry/registry-page';

export const metadata: Metadata = { title: 'Geography' };
export const revalidate = 300;

type City = { code: string; name: string; stateCode: string; serviceable: boolean; modules: string[] };

/** Opening a city is a row here. Nothing in the codebase contains a list of cities. */
export default async function GeoRegistryPage() {
  const cities = await serverApi.cities(false).catch(() => null);

  const columns: Array<DataTableColumn<City>> = [
    { id: 'code', header: 'Code', mono: true, width: '9rem', cell: (row) => row.code },
    { id: 'name', header: 'City', cell: (row) => row.name },
    { id: 'state', header: 'State', mono: true, width: '5rem', cell: (row) => row.stateCode },
    {
      id: 'serviceable',
      header: 'Serviceable',
      cell: (row) => (
        <Badge variant={row.serviceable ? 'clear' : 'neutral'} size="sm">
          {row.serviceable ? 'live' : 'not live'}
        </Badge>
      ),
    },
    {
      id: 'modules',
      header: 'Modules enabled here',
      hideBelow: 'md',
      cell: (row) => (
        <span className="flex flex-wrap gap-1">
          {row.modules.map((module) => (
            <Badge key={module} variant="outline" size="sm">{module}</Badge>
          ))}
          {row.modules.length === 0 ? <span className="text-[color:var(--text-secondary)]">—</span> : null}
        </span>
      ),
    },
  ];

  return (
    <RegistryPage
      eyebrow="Registry 10 of 10"
      title="Geography"
      description="States, cities, RTO offices and service areas. Search, pricing and features are all scoped by these."
      version={cities?.version ?? '—'}
      publishedBy="ops@autohub.in"
    >
      <DataTable
        columns={columns}
        rows={(cities?.items ?? []) as City[]}
        getRowId={(row) => row.code}
        caption="Cities in the geography registry"
        empty={<EmptyState title="The geography registry is unreachable" body="Start the API to edit cities." align="center" />}
      />
    </RegistryPage>
  );
}
