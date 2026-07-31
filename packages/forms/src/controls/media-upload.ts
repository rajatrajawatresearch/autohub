'use client';

/**
 * Upload lifecycle for the `file` and `photo_set` controls.
 *
 * The value is the list. Progress, failure and retry all mutate entries of the same array the form
 * already holds, so a half-uploaded set survives a step change, a validation run and a browser
 * back — an inspector standing next to a car on 3G does not get to lose six photos because they
 * tapped Back.
 *
 * The transport itself is injected (`SchemaFormServices.uploader`). With no uploader present the
 * files stay local and `pending`, which is exactly what a Storybook or a test wants.
 */

import * as React from 'react';

import { useUploader } from '../form-context';
import { localId } from './shared';
import type { FieldDefinition, LabelResolver, MediaItem } from '../types';

export interface UseMediaUploadArgs {
  field: FieldDefinition;
  items: MediaItem[];
  onChange: (next: MediaItem[]) => void;
  t: LabelResolver;
}

export interface MediaUploadApi {
  add: (files: readonly File[], prompt?: string | null) => void;
  remove: (id: string) => void;
  retry: (id: string) => void;
  move: (id: string, delta: number) => void;
  /** Absolute reorder — what a drag knows, since a drop target knows its own index and not yours. */
  moveTo: (id: string, targetIndex: number) => void;
  assign: (id: string, prompt: string | null) => void;
  /** `true` while anything is still in flight — the step's Continue waits for it. */
  busy: boolean;
}

/** `accept` is a list of MIME types and/or extensions, exactly as an `<input accept>` takes them. */
function isAccepted(file: File, accept: readonly string[] | undefined): boolean {
  if (!accept || accept.length === 0) return true;
  const name = file.name.toLowerCase();
  const type = file.type.toLowerCase();
  return accept.some((entry) => {
    const pattern = entry.trim().toLowerCase();
    if (pattern === '') return true;
    if (pattern.startsWith('.')) return name.endsWith(pattern);
    if (pattern.endsWith('/*')) return type.startsWith(pattern.slice(0, -1));
    return type === pattern;
  });
}

