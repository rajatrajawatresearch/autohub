'use client';

import * as SliderPrimitive from '@radix-ui/react-slider';
import type * as React from 'react';

import { cn } from '../lib/cn';

export type SliderProps = React.ComponentPropsWithRef<typeof SliderPrimitive.Root>;

/**
 * The range control behind every `facetStyle: "range"` facet — kms driven, price, battery health.
 * Its bounds and step come from the taxonomy attribute's `range`, never from this file.
 *
 * Two thumbs render automatically when the caller passes a two-value array, which is what a
 * `0..40000` search facet is.
 */
export function Slider({ className, value, defaultValue, ...props }: SliderProps) {
  const thumbCount = (value ?? defaultValue ?? [props.min ?? 0]).length;

  return (
    <SliderPrimitive.Root
      value={value}
      defaultValue={defaultValue}
      className={cn(
        'relative flex w-full touch-none items-center select-none',
        'data-[orientation=vertical]:h-full data-[orientation=vertical]:w-auto data-[orientation=vertical]:flex-col',
        'data-[disabled]:opacity-45',
        // Full-height rail so the whole strip is draggable, not just the 4px track.
        'py-3',
        className,
      )}
      {...props}
    >
      <SliderPrimitive.Track
        className={cn(
          'relative grow overflow-hidden rounded-control bg-[var(--surface-sunken)]',
          'border border-[var(--border-hairline)]',
          'data-[orientation=horizontal]:h-1 data-[orientation=vertical]:w-1',
        )}
      >
        <SliderPrimitive.Range className="absolute h-full bg-[var(--accent-primary)] data-[orientation=vertical]:w-full" />
      </SliderPrimitive.Track>
      {Array.from({ length: thumbCount }, (_, index) => (
        <SliderPrimitive.Thumb
          key={index}
          className={cn(
            'block size-4 rounded-control border-2 border-[var(--accent-primary)]',
            'bg-[var(--surface-card)]',
            'outline-none focus-visible:outline-2 focus-visible:outline-offset-2 focus-visible:outline-[var(--focus-ring)]',
            'disabled:pointer-events-none',
          )}
        />
      ))}
    </SliderPrimitive.Root>
  );
}
