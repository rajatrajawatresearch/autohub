import { clsx, type ClassValue } from 'clsx';
import { twMerge } from 'tailwind-merge';

/**
 * Merges class names, letting a caller's utility win over a component's default.
 * Without it, `<Button className="bg-signal">` loses to the variant's own background and
 * the override silently does nothing.
 */
export function cn(...inputs: ClassValue[]): string {
  return twMerge(clsx(inputs));
}
