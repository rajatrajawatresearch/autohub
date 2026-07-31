'use client';

import * as React from 'react';
import { Button, cn, formatIndianNumber } from '@autohub/ui';

import { useFormLabels } from '../form-context';
import { useMediaUpload } from './media-upload';
import { asMediaItems } from './shared';
import type { FieldProps, MediaItem } from '../types';

/** Documents: RC, insurance, PUC, PAN, GST certificate, a bank statement for a loan application. */
export function FileControl({
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
  const multiple = field.config?.multiple === true;
  const inputRef = React.useRef<HTMLInputElement>(null);
  const [dragging, setDragging] = React.useState(false);

  const media = useMediaUpload({
    field,
    items,
    onChange: (next) => onChange(next.length === 0 ? undefined : next),
    t,
  });

  function accept(files: FileList | null) {
    if (!files || files.length === 0) return;
    const list = Array.from(files);
    media.add(multiple ? list : list.slice(0, 1));
    if (!multiple && items.length > 0) {
      for (const existing of items) media.remove(existing.id);
    }
  }

  return (
    <div className="grid gap-3" onBlur={onBlur}>
      <div
        onDragOver={(event) => {
          event.preventDefault();
          setDragging(true);
        }}
        onDragLeave={() => setDragging(false)}
        onDrop={(event) => {
          event.preventDefault();
          setDragging(false);
          if (!disabled) accept(event.dataTransfer.files);
        }}
        className={cn(
          'flex flex-col items-start gap-3 rounded-card border p-4',
          'transition-colors duration-100 ease-instrument',
          dragging
            ? 'border-[var(--accent-primary)] bg-[var(--surface-sunken)]'
            : 'border-[var(--border-hairline)] bg-[var(--surface-card)]',
          invalid ? 'border-flag' : '',
        )}
      >
        <p className="text-body-sm text-[color:var(--text-secondary)]">{t('form.file.drop')}</p>
        <Button
          size="sm"
          variant="outline"
          disabled={disabled || field.readOnly === true}
          onClick={() => inputRef.current?.click()}
        >
          {t('form.file.choose')}
        </Button>
        <input
          ref={inputRef}
          id={inputId}
          name={name}
          type="file"
          multiple={multiple}
          accept={field.config?.accept?.join(',')}
          disabled={disabled}
          aria-describedby={describedBy}
          aria-invalid={invalid || undefined}
          className="sr-only"
          onChange={(event) => {
            accept(event.target.files);
            event.target.value = '';
          }}
        />
      </div>

      {items.length > 0 ? (
        <ul className="grid gap-0 border-y border-[var(--border-hairline)]">
          {items.map((item) => (
            <li
              key={item.id}
              className="flex items-center gap-3 border-b border-[var(--border-hairline)] py-2 last:border-b-0"
            >
              <div className="min-w-0 flex-1">
                <p className="truncate text-body-sm text-[color:var(--text-primary)]">{item.fileName}</p>
                <p className="ah-label">
                  {formatIndianNumber(Math.max(1, Math.round(item.sizeBytes / 1024)))} KB
                </p>
                <UploadBar item={item} />
              </div>
              {item.status === 'failed' ? (
                <Button size="sm" variant="outline" onClick={() => media.retry(item.id)}>
                  {t('form.file.retry')}
                </Button>
              ) : null}
              <Button
                size="sm"
                variant="ghost"
                disabled={disabled}
                onClick={() => media.remove(item.id)}
                aria-label={`${t('form.file.remove')} ${item.fileName}`}
              >
                {t('form.file.remove')}
              </Button>
            </li>
          ))}
        </ul>
      ) : null}
    </div>
  );
}

/**
 * Per-item progress. A 2px rule, not a bar with a percentage badge — this is an instrument panel,
 * and the only question a user has is "is it moving".
 */
export function UploadBar({ item }: { item: MediaItem }) {
  if (item.status === 'uploaded' || item.status === 'pending') {
    return item.errorMessage ? (
      <p className="border-l-2 border-flag pl-2 text-body-sm text-[color:var(--text-primary)]">
        {item.errorMessage}
      </p>
    ) : null;
  }

  if (item.status === 'failed') {
    return (
      <p
        data-error-code={item.errorCode}
        className="border-l-2 border-flag pl-2 text-body-sm text-[color:var(--text-primary)]"
      >
        {item.errorMessage}
      </p>
    );
  }

  return (
    <div
      role="progressbar"
      aria-valuenow={item.progress}
      aria-valuemin={0}
      aria-valuemax={100}
      className="mt-1 h-0.5 w-full bg-[var(--surface-sunken)]"
    >
      <div
        className="h-full bg-[var(--accent-primary)]"
        style={{ width: `${Math.max(2, item.progress)}%` }}
      />
    </div>
  );
}
