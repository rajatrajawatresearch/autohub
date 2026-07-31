'use client';

import * as SelectPrimitive from '@radix-ui/react-select';
import { Check, ChevronDown, ChevronUp } from 'lucide-react';
import type * as React from 'react';

import { cn } from '../lib/cn';

/**
 * An option is whatever the registry says it is. Nothing in this file knows what a fuel type is,
 * and nothing in this file ever will — the list arrives from
 * `GET /platform/taxonomy/attributes` or from a form schema's field definition.
 */
export interface SelectOption {
  value: string;
  label: string;
  disabled?: boolean;
}

/* ── Composable parts ─────────────────────────────────────────────────────────
   Exposed as statics on `Select` for the cases the convenience form cannot express:
   grouped city lists, an option that renders a PlateBlock, an async-loading list. */

type TriggerProps = React.ComponentPropsWithRef<typeof SelectPrimitive.Trigger> & {
  invalid?: boolean;
  triggerSize?: 'sm' | 'md';
};

function SelectTrigger({ className, children, invalid, triggerSize = 'md', ...props }: TriggerProps) {
  return (
    <SelectPrimitive.Trigger
      aria-invalid={invalid === true ? true : props['aria-invalid']}
      className={cn(
        'flex w-full items-center justify-between gap-2 rounded-control border',
        'border-[var(--border-strong)] bg-[var(--surface-card)] text-[color:var(--text-primary)]',
        'font-sans transition-colors duration-100 ease-instrument',
        'outline-none focus-visible:outline-2 focus-visible:outline-offset-2 focus-visible:outline-[var(--focus-ring)]',
        'aria-[invalid=true]:border-flag',
        'disabled:cursor-not-allowed disabled:bg-[var(--surface-sunken)] disabled:opacity-60',
        'data-[placeholder]:text-[color:var(--text-secondary)]',
        '[&>span]:truncate [&>span]:text-left',
        triggerSize === 'sm'
          ? 'h-[calc(var(--density-control-h)*0.8)] px-[calc(var(--density-pad-x)*0.75)] text-body-sm'
          : 'h-[var(--density-control-h)] px-[var(--density-pad-x)] text-body',
        className,
      )}
      {...props}
    >
      {children}
      <SelectPrimitive.Icon asChild>
        <ChevronDown aria-hidden="true" className="size-4 shrink-0 opacity-60" />
      </SelectPrimitive.Icon>
    </SelectPrimitive.Trigger>
  );
}

function SelectContent({
  className,
  children,
  position = 'popper',
  ...props
}: React.ComponentPropsWithRef<typeof SelectPrimitive.Content>) {
  return (
    <SelectPrimitive.Portal>
      <SelectPrimitive.Content
        position={position}
        sideOffset={4}
        className={cn(
          'relative z-50 max-h-72 min-w-[8rem] overflow-hidden rounded-card border',
          'border-[var(--border-hairline)] bg-[var(--surface-card)] shadow-overlay',
          'text-[color:var(--text-primary)]',
          position === 'popper' ? 'w-[var(--radix-select-trigger-width)]' : '',
          className,
        )}
        {...props}
      >
        <SelectPrimitive.ScrollUpButton className="flex h-6 items-center justify-center">
          <ChevronUp aria-hidden="true" className="size-4 opacity-60" />
        </SelectPrimitive.ScrollUpButton>
        <SelectPrimitive.Viewport className="p-1">{children}</SelectPrimitive.Viewport>
        <SelectPrimitive.ScrollDownButton className="flex h-6 items-center justify-center">
          <ChevronDown aria-hidden="true" className="size-4 opacity-60" />
        </SelectPrimitive.ScrollDownButton>
      </SelectPrimitive.Content>
    </SelectPrimitive.Portal>
  );
}

function SelectItem({
  className,
  children,
  ...props
}: React.ComponentPropsWithRef<typeof SelectPrimitive.Item>) {
  return (
    <SelectPrimitive.Item
      className={cn(
        'relative flex w-full cursor-default items-center gap-2 rounded-control',
        'py-[var(--density-pad-y)] pr-8 pl-[calc(var(--density-pad-x)*0.75)]',
        'text-body-sm outline-none select-none',
        'data-[highlighted]:bg-[var(--surface-sunken)]',
        'data-[state=checked]:font-semibold',
        'data-[disabled]:pointer-events-none data-[disabled]:opacity-45',
        className,
      )}
      {...props}
    >
      <SelectPrimitive.ItemText>{children}</SelectPrimitive.ItemText>
      <span className="absolute right-2 flex size-4 items-center justify-center">
        <SelectPrimitive.ItemIndicator>
          <Check aria-hidden="true" className="size-4 text-[color:var(--accent-primary)]" />
        </SelectPrimitive.ItemIndicator>
      </span>
    </SelectPrimitive.Item>
  );
}

function SelectLabel({
  className,
  ...props
}: React.ComponentPropsWithRef<typeof SelectPrimitive.Label>) {
  return (
    <SelectPrimitive.Label
      className={cn('ah-label px-[calc(var(--density-pad-x)*0.75)] py-2', className)}
      {...props}
    />
  );
}

function SelectSeparator({
  className,
  ...props
}: React.ComponentPropsWithRef<typeof SelectPrimitive.Separator>) {
  return (
    <SelectPrimitive.Separator
      className={cn('-mx-1 my-1 h-px bg-[var(--border-hairline)]', className)}
      {...props}
    />
  );
}

/* ── The everyday form ────────────────────────────────────────────────────── */

export interface SelectFieldProps {
  /** Straight from a registry response. Order is the registry's `sortOrder`, not ours. */
  options: SelectOption[];
  value?: string;
  defaultValue?: string;
  onValueChange?: (value: string) => void;
  /** An instruction, not a mood: "Select fuel type", never "Choose one…". */
  placeholder?: string;
  disabled?: boolean;
  required?: boolean;
  invalid?: boolean;
  /** Submitted with a native form — the schema form renderer uses this. */
  name?: string;
  id?: string;
  triggerSize?: 'sm' | 'md';
  className?: string;
  'aria-label'?: string;
  'aria-labelledby'?: string;
  'aria-describedby'?: string;
}

function SelectField({
  options,
  value,
  defaultValue,
  onValueChange,
  placeholder,
  disabled,
  required,
  invalid,
  name,
  id,
  triggerSize = 'md',
  className,
  ...aria
}: SelectFieldProps) {
  return (
    <SelectPrimitive.Root
      value={value}
      defaultValue={defaultValue}
      onValueChange={onValueChange}
      disabled={disabled}
      required={required}
      name={name}
    >
      <SelectTrigger id={id} invalid={invalid} triggerSize={triggerSize} className={className} {...aria}>
        <SelectPrimitive.Value placeholder={placeholder} />
      </SelectTrigger>
      <SelectContent>
        {options.map((option) => (
          <SelectItem key={option.value} value={option.value} disabled={option.disabled}>
            {option.label}
          </SelectItem>
        ))}
      </SelectContent>
    </SelectPrimitive.Root>
  );
}

/**
 * `<Select options={…} />` for the ninety-per-cent case; `Select.Root` / `Select.Trigger` / … for
 * the rest. One export, because `packages/ui/src/index.ts` is the contract and three portals must
 * not grow three different dropdowns.
 */
export const Select = Object.assign(SelectField, {
  Root: SelectPrimitive.Root,
  Trigger: SelectTrigger,
  Value: SelectPrimitive.Value,
  Content: SelectContent,
  Item: SelectItem,
  Group: SelectPrimitive.Group,
  Label: SelectLabel,
  Separator: SelectSeparator,
});
