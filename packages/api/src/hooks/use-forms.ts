'use client';

/**
 * Form Schema Registry hooks. The renderer asks for a key; what comes back is the whole form.
 */

import { useMutation, useQuery } from '@tanstack/react-query';
import { getFormDefinition, validateForm } from '../endpoints/platform';
import { getApiConfig } from '../config';
import type { FormDefinition, FormValidateResponse } from '../schemas';
import { queryKeys } from './keys';

/** `context` picks the per-category variant, e.g. `category:suv`. */
export function useFormDefinition(key: string | undefined, context?: string) {
  return useQuery<FormDefinition>({
    queryKey: queryKeys.form(key ?? '', context),
    queryFn: () => getFormDefinition(key as string, context),
    enabled: Boolean(key),
    staleTime: getApiConfig().registryStaleMs,
  });
}

/**
 * Server-side validation of a partial payload — the same schema the client just validated against,
 * checked again by the only party that can be trusted to.
 */
export function useValidateForm(key: string) {
  return useMutation<FormValidateResponse, Error, Record<string, unknown>>({
    mutationFn: (values) => validateForm(key, values),
  });
}
