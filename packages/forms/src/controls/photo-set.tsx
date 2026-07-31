'use client';

import * as React from 'react';
import { Button, cn } from '@autohub/ui';

import { useFormLabels } from '../form-context';
import { useMediaUpload, type MediaUploadApi } from './media-upload';
import { asMediaItems } from './shared';
import type { FieldProps, LabelResolver, MediaItem } from '../types';

/**
 * The sell flow's core.
 *
 * A seller with a phone in a parking lot does not know what a listing needs, so the control tells
 * them: one slot per prompt from the registry — "Front 3/4", "Odometer", "Tyres" — each a labelled
 * empty frame that is obviously waiting for something. That is the difference between six usable
 * photos and eleven pictures of the same bumper.
 *
 * Order is information: the first item is the cover, so reordering is a real operation and it is
 * available by drag *and* by button, because a one-handed user outdoors cannot drag anything.
 *
 * The prompt list, the minimum, the maximum and the accepted types all come from `config`. This
 * file has no opinion about what a car should be photographed from.
 */
export function PhotoSetControl({
  field,
  value,
  onChange,
  onBlur,
  name,
  inputId,
  describedBy,
  invalid,
  disabled,
}: FieldProps) {
  const t = useFormLabels();
  const items = asMediaItems(value);
  const config = field.config;
  const prompts = config?.prompts ?? [];
  const min = config?.min;
  const max = config?.max;
  const full = typeof max === 'number' && items.length >= max;

  const inputRef = React.useRef<HTMLInputElement>(null);
  const targetPrompt = React.useRef<string | null>(null);
  const [dragging, setDragging] = React.useState(false);

  const media = useMediaUpload({
    field,
    items,
    onChange: (next) => onChange(next.length === 0 ? undefined : next),
    t,
  });

  const openPicker = React.useCallback((prompt: string | null) => {
    targetPrompt.current = prompt;
    inputRef.current?.click();
  }, []);

  const acceptFiles = React.useCallback(
    (files: FileList | null, prompt: string | null) => {
      if (!files || files.length === 0) return;
      const room = typeof max === 'number' ? Math.max(0, max - items.length) : files.length;
      const list = Array.from(files).slice(0, room);
      if (list.length === 0) return;
      // A drop onto a named slot claims that slot; the rest of the batch lands loose.
      const [first, ...rest] = list;
      if (first) media.add([first], prompt);
      if (rest.length > 0) media.add(rest, null);
    },
    [items.length, max, media],
  );

  const assigned = new Set(items.map((item) => item.prompt).filter((p): p is string => typeof p === 'string'));
  const extras = items.filter((item) => item.prompt == null || !prompts.includes(item.prompt));
  const openPrompts = prompts.filter((prompt) => !assigned.has(prompt));

  return (
    <div
      className="grid gap-4"
      onBlur={onBlur}
      onDragOver={(event) => {
        if (disabled) return;
        event.preventDefault();
        setDragging(true);
      }}
      onDragLeave={() => setDragging(false)}
      onDrop={(event) => {
        setDragging(false);
        if (disabled || event.dataTransfer.files.length === 0) return;
        event.preventDefault();
        acceptFiles(event.dataTransfer.files, null);
      }}
    >
      <input
        ref={inputRef}
        id={inputId}
        name={name}
        type="file"
        multiple
        accept={config?.accept?.join(',') ?? 'image/*'}
        disabled={disabled}
        aria-describedby={describedBy}
        aria-invalid={invalid || undefined}
        className="sr-only"
        onChange={(event) => {
          acceptFiles(event.target.files, targetPrompt.current);
          targetPrompt.current = null;
          event.target.value = '';
        }}
      />

      <div className="flex flex-wrap items-center justify-between gap-3">
        <p className="ah-label" aria-live="polite">
          {typeof min === 'number'
            ? t('form.photo.count', { count: items.length, min })
            : t('form.multiselect.count', { count: items.length })}
        </p>
        <Button
          size="sm"
          variant="outline"
          disabled={disabled || full || field.readOnly === true}
          onClick={() => openPicker(null)}
        >
          {t('form.photo.add')}
        </Button>
      </div>

      {prompts.length > 0 ? (
        <section className="grid gap-2">
          <h4 className="ah-label">{t('form.photo.required_shots')}</h4>
          <div className="grid grid-cols-2 gap-2 sm:grid-cols-3 lg:grid-cols-4">
            {prompts.map((prompt) => {
              const item = items.find((entry) => entry.prompt === prompt);
              if (!item) {
                return (
                  <EmptySlot
                    key={prompt}
                    prompt={prompt}
                    disabled={disabled === true || full}
                    onPick={() => openPicker(prompt)}
                    onFiles={(files) => acceptFiles(files, prompt)}
                    t={t}
                  />
                );
              }
              return (
                <PhotoTile
                  key={item.id}
                  item={item}
                  index={items.indexOf(item)}
                  total={items.length}
                  openPrompts={openPrompts}
                  media={media}
                  disabled={disabled === true}
                  t={t}
                />
              );
            })}
          </div>
        </section>
      ) : null}

      {extras.length > 0 || prompts.length === 0 ? (
        <section className="grid gap-2">
          {prompts.length > 0 ? <h4 className="ah-label">{t('form.photo.more_shots')}</h4> : null}
          <div className="grid grid-cols-2 gap-2 sm:grid-cols-3 lg:grid-cols-4">
            {extras.map((item) => (
              <PhotoTile
                key={item.id}
                item={item}
                index={items.indexOf(item)}
                total={items.length}
                openPrompts={openPrompts}
                media={media}
                disabled={disabled === true}
                t={t}
              />
            ))}
            {!full ? (
              <EmptySlot
                prompt={null}
                disabled={disabled === true}
                onPick={() => openPicker(null)}
                onFiles={(files) => acceptFiles(files, null)}
                t={t}
              />
            ) : null}
          </div>
        </section>
      ) : null}

      {dragging ? (
        <p className="ah-label text-[color:var(--accent-primary)]">{t('form.photo.drop')}</p>
      ) : null}
    </div>
  );
}

