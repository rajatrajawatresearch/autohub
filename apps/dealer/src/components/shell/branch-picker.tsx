'use client';

import { useState } from 'react';
import { useCities } from '@autohub/api';
import { Select } from '@autohub/ui';

/**
 * A dealer with three showrooms is one account with three branches, and almost every number in this
 * console — stock, leads, payouts — is scoped to the active one. It sits in the top bar rather than
 * inside each page so there is exactly one place to change it and no page can disagree.
 */
export function BranchPicker() {
  const { data } = useCities(true);
  const [branch, setBranch] = useState('pune');

  const options = (data?.items ?? []).map((city) => ({
    value: city.code,
    label: `${city.name} branch`,
  }));

  return (
    <Select
      value={branch}
      onValueChange={setBranch}
      options={options}
      triggerSize="sm"
      aria-label="Active branch"
      placeholder="Select branch"
      className="w-[11rem]"
    />
  );
}
