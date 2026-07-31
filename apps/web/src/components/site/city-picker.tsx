'use client';

import { useCities } from '@autohub/api';
import { Select } from '@autohub/ui';
import { useActiveCity } from '@/lib/use-active-city';

/**
 * Which cities exist comes from the geography registry. Opening a new service city is a row, and
 * this control picks it up on its next revalidation — there is no list of cities in this codebase.
 */
export function CityPicker() {
  const { data, isPending } = useCities(true);
  const [city, setCity] = useActiveCity();

  const options = (data?.items ?? []).map((c) => ({ value: c.code, label: c.name }));

  return (
    <Select
      value={city}
      onValueChange={setCity}
      options={options}
      disabled={isPending || options.length === 0}
      aria-label="Active city"
      placeholder={isPending ? 'Loading…' : 'Select city'}
      className="h-9 w-[8.5rem]"
    />
  );
}
