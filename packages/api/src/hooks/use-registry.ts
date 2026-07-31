'use client';

/**
 * Registry hooks.
 *
 * These are cached hard — five minutes — because registries change by the day, not by the second,
 * and every screen in the product asks for them. The taxonomy behind a filter rail must not cost a
 * round trip per navigation.
 */

import { useQuery } from '@tanstack/react-query';
import { useMemo } from 'react';
import { getApiConfig } from '../config';
import {
  getAttributes,
  getCities,
  getFeatures,
  getI18nBundle,
  getMakes,
  getModels,
  getTaxonomyTree,
  getVariants,
} from '../endpoints/platform';
import type {
  AttributesResponse,
  CitiesResponse,
  Features,
  I18nBundle,
  TaxonomyNodesResponse,
  TaxonomyTree,
} from '../schemas';
import { queryKeys } from './keys';

function registryStaleTime(): number {
  return getApiConfig().registryStaleMs;
}

export function useTaxonomyTree() {
  return useQuery<TaxonomyTree>({
    queryKey: queryKeys.taxonomyTree(),
    queryFn: () => getTaxonomyTree(),
    staleTime: registryStaleTime(),
  });
}

/** The attribute definitions a category exposes — form fields, facets and spec rows, all at once. */
export function useAttributes(category: string | undefined) {
  return useQuery<AttributesResponse>({
    queryKey: queryKeys.attributes(category ?? ''),
    queryFn: () => getAttributes(category as string),
    enabled: Boolean(category),
    staleTime: registryStaleTime(),
  });
}

export function useMakes(category?: string) {
  return useQuery<TaxonomyNodesResponse>({
    queryKey: queryKeys.makes(category),
    queryFn: () => getMakes(category),
    staleTime: registryStaleTime(),
  });
}

export function useModels(make: string | undefined) {
  return useQuery<TaxonomyNodesResponse>({
    queryKey: queryKeys.models(make ?? ''),
    queryFn: () => getModels(make as string),
    enabled: Boolean(make),
    staleTime: registryStaleTime(),
  });
}

export function useVariants(model: string | undefined, year?: number) {
  return useQuery<TaxonomyNodesResponse>({
    queryKey: queryKeys.variants(model ?? '', year),
    queryFn: () => getVariants(model as string, year),
    enabled: Boolean(model),
    staleTime: registryStaleTime(),
  });
}

/**
 * Module and flag booleans, already evaluated for this user in this city. The client never sees a
 * targeting rule, so `isEnabled('module.lease')` is the only question it can ask.
 */
export function useFeatures() {
  const query = useQuery<Features>({
    queryKey: queryKeys.features(),
    queryFn: () => getFeatures(),
    staleTime: registryStaleTime(),
  });

  const isModuleEnabled = (module: string): boolean => query.data?.modules[module] ?? false;
  const isFlagEnabled = (flag: string): boolean => query.data?.flags[flag] ?? false;

  return { ...query, isModuleEnabled, isFlagEnabled };
}

export function useCities(serviceableOnly = true) {
  return useQuery<CitiesResponse>({
    queryKey: queryKeys.cities(serviceableOnly),
    queryFn: () => getCities(serviceableOnly),
    staleTime: registryStaleTime(),
  });
}

/**
 * Localisation with the contract's fallback chain: requested locale → `en-IN` → the key itself.
 * `t` never returns an empty string, because an empty label is worse than an untranslated one.
 */
export function useI18n(locale?: string, namespace = 'web') {
  const active = locale ?? getApiConfig().defaultLocale;

  const primary = useQuery<I18nBundle>({
    queryKey: queryKeys.i18n(active, namespace),
    queryFn: () => getI18nBundle(active, namespace),
    staleTime: registryStaleTime(),
  });

  const fallback = useQuery<I18nBundle>({
    queryKey: queryKeys.i18n('en-IN', namespace),
    queryFn: () => getI18nBundle('en-IN', namespace),
    enabled: active !== 'en-IN',
    staleTime: registryStaleTime(),
  });

  const resources = useMemo(
    () => ({ ...(fallback.data?.resources ?? {}), ...(primary.data?.resources ?? {}) }),
    [fallback.data, primary.data],
  );

  const t = useMemo(
    () =>
      (key: string, fallbackText?: string): string =>
        resources[key] ?? fallbackText ?? key,
    [resources],
  );

  return {
    locale: active,
    resources,
    t,
    isLoading: primary.isLoading,
    isError: primary.isError,
    error: primary.error,
  };
}
