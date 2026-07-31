/**
 * The AutoHub design system's public surface.
 *
 * This file is the contract. Web, dealer, and admin import from here and nowhere else, so a
 * component that is not listed here does not exist as far as the apps are concerned. Adding an
 * export is a deliberate act; it is what stops three portals from quietly growing three different
 * buttons.
 */

export { cn } from './lib/cn';
export { formatIndianMoney, formatIndianNumber, formatKms } from './lib/format';

/* ── Primitives ──────────────────────────────────────────────────────────────
   Radix behaviour, Registry styling. Near-square, hairline-bordered, flat. */
export { Button, buttonVariants, type ButtonProps } from './primitives/button';
export { Input, type InputProps } from './primitives/input';
export { Textarea, type TextareaProps } from './primitives/textarea';
export { Label } from './primitives/label';
export { Select } from './primitives/select';
export { Checkbox } from './primitives/checkbox';
export { RadioGroup, RadioGroupItem } from './primitives/radio-group';
export { Switch } from './primitives/switch';
export { Slider } from './primitives/slider';
export { Separator } from './primitives/separator';
export { Card, CardHeader, CardTitle, CardDescription, CardContent, CardFooter } from './primitives/card';
export { Dialog, DialogTrigger, DialogContent, DialogHeader, DialogTitle, DialogDescription, DialogFooter } from './primitives/dialog';
export { Popover, PopoverTrigger, PopoverContent } from './primitives/popover';
export { Tooltip, TooltipProvider, TooltipTrigger, TooltipContent } from './primitives/tooltip';
export { Tabs, TabsList, TabsTrigger, TabsContent } from './primitives/tabs';
export { DropdownMenu, DropdownMenuTrigger, DropdownMenuContent, DropdownMenuItem, DropdownMenuSeparator, DropdownMenuLabel } from './primitives/dropdown-menu';
export { Accordion, AccordionItem, AccordionTrigger, AccordionContent } from './primitives/accordion';
export { Skeleton } from './primitives/skeleton';
export { Badge, badgeVariants, type BadgeProps } from './primitives/badge';

/* ── The AutoHub canon ───────────────────────────────────────────────────────
   Components that only make sense in this product. The vocabulary of the registry. */
export { PlateBlock, type PlateBlockProps, type PlateClass } from './canon/plate-block';
export { FieldBlock, type FieldBlockProps } from './canon/field-block';
export { SpecStrip, type SpecStripProps, type SpecItem } from './canon/spec-strip';
export { MoneyValue, type MoneyValueProps } from './canon/money-value';
export { OdometerValue, type OdometerValueProps } from './canon/odometer-value';
export { StatusPill, type StatusPillProps, type StateCategory } from './canon/status-pill';
export { VehicleCard, type VehicleCardProps } from './canon/vehicle-card';
export { WorkflowTimeline, type WorkflowTimelineProps } from './canon/workflow-timeline';
export { BidTicket, type BidTicketProps } from './canon/bid-ticket';
export { EmptyState, type EmptyStateProps } from './canon/empty-state';
export { SectionHeading, type SectionHeadingProps } from './canon/section-heading';
export { PlateInput, type PlateInputProps } from './canon/plate-input';
export { DataTable, type DataTableProps, type DataTableColumn } from './canon/data-table';
export { ThemeToggle, THEME_SCRIPT } from './canon/theme-toggle';