export function useMediaUpload({ field, items, onChange, t }: UseMediaUploadArgs): MediaUploadApi {
  const uploader = useUploader();

  // Callbacks fire from promises long after the render that created them; the ref is what keeps
  // a progress tick from writing against a list that has since had two more photos added to it.
  const itemsRef = React.useRef(items);
  itemsRef.current = items;

  const filesRef = React.useRef(new Map<string, File>());
  const objectUrlsRef = React.useRef(new Set<string>());
  const abortRef = React.useRef(new Map<string, AbortController>());

  const patch = React.useCallback(
    (id: string, changes: Partial<MediaItem>) => {
      const next = itemsRef.current.map((item) => (item.id === id ? { ...item, ...changes } : item));
      itemsRef.current = next;
      onChange(next);
    },
    [onChange],
  );

  const releaseUrl = React.useCallback((url: string | undefined) => {
    if (!url || !objectUrlsRef.current.has(url)) return;
    URL.revokeObjectURL(url);
    objectUrlsRef.current.delete(url);
  }, []);

  // Object URLs are a process-wide leak if nobody revokes them, and an inspection run can create
  // two hundred of them in an afternoon.
  React.useEffect(() => {
    const urls = objectUrlsRef.current;
    const aborts = abortRef.current;
    return () => {
      for (const url of urls) URL.revokeObjectURL(url);
      urls.clear();
      for (const controller of aborts.values()) controller.abort();
      aborts.clear();
    };
  }, []);

  const upload = React.useCallback(
    (id: string) => {
      const file = filesRef.current.get(id);
      if (!uploader || !file) return;

      abortRef.current.get(id)?.abort();
      const controller = new AbortController();
      abortRef.current.set(id, controller);

      const item = itemsRef.current.find((entry) => entry.id === id);
      patch(id, { status: 'uploading', progress: 0, errorCode: undefined, errorMessage: undefined });

      void uploader({
        file,
        field,
        prompt: item?.prompt ?? null,
        onProgress: (percent) => {
          if (controller.signal.aborted) return;
          patch(id, { progress: Math.max(0, Math.min(100, Math.round(percent))) });
        },
        signal: controller.signal,
      })
        .then((result) => {
          if (controller.signal.aborted) return;
          patch(id, { status: 'uploaded', progress: 100, url: result.url, assetId: result.assetId });
        })
        .catch((error: unknown) => {
          if (controller.signal.aborted) return;
          const code = error instanceof Error && error.name === 'AbortError' ? undefined : `${field.key}.upload_failed`;
          if (code === undefined) return;
          patch(id, { status: 'failed', errorCode: code, errorMessage: t('form.file.failed') });
        })
        .finally(() => {
          abortRef.current.delete(id);
        });
    },
    [uploader, field, patch, t],
  );

  const add = React.useCallback(
    (files: readonly File[], prompt: string | null = null) => {
      if (files.length === 0) return;
      const config = field.config;
      const maxSize = config?.maxSizeMb;
      const accepted = config?.accept;

      const created: MediaItem[] = [];
      for (const file of files) {
        const id = localId();
        const previewUrl = file.type.startsWith('image/') ? URL.createObjectURL(file) : undefined;
        if (previewUrl) objectUrlsRef.current.add(previewUrl);

        // A rejected file still enters the list, visibly rejected. Silently dropping a photo a user
        // just chose is the worst possible answer — they will tap it again, and again.
        let status: MediaItem['status'] = 'pending';
        let errorCode: string | undefined;
        let errorMessage: string | undefined;

        if (!isAccepted(file, accepted)) {
          status = 'failed';
          errorCode = `${field.key}.wrong_type`;
          errorMessage = t('form.file.wrong_type');
        } else if (typeof maxSize === 'number' && file.size > maxSize * 1024 * 1024) {
          status = 'failed';
          errorCode = `${field.key}.too_large`;
          errorMessage = t('form.file.too_large', { value: maxSize });
        }

        filesRef.current.set(id, file);
        created.push({
          id,
          fileName: file.name,
          sizeBytes: file.size,
          contentType: file.type,
          prompt,
          previewUrl,
          status,
          progress: 0,
          errorCode,
          errorMessage,
        });
      }

      const next = [...itemsRef.current, ...created];
      itemsRef.current = next;
      onChange(next);

      for (const item of created) {
        if (item.status === 'pending') upload(item.id);
      }
    },
    [field, onChange, t, upload],
  );

  const remove = React.useCallback(
    (id: string) => {
      abortRef.current.get(id)?.abort();
      abortRef.current.delete(id);
      const target = itemsRef.current.find((item) => item.id === id);
      releaseUrl(target?.previewUrl);
      filesRef.current.delete(id);
      const next = itemsRef.current.filter((item) => item.id !== id);
      itemsRef.current = next;
      onChange(next);
    },
    [onChange, releaseUrl],
  );

  const retry = React.useCallback((id: string) => upload(id), [upload]);

  /** The first item is the cover, so order is information, not preference. */
  const moveTo = React.useCallback(
    (id: string, targetIndex: number) => {
      const current = itemsRef.current;
      const from = current.findIndex((item) => item.id === id);
      if (from < 0) return;
      const to = Math.max(0, Math.min(current.length - 1, targetIndex));
      if (to === from) return;
      const next = [...current];
      const [moved] = next.splice(from, 1);
      if (!moved) return;
      next.splice(to, 0, moved);
      itemsRef.current = next;
      onChange(next);
    },
    [onChange],
  );

  const move = React.useCallback(
    (id: string, delta: number) => {
      const from = itemsRef.current.findIndex((item) => item.id === id);
      if (from < 0) return;
      moveTo(id, from + delta);
    },
    [moveTo],
  );

  /** Points a shot at a prompted slot, evicting whatever held that slot back into the loose pile. */
  const assign = React.useCallback(
    (id: string, prompt: string | null) => {
      const next = itemsRef.current.map((item) => {
        if (item.id === id) return { ...item, prompt };
        if (prompt !== null && item.prompt === prompt) return { ...item, prompt: null };
        return item;
      });
      itemsRef.current = next;
      onChange(next);
    },
    [onChange],
  );

  const busy = items.some((item) => item.status === 'uploading');

  return { add, remove, retry, move, moveTo, assign, busy };
}
