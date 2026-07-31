/**
 * Control name → component.
 *
 * The one place in the frontend that knows the control vocabulary. Everything else — the sell flow,
 * dealer onboarding, the 200-point inspection, the loan application — is the same renderer walking
 * different JSON, and this map is the only seam where a name becomes a React component.
 *
 * A name nobody here recognises resolves to a visible placeholder, never to a throw. The registries
 * are edited by people who are not shipping this bundle; a schema that runs ahead of a deployed
 * client is a normal Tuesday, not an incident, and it must cost one field rather than the page.
 */

import {
  AddressControl,
  CheckboxControl,
  DateControl,
  FileControl,
  MoneyControl,
  MultiselectControl,
  NumberControl,
  OtpControl,
  PhoneControl,
  PhotoSetControl,
  PlateControl,
  RadioControl,
  RichTextControl,
  SelectControl,
  SliderControl,
  TextControl,
  ToggleControl,
  UnsupportedControl,
  VehiclePickerControl,
} from './controls';
import type { ControlComponent, FieldDefinition } from './types';

export interface ControlEntry {
  Component: ControlComponent;
  /**
   * The control draws its own caption. True for the ones whose caption is part of the control's
   * anatomy — a plate wears its label, a toggle's label is the row it sits in — so the field shell
   * does not stack a second `.ah-label` on top of it.
   */
  ownsLabel?: (field: FieldDefinition) => boolean;
  /** The control draws its own error affordance; the shell keeps the machine code but not the text. */
  ownsError?: (field: FieldDefinition) => boolean;
  /**
   * The control fills a form column on its own rather than sharing a row. Photo sets, addresses and
   * cascading pickers need the width; a year does not.
   */
  fullWidth?: boolean;
}

const always = () => true;
const never = () => false;

/**
 * The vocabulary from the contract. Keys are the exact `control` values the Form Schema Registry
 * emits — snake_case, as they appear in JSON.
 */
export const controlRegistry: Record<string, ControlEntry> = {
  plate: { Component: PlateControl, ownsLabel: always, ownsError: always, fullWidth: true },
  text: { Component: TextControl },
  number: { Component: NumberControl },
  select: { Component: SelectControl },
  multiselect: { Component: MultiselectControl, fullWidth: true },
  radio: { Component: RadioControl, fullWidth: true },
  // A lone tick carries its own label beside the box; a group of ticks needs a caption above it.
  checkbox: {
    Component: CheckboxControl,
    ownsLabel: (field) => (field.options?.length ?? 0) === 0 && field.config?.optionsSource === undefined,
    fullWidth: true,
  },
  date: { Component: DateControl },
  money: { Component: MoneyControl },
  phone: { Component: PhoneControl },
  otp: { Component: OtpControl, fullWidth: true },
  file: { Component: FileControl, fullWidth: true },
  photo_set: { Component: PhotoSetControl, fullWidth: true },
  slider: { Component: SliderControl, fullWidth: true },
  toggle: { Component: ToggleControl, ownsLabel: always, fullWidth: true },
  address: { Component: AddressControl, fullWidth: true },
  vehicle_picker: { Component: VehiclePickerControl, fullWidth: true },
  rich_text: { Component: RichTextControl, fullWidth: true },
};

const UNSUPPORTED: ControlEntry = {
  Component: UnsupportedControl,
  ownsLabel: never,
  fullWidth: true,
};

/** Never throws, never returns undefined. A bad registry row degrades; it does not cascade. */
export function resolveControl(control: string): ControlEntry {
  return controlRegistry[control] ?? UNSUPPORTED;
}

export function isSupportedControl(control: string): boolean {
  return controlRegistry[control] !== undefined;
}

/**
 * Extension point for a portal that needs a control the shared vocabulary does not have — an admin
 * rule builder, say. Registering a name the contract already defines is refused: three portals
 * quietly rendering three different date pickers is the exact failure this package exists to stop.
 */
export function registerControl(name: string, entry: ControlEntry): void {
  if (controlRegistry[name] !== undefined) {
    throw new Error(`Control "${name}" is already registered. Change it in packages/forms, not here.`);
  }
  controlRegistry[name] = entry;
}
