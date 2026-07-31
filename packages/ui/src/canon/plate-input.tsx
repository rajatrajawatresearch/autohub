'use client';

import { useEffect, useId, useRef, type ChangeEvent, type KeyboardEvent } from 'react';
import { cn } from '../lib/cn';
import { groupRegistration } from './plate-block';

/**
 * The homepage hero, and the thesis rendered as an interaction: the most characteristic artifact
 * in the subject's world doing the most valuable job in the product.
 *
 * The input *is* the plate — same 2px border, same mono, same tracking, same zero radius. The
 * user types "mh12ab1234"; the field shows "MH 12 AB 1234" and the caller receives the canonical
 * "MH12AB1234". Separators are stripped as they are typed, so paste from an RC, an SMS or a
 * WhatsApp forward all land in the same place.
 *
 * One input, one outcome, no funnel.
 */

export interface PlateInputProps {
  /** Canonical value — uppercase, no separators. The component owns display, not state. */
  value: string;
  onChange: (canonical: string) => void;
  /** Fired on Enter. The page owns the actual call to action. */
  onSubmit?: (canonical: string) => void;
  size?: 'sm' | 'md' | 'lg';
  disabled?: boolean;
  /** Resolved message from the form registry's validation rule. Presence marks the field invalid. */
  error?: string;
  /** Sits above the field as an .ah-label, like every other value in the product. */
  label?: string;
  hint?: string;
  placeholder?: string;
  /**
   * Input guard only — 11 characters is the longest registration any Indian series produces.
   * The actual validation rule and its message come from the form schema registry.
   */
  maxChars?: number;
  id?: string;
  name?: string;
  autoFocus?: boolean;
  inputMode?: 'text' | 'none';
  className?: string;
  'aria-label'?: string;
}

const SIZES = {
  sm: 'h-[2.5rem] px-3 text-[length:0.9375rem]',
  md: 'h-[3.25rem] px-4 text-[length:var(--text-plate)]',
  lg: 'h-[4.25rem] px-5 text-[length:clamp(1.25rem,5.5vw,2rem)]',
} as const;

const ALPHANUMERIC = /[^0-9A-Za-z]/g;

export function PlateInput({
  value,
  onChange,
  onSubmit,
  size = 'md',
  disabled = false,
  error,
  label,
  hint,
  placeholder = 'MH 12 AB 1234',
  maxChars = 11,
  id,
  name,
  autoFocus,
  inputMode,
  className,
  'aria-label': ariaLabel,
}: PlateInputProps) {
  const reactId = useId();
  const inputId = id ?? `plate-${reactId}`;
  const messageId = `${inputId}-message`;
  const inputRef = useRef<HTMLInputElement>(null);
  /** Caret expressed as "how many alphanumerics precede it" — the only position that survives regrouping. */
  const pendingCaret = useRef<number | null>(null);

  const display = groupRegistration(value);

  useEffect(() => {
    const el = inputRef.current;
    const target = pendingCaret.current;
    pendingCaret.current = null;
    if (!el || target === null) return;

    let seen = 0;
    let index = target === 0 ? 0 : display.length;
    if (target > 0) {
      for (let i = 0; i < display.length; i += 1) {
        if (display[i] !== ' ') {
          seen += 1;
          if (seen === target) {
            index = i + 1;
            break;
          }
        }
      }
    }
    el.setSelectionRange(index, index);
  }, [display]);

  function handleChange(event: ChangeEvent<HTMLInputElement>) {
    const raw = event.target.value;
    const caret = event.target.selectionStart ?? raw.length;
    const canonical = raw.replace(ALPHANUMERIC, '').toUpperCase().slice(0, maxChars);
    const alphanumericsBeforeCaret = raw.slice(0, caret).replace(ALPHANUMERIC, '').length;

    pendingCaret.current = Math.min(alphanumericsBeforeCaret, canonical.length);
    onChange(canonical);
  }

  function handleKeyDown(event: KeyboardEvent<HTMLInputElement>) {
    if (event.key !== 'Enter' || !onSubmit) return;
    event.preventDefault();
    onSubmit(value);
  }

  return (
    <div className={cn('flex w-full flex-col gap-2', className)}>
      {label ? (
        <label className="ah-label" htmlFor={inputId}>
          {label}
        </label>
      ) : null}

      <input
        ref={inputRef}
        id={inputId}
        name={name}
        type="text"
        value={display}
        onChange={handleChange}
        onKeyDown={handleKeyDown}
        disabled={disabled}
        autoFocus={autoFocus}
        placeholder={placeholder}
        autoComplete="off"
        autoCorrect="off"
        autoCapitalize="characters"
        spellCheck={false}
        inputMode={inputMode}
        enterKeyHint="go"
        aria-label={label ? undefined : ariaLabel}
        aria-invalid={error ? true : undefined}
        aria-describedby={error || hint ? messageId : undefined}
        className={cn(
          'w-full rounded-plate border-2 bg-plate text-center font-mono font-bold uppercase tracking-[0.14em] text-[color:var(--color-tarmac)]',
          'caret-[var(--color-highway)] placeholder:font-normal placeholder:tracking-[0.14em] placeholder:text-[color:var(--color-n-400)]',
          'disabled:cursor-not-allowed disabled:opacity-60',
          error ? 'border-[color:var(--color-flag)]' : 'border-[color:var(--color-tarmac)]',
          SIZES[size],
        )}
      />

      {error ? (
        <p
          id={messageId}
          role="alert"
          className={cn(
            'border-l-2 border-[color:var(--color-flag)] bg-[var(--color-flag-soft)]',
            'px-2 py-1 text-[length:var(--text-body-sm)] leading-[1.45] text-[color:var(--color-flag)]',
          )}
        >
          {error}
        </p>
      ) : hint ? (
        <p id={messageId} className="text-[length:var(--text-body-sm)] text-[color:var(--text-secondary)]">
          {hint}
        </p>
      ) : null}
    </div>
  );
}
