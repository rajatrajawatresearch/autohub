import { Slot } from '@radix-ui/react-slot';
import { cva, type VariantProps } from 'class-variance-authority';
import type * as React from 'react';

import { cn } from '../lib/cn';

/**
 * Buttons are flat, near-square and bordered. No gradient, no glow, no lift on rest — a control
 * on an instrument, not a call to action on a shopfront.
 *
 * Sizes are expressed as multiples of the density tokens rather than fixed pixel heights, so
 * switching `<html data-density="compact">` retunes every button in dealer and admin without a
 * single component knowing it happened.
 */
export const buttonVariants = cva(
  [
    'inline-flex shrink-0 select-none items-center justify-center gap-2 whitespace-nowrap',
    'rounded-control border font-sans font-semibold',
    'transition-colors duration-100 ease-instrument',
    'outline-none focus-visible:outline-2 focus-visible:outline-offset-2 focus-visible:outline-[var(--focus-ring)]',
    'disabled:pointer-events-none disabled:opacity-45',
    '[&_svg]:pointer-events-none [&_svg]:shrink-0 [&_svg]:size-4',
  ],
  {
    variants: {
      variant: {
        /** Structure and trust. The default for almost everything. */
        primary:
          'border-transparent bg-[var(--accent-primary)] text-[color:var(--text-on-primary)] hover:bg-[var(--accent-primary-hover)]',
        /**
         * The rationed one. Plate yellow, dark text, one per viewport — it marks the single most
         * consequential action on screen (Place bid / Get valuation / Accept offer). A second
         * yellow button in the same view is a bug, not a style choice.
         */
        action:
          'border-transparent bg-[var(--accent-action)] text-[color:var(--text-on-accent)] hover:bg-[var(--accent-action-hover)]',
        outline:
          'border-[var(--border-strong)] bg-transparent text-[color:var(--text-primary)] hover:bg-[var(--surface-sunken)]',
        ghost:
          'border-transparent bg-transparent text-[color:var(--text-primary)] hover:bg-[var(--surface-sunken)]',
        danger: 'border-transparent bg-flag text-plate hover:bg-flag/90',
      },
      size: {
        sm: 'h-[calc(var(--density-control-h)*0.8)] px-[calc(var(--density-pad-x)*0.75)] text-body-sm',
        md: 'h-[var(--density-control-h)] px-[var(--density-pad-x)] text-body-sm',
        lg: 'h-[calc(var(--density-control-h)*1.15)] px-[calc(var(--density-pad-x)*1.5)] text-body',
        /** Square. Same height as `md`, so it lines up in a toolbar without a wrapper. */
        icon: 'h-[var(--density-control-h)] w-[var(--density-control-h)] px-0',
      },
      /** Full width in a form column or a mobile sheet, where a control owns its row. */
      block: {
        true: 'w-full',
        false: '',
      },
    },
    defaultVariants: {
      variant: 'primary',
      size: 'md',
      block: false,
    },
  },
);

export interface ButtonProps
  extends React.ComponentPropsWithRef<'button'>,
    VariantProps<typeof buttonVariants> {
  /** Render the child element instead of a `<button>` — for links that must look like controls. */
  asChild?: boolean;
}

export function Button({
  className,
  variant,
  size,
  block,
  asChild = false,
  type,
  ...props
}: ButtonProps) {
  const Comp = asChild ? Slot : 'button';

  return (
    <Comp
      // A button inside a form defaults to `submit` in HTML, which fires flows nobody asked for.
      // Only a real <button> gets an explicit default; Slot passes whatever the child declares.
      {...(asChild ? {} : { type: type ?? 'button' })}
      className={cn(buttonVariants({ variant, size, block }), className)}
      {...props}
    />
  );
}
