import * as SeparatorPrimitive from '@radix-ui/react-separator';
import type * as React from 'react';

import { cn } from '../lib/cn';

export type SeparatorProps = React.ComponentPropsWithRef<typeof SeparatorPrimitive.Root>;

/**
 * A 1px rule. Structure in this product comes from rules, not from elevation — which is why this
 * is one of the most-used components in the system and why it never gets thicker or coloured.
 */
export function Separator({
  className,
  orientation = 'horizontal',
  decorative = true,
  ...props
}: SeparatorProps) {
  return (
    <SeparatorPrimitive.Root
      orientation={orientation}
      decorative={decorative}
      className={cn(
        'shrink-0 bg-[var(--border-hairline)]',
        orientation === 'horizontal' ? 'h-px w-full' : 'h-full w-px',
        className,
      )}
      {...props}
    />
  );
}
