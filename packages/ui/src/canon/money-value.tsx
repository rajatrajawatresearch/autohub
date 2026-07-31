import { cn } from '../lib/cn';
import { formatIndianMoney } from '../lib/format';

/**
 * Money, the way India writes and says it.
 *
 * Amounts arrive as integer minor units (paise) and are formatted here, at the edge — never in
 * the domain, never as a float, never as a server-rendered string. `₹8,40,000` is how India
 * writes it and `₹8.4L` is how India says it; rendering `$840,000.00` would mark the product as
 * foreign on the first screen.
 *
 * The rupee sign sits at 0.85em so the digits, which are what the user is actually comparing,
 * hold the line.
 */

export interface MoneyValueProps {
  /** Integer paise. */
  minorUnits: number;
  /** `₹8.4L` instead of `₹8,40,000`. Use in cards and tables; spell it out on a contract. */
  compact?: boolean;
  size?: 'xs' | 'sm' | 'md' | 'lg' | 'xl';
  tone?: 'default' | 'secondary' | 'accent' | 'positive' | 'negative';
  className?: string;
  title?: string;
}

const SIZES = {
  xs: 'text-[length:var(--text-body-sm)] leading-[1.35]',
  sm: 'text-[length:var(--text-data)] leading-[1.4]',
  md: 'text-[length:1.0625rem] leading-[1.35]',
  lg: 'text-[length:1.5rem] leading-[1.2]',
  xl: 'text-[length:clamp(2rem,5vw,2.75rem)] leading-[1.05]',
} as const;

const TONES = {
  default: 'text-[color:var(--text-primary)]',
  secondary: 'text-[color:var(--text-secondary)]',
  accent: 'text-[color:var(--accent-primary)]',
  positive: 'text-[color:var(--color-clear)]',
  negative: 'text-[color:var(--color-flag)]',
} as const;

/** Splits "-₹8.4L" into sign, symbol and digits so only the symbol is scaled down. */
function splitAmount(formatted: string): { prefix: string; rest: string } {
  const match = /^([^0-9]*)([\s\S]*)$/.exec(formatted);
  return { prefix: match?.[1] ?? '', rest: match?.[2] ?? formatted };
}

export function MoneyValue({
  minorUnits,
  compact = false,
  size = 'md',
  tone = 'default',
  className,
  title,
}: MoneyValueProps) {
  const formatted = formatIndianMoney(minorUnits, { compact });
  const { prefix, rest } = splitAmount(formatted);

  return (
    <span
      title={title}
      className={cn('whitespace-nowrap font-mono font-semibold', SIZES[size], TONES[tone], className)}
    >
      <span aria-hidden="true" className="text-[0.85em]">
        {prefix}
      </span>
      <span aria-hidden="true">{rest}</span>
      <span className="sr-only">{formatted}</span>
    </span>
  );
}
