import 'package:autohub_core/autohub_core.dart';
import 'package:flutter/widgets.dart' hide FormField;

import 'controls/address_control.dart';
import 'controls/checkbox_control.dart';
import 'controls/date_control.dart';
import 'controls/file_control.dart';
import 'controls/money_control.dart';
import 'controls/multiselect_control.dart';
import 'controls/number_control.dart';
import 'controls/otp_control.dart';
import 'controls/phone_control.dart';
import 'controls/photo_set_control.dart';
import 'controls/plate_control.dart';
import 'controls/radio_control.dart';
import 'controls/rich_text_control.dart';
import 'controls/select_control.dart';
import 'controls/slider_control.dart';
import 'controls/text_control.dart';
import 'controls/toggle_control.dart';
import 'controls/unsupported_control.dart';
import 'controls/vehicle_picker_control.dart';
import 'form_scope.dart';

/// Builds one control from the props the field shell hands it.
typedef ControlBuilder = Widget Function(FieldProps props);

/// How the shell should treat a control it is about to draw.
@immutable
class ControlEntry {
  const ControlEntry(this.build, {this.ownsLabel, this.ownsError});

  final ControlBuilder build;

  /// The control draws its own caption.
  ///
  /// True for the ones whose caption is part of their anatomy — a plate wears its label, a toggle's
  /// label *is* the row it sits in, a lone tick's label sits beside the box. Without this the shell
  /// stacks a second uppercase caption above a control that already has one, which reads as two
  /// different questions.
  final bool Function(FormField field)? ownsLabel;

  /// The control draws its own fault sentence. The shell then contributes only the machine code.
  final bool Function(FormField field)? ownsError;
}

bool _always(FormField field) => true;

/// The vocabulary from the contract, keyed by the enum `autohub_core` decodes `control` into.
///
/// This map is the only seam in the mobile client where a registry value becomes a widget.
/// Everything else — the sell flow, dealer onboarding, the 200-point inspection, the loan
/// application — is one renderer walking different JSON.
///
/// Keyed by [FormControl] rather than by string, deliberately. That enum is a statement about *this
/// build's capabilities* rather than about the registry's contents, and keying on it makes the
/// coverage check a compile-time one: add a control to the contract and this file stops building
/// until somebody writes the widget, rather than shipping a placeholder to users.
///
/// Notably absent: any notion of column span. The web registry carries `fullWidth` because it lays
/// fields out in a two-column grid. A phone form is one column, always, and a control that tried to
/// share a row with another would put two 44dp targets inside 180dp.
final Map<FormControl, ControlEntry> _controls = <FormControl, ControlEntry>{
  FormControl.plate: ControlEntry(
    (FieldProps props) => PlateControl(props),
    ownsLabel: _always,
    ownsError: _always,
  ),
  // The five that render through `AhTextField` already draw the fault sentence, with the invalid
  // border that belongs to it, so the shell contributes only the machine code.
  FormControl.text: ControlEntry((FieldProps props) => TextControl(props), ownsError: _always),
  FormControl.number: ControlEntry((FieldProps props) => NumberControl(props), ownsError: _always),
  FormControl.select: ControlEntry((FieldProps props) => SelectControl(props)),
  FormControl.multiselect: ControlEntry((FieldProps props) => MultiselectControl(props)),
  FormControl.radio: ControlEntry((FieldProps props) => RadioControl(props)),
  FormControl.checkbox: ControlEntry(
    (FieldProps props) => CheckboxControl(props),
    ownsLabel: CheckboxControl.ownsLabel,
  ),
  FormControl.date: ControlEntry((FieldProps props) => DateControl(props)),
  FormControl.money: ControlEntry((FieldProps props) => MoneyControl(props), ownsError: _always),
  FormControl.phone: ControlEntry((FieldProps props) => PhoneControl(props), ownsError: _always),
  FormControl.otp: ControlEntry((FieldProps props) => OtpControl(props)),
  FormControl.file: ControlEntry((FieldProps props) => FileControl(props)),
  FormControl.photoSet: ControlEntry((FieldProps props) => PhotoSetControl(props)),
  FormControl.slider: ControlEntry((FieldProps props) => SliderControl(props)),
  FormControl.toggle: ControlEntry(
    (FieldProps props) => ToggleControl(props),
    ownsLabel: _always,
  ),
  FormControl.address: ControlEntry((FieldProps props) => AddressControl(props)),
  FormControl.vehiclePicker: ControlEntry((FieldProps props) => VehiclePickerControl(props)),
  FormControl.richText:
      ControlEntry((FieldProps props) => RichTextControl(props), ownsError: _always),
  FormControl.unsupported: ControlEntry((FieldProps props) => UnsupportedControl(props)),
};

/// Never throws, never returns null.
///
/// `autohub_core` already decodes a control name this build has never heard of into
/// [FormControl.unsupported], so a registry running ahead of a deployed binary arrives here as a
/// value that has an entry rather than as a gap. The `??` is the belt to that braces: a value added
/// to the enum without an entry in the map degrades to the placeholder rather than taking down the
/// one screen that creates supply.
ControlEntry resolveControl(FormControl control) =>
    _controls[control] ?? _controls[FormControl.unsupported]!;

bool isSupportedControl(FormControl control) =>
    control != FormControl.unsupported && _controls.containsKey(control);

/// Every control this build can draw. A diagnostics screen wants it, and so does a test that
/// asserts the contract's vocabulary is covered.
Iterable<FormControl> get supportedControls =>
    _controls.keys.where((FormControl control) => control != FormControl.unsupported);

/// Replaces one control's widget for a single binary.
///
/// The inspector app will want a `photo_set` that also writes to its own evidence store; the dealer
/// app may want a bid-shaped `money`. Substituting the *widget* is legitimate. Substituting the
/// *vocabulary* is not, which is why there is no way to add a name here — three apps quietly
/// rendering three different date pickers is the failure this package exists to stop, and on mobile
/// it would be three date pickers that cannot be corrected without three app-store releases.
void overrideControl(FormControl control, ControlEntry entry) {
  if (control == FormControl.unsupported) {
    throw StateError('The unsupported placeholder is not overridable.');
  }
  _controls[control] = entry;
}