/* ── Tiles ─────────────────────────────────────────────────────────────────── */

interface EmptySlotProps {
  prompt: string | null;
  disabled: boolean;
  onPick: () => void;
  onFiles: (files: FileList) => void;
  t: LabelResolver;
}

function EmptySlot({ prompt, disabled, onPick, onFiles, t }: EmptySlotProps) {
  const [over, setOver] = React.useState(false);

  return (
    <button
      type="button"
      disabled={disabled}
      onClick={onPick}
      onDragOver={(event) => {
        event.preventDefault();
        setOver(true);
      }}
      onDragLeave={() => setOver(false)}
      onDrop={(event) => {
        event.preventDefault();
        event.stopPropagation();
        setOver(false);
        if (event.dataTransfer.files.length > 0) onFiles(event.dataTransfer.files);
      }}
      className={cn(
        'flex aspect-[4/3] flex-col items-start justify-between rounded-card border p-2 text-left',
        'transition-colors duration-100 ease-instrument',
        'outline-none focus-visible:outline-2 focus-visible:outline-offset-2 focus-visible:outline-[var(--focus-ring)]',
        'disabled:cursor-not-allowed disabled:opacity-45',
        over
          ? 'border-[var(--accent-primary)] bg-[var(--surface-sunken)]'
          : 'border-[var(--border-hairline)] bg-[var(--surface-sunken)] hover:border-[var(--border-strong)]',
      )}
    >
      <span className="ah-label">{prompt ?? t('form.photo.add')}</span>
      <span className="text-body-sm text-[color:var(--text-secondary)]">{t('form.photo.empty_slot')}</span>
    </button>
  );
}

interface PhotoTileProps {
  item: MediaItem;
  index: number;
  total: number;
  openPrompts: string[];
  media: MediaUploadApi;
  disabled: boolean;
  t: LabelResolver;
}

