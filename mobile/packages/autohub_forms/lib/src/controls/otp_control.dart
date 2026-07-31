import 'package:autohub_ui/autohub_ui.dart';
import 'package:flutter/material.dart';
import 'package:flutter/services.dart';

import '../field_config.dart';
import '../form_scope.dart';
import '../types.dart';
import 'field_chrome.dart';
import 'shared.dart';

/// The SMS code.
///
/// Separate boxes because that is what a one-time code looks like everywhere, but **one string
/// underneath**: a compact, left-aligned run of digits with no padding character and no per-box
/// state. Modelling it as slots would invent states the verify endpoint cannot represent — "box 4
/// filled, boxes 1–3 empty" is not a code — and would then have to decide what to send for them.
///
/// Only the first box claims the autofill hint. Six competing one-time-code fields make the OS give
/// up and fill none of them, and on Android that failure is silent.
class OtpControl extends StatefulWidget {
  const OtpControl(this.props, {super.key});

  final FieldProps props;

  @override
  State<OtpControl> createState() => _OtpControlState();
}

class _OtpControlState extends State<OtpControl> {
  List<FocusNode> _nodes = const <FocusNode>[];
  List<TextEditingController> _boxes = const <TextEditingController>[];

  int get _length => widget.props.field.otpLength ?? 6;

  String get _code {
    final String digits = digitsOnly(asString(widget.props.value));
    return digits.length > _length ? digits.substring(0, _length) : digits;
  }

  @override
  void initState() {
    super.initState();
    _build();
  }

  @override
  void didUpdateWidget(OtpControl oldWidget) {
    super.didUpdateWidget(oldWidget);
    if (_boxes.length != _length) {
      _teardown();
      _build();
      return;
    }
    _paint();
  }

  void _build() {
    _nodes = List<FocusNode>.generate(_length, (int _) => FocusNode());
    _boxes = List<TextEditingController>.generate(_length, (int _) => TextEditingController());
    _paint();
  }

  void _paint() {
    final String code = _code;
    for (int index = 0; index < _boxes.length; index += 1) {
      final String character = index < code.length ? code[index] : '';
      if (_boxes[index].text != character) _boxes[index].text = character;
    }
  }

  void _teardown() {
    for (final FocusNode node in _nodes) {
      node.dispose();
    }
    for (final TextEditingController box in _boxes) {
      box.dispose();
    }
  }

  @override
  void dispose() {
    _teardown();
    super.dispose();
  }

  void _commit(String next) {
    final String clipped = next.length > _length ? next.substring(0, _length) : next;
    widget.props.onChanged(clipped.isEmpty ? null : clipped);
  }

  void _onBoxChanged(int index, String raw) {
    final String digits = digitsOnly(raw);
    final List<String> slots = List<String>.generate(
      _length,
      (int slot) => slot < _code.length ? _code[slot] : '',
    );

    if (digits.isEmpty) {
      slots[index] = '';
      _commit(slots.join());
      return;
    }

    // A paste fills from this box onward and lands the caret after whatever arrived. Android's
    // SMS autofill delivers the whole code into whichever box has focus, so this is the common
    // path, not the exotic one.
    for (int offset = 0; index + offset < _length && offset < digits.length; offset += 1) {
      slots[index + offset] = digits[offset];
    }
    _commit(slots.join());
    final int target = (index + digits.length).clamp(0, _length - 1);
    _nodes[target].requestFocus();
  }

  KeyEventResult _onKey(int index, KeyEvent event) {
    if (event is! KeyDownEvent) return KeyEventResult.ignored;
    if (event.logicalKey != LogicalKeyboardKey.backspace) return KeyEventResult.ignored;
    if (_boxes[index].text.isNotEmpty || index == 0) return KeyEventResult.ignored;

    // Backspace on an empty box walks left and clears the one it lands on — otherwise a user who
    // mistypes the third digit has to tap the box before they can fix it.
    final List<String> slots = List<String>.generate(
      _length,
      (int slot) => slot < _code.length ? _code[slot] : '',
    );
    slots[index - 1] = '';
    _commit(slots.join());
    _nodes[index - 1].requestFocus();
    return KeyEventResult.handled;
  }

  @override
  Widget build(BuildContext context) {
    final FormLabels t = SchemaFormScope.of(context).labels;
    final FieldProps props = widget.props;

    return Column(
      crossAxisAlignment: CrossAxisAlignment.start,
      children: <Widget>[
        Wrap(
          spacing: AhSpace.sm,
          runSpacing: AhSpace.sm,
          children: <Widget>[
            for (int index = 0; index < _length; index += 1)
              SizedBox(
                width: 44,
                height: 52,
                child: Focus(
                  onKeyEvent: (FocusNode _, KeyEvent event) => _onKey(index, event),
                  child: TextField(
                    controller: _boxes[index],
                    focusNode: _nodes[index],
                    enabled: !props.locked,
                    textAlign: TextAlign.center,
                    keyboardType: TextInputType.number,
                    autofillHints:
                        index == 0 ? const <String>[AutofillHints.oneTimeCode] : null,
                    style: AhText.plate.copyWith(fontSize: 20, letterSpacing: 0, color: AhColors.tarmac),
                    cursorColor: AhColors.highway,
                    onChanged: (String raw) => _onBoxChanged(index, raw),
                    onTapOutside: (PointerDownEvent _) => props.onBlur(),
                    decoration: InputDecoration(
                      counterText: '',
                      filled: true,
                      fillColor: AhColors.plate,
                      contentPadding: EdgeInsets.zero,
                      border: _plateBorder(props.invalid),
                      enabledBorder: _plateBorder(props.invalid),
                      focusedBorder: _plateBorder(props.invalid),
                      disabledBorder: _plateBorder(props.invalid),
                    ),
                  ),
                ),
              ),
          ],
        ),
        const SizedBox(height: AhSpace.sm),
        AhQuietText(t(FormLabelKeys.otpHint, <String, Object?>{'length': _length})),
      ],
    );
  }
}

/// Each box is a plate: mono, hard-bordered, zero radius. The same visual grammar as the thing the
/// user is about to unlock.
OutlineInputBorder _plateBorder(bool invalid) => OutlineInputBorder(
      borderRadius: const BorderRadius.all(AhRadius.plate),
      borderSide: BorderSide(color: invalid ? AhColors.flag : AhColors.tarmac, width: 2),
    );
