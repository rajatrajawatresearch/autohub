import 'package:autohub_core/autohub_core.dart';
import 'package:autohub_ui/autohub_ui.dart';
import 'package:flutter/material.dart';

import '../field_config.dart';
import '../form_scope.dart';
import '../types.dart';
import 'shared.dart';
import 'text_control.dart';

/// Long-form prose — a listing description, a rejection reason, an inspector's note.
///
/// Deliberately a plain box with a live character count and no formatting toolbar. Nothing
/// downstream renders rich text: the same description appears in a search result, an SMS, a
/// WhatsApp card and a PDF, and every one of those wants plain characters. A bold button here would
/// be a promise four other surfaces cannot keep.
///
/// The limit comes off the registry's own `maxLength` rule rather than from `config`, so the count
/// and the validator can never disagree about what the ceiling is.
class RichTextControl extends StatelessWidget {
  const RichTextControl(this.props, {super.key});

  final FieldProps props;

  @override
  Widget build(BuildContext context) {
    final FormLabels t = SchemaFormScope.of(context).labels;
    final String text = asString(props.value);

    // The ceiling comes off the registry's own `maxLength` rule rather than from `config`, so the
    // live count and the validator can never disagree about what the limit is.
    int? limit;
    for (final ValidationRule rule in props.field.validation) {
      if (rule is! ValidationRuleMaxLength) continue;
      limit = rule.value;
      break;
    }

    return Column(
      crossAxisAlignment: CrossAxisAlignment.stretch,
      children: <Widget>[
        AhManagedTextField(
          text: text,
          enabled: !props.disabled,
          readOnly: props.field.readOnly,
          error: props.error?.message,
          placeholder: props.field.placeholder,
          maxLines: props.field.rows ?? 6,
          keyboardType: TextInputType.multiline,
          onBlur: props.onBlur,
          onTextChanged: (String next) => props.onChanged(next.isEmpty ? null : next),
        ),
        const SizedBox(height: AhSpace.xs),
        Align(
          alignment: Alignment.centerRight,
          child: Text(
            limit == null
                ? t(FormLabelKeys.richTextCount, <String, Object?>{'count': text.length})
                : '${text.length}/$limit',
            style: AhText.label.copyWith(color: context.ahSurfaces.textSecondary),
          ),
        ),
      ],
    );
  }
}
