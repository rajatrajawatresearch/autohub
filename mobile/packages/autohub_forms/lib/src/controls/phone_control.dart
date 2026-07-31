import 'package:flutter/material.dart';

import '../field_config.dart';
import '../form_scope.dart';
import '../types.dart';
import 'money_control.dart';
import 'shared.dart';
import 'text_control.dart';

/// A mobile number, stored E.164 — `+919812345678` — because that is what `/auth/otp/request`
/// takes.
///
/// The dial code is a fixed block to the left of the field rather than part of the text, so a user
/// cannot delete it, cannot type it twice, and cannot paste a number that quietly ends up carrying
/// two country codes. India is phone-first and this field is the front door: the offer, the OTP and
/// every later notification hang off whatever is stored here.
class PhoneControl extends StatelessWidget {
  const PhoneControl(this.props, {super.key});

  final FieldProps props;

  @override
  Widget build(BuildContext context) {
    final FormLabels t = SchemaFormScope.of(context).labels;
    final String dialCode = props.field.dialCode ?? t(FormLabelKeys.phoneDialCode);
    final int nationalLength = props.field.nationalLength ?? 10;

    final String stored = asString(props.value);
    final String national = stored.startsWith(dialCode)
        ? digitsOnly(stored.substring(dialCode.length))
        : digitsOnly(stored);

    return Row(
      crossAxisAlignment: CrossAxisAlignment.start,
      children: <Widget>[
        FixedAffix(text: dialCode),
        Expanded(
          child: AhManagedTextField(
            text: national,
            mono: true,
            enabled: !props.disabled,
            readOnly: props.field.readOnly,
            error: props.error?.message,
            placeholder: props.field.placeholder,
            keyboardType: TextInputType.phone,
            onBlur: props.onBlur,
            onTextChanged: (String next) {
              final String digits = digitsOnly(next);
              final String clipped =
                  digits.length > nationalLength ? digits.substring(0, nationalLength) : digits;
              props.onChanged(clipped.isEmpty ? null : '$dialCode$clipped');
            },
          ),
        ),
      ],
    );
  }
}
