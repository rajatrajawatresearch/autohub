'use client';

/**
 * The services a rendered form needs but must not own.
 *
 * A form renderer that fetched its own option lists would be a form renderer that knew about the
 * taxonomy API, and a form renderer that uploaded its own files would be one that knew about the
 * media pipeline. Both are injected here, so this package stays a pure function of registry JSON.
 */

import * as React from 'react';

import {
  createLabelResolver,
  type FieldDefinition,
  type FieldOption,
  type FileUploader,
  type FormLabels,
  type LabelResolver,
  type OptionsLoader,
} from './types';

export interface SchemaFormServices {
  t: LabelResolver;
  /** Supplied by the app — typically a TanStack Query fetch against the taxonomy/geo registries. */
  loadOptions?: OptionsLoader;
  /** Supplied by the app — presigned upload, resumable upload, whatever the media pipeline is. */
  uploader?: FileUploader;
  /** Density hint mirrors `<html data-density>`; controls only use it for grid sizing decisions. */
  compact: boolean;
}

const FALLBACK: SchemaFormServices = {
  t: createLabelResolver(),
  compact: false,
};

const ServicesContext = React.createContext<SchemaFormServices>(FALLBACK);

export interface SchemaFormServicesProviderProps {
  labels?: FormLabels;
  loadOptions?: OptionsLoader;
  uploader?: FileUploader;
  compact?: boolean;
  children: React.ReactNode;
}

export function SchemaFormServicesProvider({
  labels,
  loadOptions,
  uploader,
  compact,
  children,
}: SchemaFormServicesProviderProps): React.ReactElement {
  const value = React.useMemo<SchemaFormServices>(
    () => ({
      t: createLabelResolver(labels),
      loadOptions,
      uploader,
      compact: compact ?? false,
    }),
    [labels, loadOptions, uploader, compact],
  );

  return <ServicesContext.Provider value={value}>{children}</ServicesContext.Provider>;
}

export function useSchemaFormServices(): SchemaFormServices {
  return React.useContext(ServicesContext);
}

/** The chrome-string resolver. Field labels come from the registry; these are the renderer's own. */
export function useFormLabels(): LabelResolver {
  return React.useContext(ServicesContext).t;
}

export function useUploader(): FileUploader | undefined {
  return React.useContext(ServicesContext).uploader;
}

/* ── Options ───────────────────────────────────────────────────────────────── */

export interface FieldOptionsState {
  options: FieldOption[];
  loading: boolean;
  /** Set when the loader rejected. The control offers a retry rather than showing an empty list. */
  failed: boolean;
  reload: () => void;
}

export interface UseFieldOptionsArgs {
  field: FieldDefinition;
  /** Inline options from the registry row. Present means no fetch happens at all. */
  inline?: FieldOption[];
  /** Cascading controls name the level being loaded — `model`, `variant`, `city`. */
  level?: string;
  source?: string;
  params?: Record<string, unknown>;
  /** `false` while a parent level is unchosen — a model list without a make is meaningless. */
  enabled?: boolean;
}

function sortOptions(options: FieldOption[]): FieldOption[] {
  const anyOrdered = options.some((option) => typeof option.sortOrder === 'number');
  if (!anyOrdered) return options;
  return [...options].sort((a, b) => (a.sortOrder ?? 0) - (b.sortOrder ?? 0));
}

/**
 * Resolves a control's option list: inline from the registry row when present, otherwise through
 * the injected loader. Aborts in flight when the dependencies move — a dealer changing make three
 * times must not see the second answer land after the third.
 */
export function useFieldOptions({
  field,
  inline,
  level,
  source,
  params,
  enabled = true,
}: UseFieldOptionsArgs): FieldOptionsState {
  const { loadOptions } = useSchemaFormServices();
  const [state, setState] = React.useState<{ options: FieldOption[]; loading: boolean; failed: boolean }>({
    options: [],
    loading: false,
    failed: false,
  });
  const [attempt, setAttempt] = React.useState(0);

  const inlineOptions = React.useMemo(() => (inline ? sortOptions(inline) : undefined), [inline]);
  const paramsKey = React.useMemo(() => JSON.stringify(params ?? {}), [params]);
  const shouldFetch = inlineOptions === undefined && enabled && loadOptions !== undefined;

  React.useEffect(() => {
    if (!shouldFetch || !loadOptions) {
      setState({ options: [], loading: false, failed: false });
      return;
    }

    const controller = new AbortController();
    let live = true;
    setState((previous) => ({ ...previous, loading: true, failed: false }));

    void loadOptions({
      field,
      level,
      source,
      params: JSON.parse(paramsKey) as Record<string, unknown>,
      signal: controller.signal,
    })
      .then((options) => {
        if (!live) return;
        setState({ options: sortOptions(options), loading: false, failed: false });
      })
      .catch(() => {
        if (!live) return;
        setState({ options: [], loading: false, failed: true });
      });

    return () => {
      live = false;
      controller.abort();
    };
    // `field` is registry data and stable per render tree; the query identity is the params key.
  }, [shouldFetch, loadOptions, field, level, source, paramsKey, attempt]);

  const reload = React.useCallback(() => setAttempt((count) => count + 1), []);

  if (inlineOptions !== undefined) {
    return { options: inlineOptions, loading: false, failed: false, reload };
  }
  return { ...state, reload };
}
