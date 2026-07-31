import 'package:autohub_ui/autohub_ui.dart';
import 'package:flutter/material.dart';

import '../field_config.dart';
import '../form_scope.dart';
import 'shared.dart';

/// An [AhTextField] whose text is owned by the widget and whose *value* is owned by the form.
///
/// Every typed control in this package needs the same three things and would get them wrong the
/// same way if each wrote its own: a controller that is not rebuilt on every keystroke, a re-sync
/// when the value moves underneath it (a draft restoring, a VAHAN lookup filling the odometer in),
/// and a blur that reports up so the field's fault may finally be shown.
///
/// The re-sync condition is the subtle one. Comparing the incoming value against what the current
/// text *parses to* — rather than against the text itself — is what lets a user type `12.` without
/// the field snapping back to `12` between the point and the digit.
class AhManagedTextField extends StatefulWidget {
  const AhManagedTextField({
    required this.text,
    required this.onTextChanged,
    required this.onBlur,
    this.placeholder,
    this.unit,
    this.error,
    this.enabled = true,
    this.readOnly = false,
    this.mono = false,
    this.keyboardType,
    this.textCapitalization = TextCapitalization.none,
    this.maxLines = 1,
    this.sameValue,
    super.key,
  });

  final String text;
  final ValueChanged<String> onTextChanged;
  final VoidCallback onBlur;
  final String? placeholder;
  final String? unit;

  /// The registry's authored sentence, drawn by [AhTextField] in the house style. The field shell
  /// prints the machine code underneath; see `ControlEntry.ownsError`.
  final String? error;

  final bool enabled;
  final bool readOnly;
  final bool mono;
  final TextInputType? keyboardType;
  final TextCapitalization textCapitalization;
  final int maxLines;

  /// `true` when the incoming [text] and the text on screen mean the same thing even though they
  /// differ as strings. Lets `12.` survive while `12` arrives back from the form.
  final bool Function(String incoming, String current)? sameValue;

  @override
  State<AhManagedTextField> createState() => _AhManagedTextFieldState();
}

class _AhManagedTextFieldState extends State<AhManagedTextField> {
  late final TextEditingController _controller = TextEditingController(text: widget.text);
  late final FocusNode _focus = FocusNode()..addListener(_onFocusChanged);

  @override
  void didUpdateWidget(AhManagedTextField oldWidget) {
    super.didUpdateWidget(oldWidget);
    final String current = _controller.text;
    if (widget.text == current) return;
    if (widget.sameValue?.call(widget.text, current) ?? false) return;
    _controller.value = TextEditingValue(
      text: widget.text,
      selection: TextSelection.collapsed(offset: widget.text.length),
    );
  }

  void _onFocusChanged() {
    if (!_focus.hasFocus) widget.onBlur();
  }

  @override
  void dispose() {
    _focus
      ..removeListener(_onFocusChanged)
      ..dispose();
    _controller.dispose();
    super.dispose();
  }

  @override
  Widget build(BuildContext context) => AhTextField(
        controller: _controller,
        focusNode: _focus,
        // The caption belongs to the field shell — a schema form draws one atom above every value,
        // and a control that drew its own would produce two. The *message* stays here, because
        // `AhTextField` already renders it in the house style with the invalid border that goes
        // with it, and reimplementing that in this package would be a second opinion about what a
        // wrong field looks like.
        placeholder: widget.placeholder,
        unit: widget.unit,
        mono: widget.mono,
        enabled: widget.enabled,
        readOnly: widget.readOnly,
        maxLines: widget.maxLines,
        keyboardType: widget.keyboardType,
        textCapitalization: widget.textCapitalization,
        error: widget.error,
        onChanged: widget.onTextChanged,
      );
}

/// Free text. `config.multiline` turns it into a box; `config.maxLength` is enforced by validation
/// and not by the keyboard — a hard stop at the limit hides *why* the user cannot type.
class TextControl extends StatelessWidget {
  const TextControl(this.props, {super.key});

  final FieldProps props;

  @override
  Widget build(BuildContext context) {
    final bool multiline = props.field.multiline;
    return AhManagedTextField(
      text: asString(props.value),
      enabled: !props.disabled,
      readOnly: props.field.readOnly,
      error: props.error?.message,
      placeholder: props.field.placeholder,
      unit: props.field.unit,
      maxLines: multiline ? (props.field.rows ?? 4) : 1,
      keyboardType: multiline ? TextInputType.multiline : TextInputType.text,
      onBlur: props.onBlur,
      onTextChanged: (String next) => props.onChanged(next.isEmpty ? null : next),
    );
  }
}