function PhotoTile({ item, index, total, openPrompts, media, disabled, t }: PhotoTileProps) {
  const source = item.url ?? item.previewUrl;

  return (
    <figure
      draggable={!disabled}
      onDragStart={(event) => {
        event.dataTransfer.setData('text/autohub-photo', item.id);
        event.dataTransfer.effectAllowed = 'move';
      }}
      onDragOver={(event) => {
        if (event.dataTransfer.types.includes('text/autohub-photo')) event.preventDefault();
      }}
      onDrop={(event) => {
        const movedId = event.dataTransfer.getData('text/autohub-photo');
        if (!movedId || movedId === item.id) return;
        event.preventDefault();
        event.stopPropagation();
        media.moveTo(movedId, index);
      }}
      className="grid gap-1 rounded-card border border-[var(--border-hairline)] bg-[var(--surface-card)] p-2"
    >
      <div className="relative aspect-[4/3] overflow-hidden rounded-control bg-[var(--surface-sunken)]">
        {source ? (
          // Known aspect ratio on the frame, so the image landing shifts nothing.
          <img
            src={source}
            alt={item.prompt ?? item.fileName}
            className="size-full object-cover"
            draggable={false}
          />
        ) : null}
        {/* A stamp, not a highlight. The plate yellow is spent on the step's one action, and a
            badge that competes with it is exactly the drift the rationing rule exists to stop. */}
        {index === 0 ? (
          <span className="absolute top-0 left-0 bg-[var(--surface-inverse)] px-1.5 py-0.5 font-mono text-label font-bold text-[color:var(--text-inverse)]">
            {t('form.photo.cover')}
          </span>
        ) : null}
        {item.status === 'uploading' ? (
          <div
            role="progressbar"
            aria-valuenow={item.progress}
            aria-valuemin={0}
            aria-valuemax={100}
            aria-label={t('form.file.uploading')}
            className="absolute inset-x-0 bottom-0 h-0.5 bg-[var(--surface-sunken)]"
          >
            <div
              className="h-full bg-[var(--accent-primary)]"
              style={{ width: `${Math.max(2, item.progress)}%` }}
            />
          </div>
        ) : null}
      </div>

      <figcaption className="grid gap-1">
        <span className="flex items-baseline justify-between gap-2">
          <span className="ah-label truncate">{item.prompt ?? t('form.photo.unassigned')}</span>
          <span className="font-mono text-label text-[color:var(--text-secondary)]">
            {String(index + 1).padStart(2, '0')}
          </span>
        </span>

        {item.status === 'failed' ? (
          <p
            data-error-code={item.errorCode}
            className="border-l-2 border-flag pl-2 text-body-sm text-[color:var(--text-primary)]"
          >
            {item.errorMessage}
          </p>
        ) : null}

        {openPrompts.length > 0 && item.prompt == null ? (
          <label className="grid gap-1">
            <span className="sr-only">{t('form.photo.assign')}</span>
            <select
              disabled={disabled}
              value=""
              onChange={(event) => {
                if (event.target.value !== '') media.assign(item.id, event.target.value);
              }}
              className="h-9 rounded-control border border-[var(--border-strong)] bg-[var(--surface-card)] px-2 text-body-sm text-[color:var(--text-primary)]"
            >
              <option value="">{t('form.photo.assign')}…</option>
              {openPrompts.map((prompt) => (
                <option key={prompt} value={prompt}>
                  {prompt}
                </option>
              ))}
            </select>
          </label>
        ) : null}

        <div className="flex flex-wrap items-center gap-1">
          <Button
            size="sm"
            variant="ghost"
            disabled={disabled || index === 0}
            aria-label={`${t('form.photo.move_earlier')} — ${item.fileName}`}
            onClick={() => media.move(item.id, -1)}
          >
            ←
          </Button>
          <Button
            size="sm"
            variant="ghost"
            disabled={disabled || index === total - 1}
            aria-label={`${t('form.photo.move_later')} — ${item.fileName}`}
            onClick={() => media.move(item.id, 1)}
          >
            →
          </Button>
          {item.status === 'failed' ? (
            <Button size="sm" variant="outline" onClick={() => media.retry(item.id)}>
              {t('form.file.retry')}
            </Button>
          ) : null}
          <Button
            size="sm"
            variant="ghost"
            disabled={disabled}
            aria-label={`${t('form.file.remove')} — ${item.fileName}`}
            onClick={() => media.remove(item.id)}
            className="ml-auto"
          >
            {t('form.file.remove')}
          </Button>
        </div>
      </figcaption>
    </figure>
  );
}
