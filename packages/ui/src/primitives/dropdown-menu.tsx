'use client';

import * as DropdownMenuPrimitive from '@radix-ui/react-dropdown-menu';
import type * as React from 'react';

import { cn } from '../lib/cn';

export const DropdownMenu = DropdownMenuPrimitive.Root;
export const DropdownMenuTrigger = DropdownMenuPrimitive.Trigger;

export function DropdownMenuContent({
  className,
  align = 'end',
  sideOffset = 6,
  ...props
}: React.ComponentPropsWithRef<typeof DropdownMenuPrimitive.Content>) {
  return (
    <DropdownMenuPrimitive.Portal>
      <DropdownMenuPrimitive.Content
        align={align}
        sideOffset={sideOffset}
        collisionPadding={12}
        className={cn(
          'z-50 min-w-44 max-w-[92vw] overflow-hidden rounded-card border border-[var(--border-hairline)]',
          'bg-[var(--surface-card)] p-1 text-[color:var(--text-primary)] shadow-overlay',
          className,
        )}
        {...props}
      />
    </DropdownMenuPrimitive.Portal>
  );
}

export type DropdownMenuItemProps = React.ComponentPropsWithRef<
  typeof DropdownMenuPrimitive.Item
> & {
  /** Destructive actions — remove listing, revoke access. Flag colour, never the default. */
  destructive?: boolean;
};

export function DropdownMenuItem({ className, destructive, ...props }: DropdownMenuItemProps) {
  return (
    <DropdownMenuPrimitive.Item
      className={cn(
        'flex cursor-default items-center gap-2 rounded-control select-none',
        'px-[calc(var(--density-pad-x)*0.75)] py-[var(--density-pad-y)] text-body-sm',
        'outline-none transition-colors duration-100 ease-instrument',
        'data-[highlighted]:bg-[var(--surface-sunken)]',
        'data-[disabled]:pointer-events-none data-[disabled]:opacity-45',
        '[&_svg]:pointer-events-none [&_svg]:size-4 [&_svg]:shrink-0',
        destructive === true ? 'text-flag' : '',
        className,
      )}
      {...props}
    />
  );
}

export function DropdownMenuSeparator({
  className,
  ...props
}: React.ComponentPropsWithRef<typeof DropdownMenuPrimitive.Separator>) {
  return (
    <DropdownMenuPrimitive.Separator
      className={cn('-mx-1 my-1 h-px bg-[var(--border-hairline)]', className)}
      {...props}
    />
  );
}

export function DropdownMenuLabel({
  className,
  ...props
}: React.ComponentPropsWithRef<typeof DropdownMenuPrimitive.Label>) {
  return (
    <DropdownMenuPrimitive.Label
      className={cn('ah-label px-[calc(var(--density-pad-x)*0.75)] py-2', className)}
      {...props}
    />
  );
}
