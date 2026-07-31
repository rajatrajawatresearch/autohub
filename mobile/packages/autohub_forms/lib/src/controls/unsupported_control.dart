import 'package:autohub_ui/autohub_ui.dart';
import 'package:flutter/material.dart';

import '../form_scope.dart';
import '../types.dart';
import 'field_chrome.dart';

/// What a registry row this build has never heard of looks like.
///
/// This is the single most important widget in the package, and it is the most boring one.
///
/// The registries are versioned and edited by people who are not shipping this binary. Sooner or
/// later a form schema will name a control that shipped after the phone in somebody's hand — and on
/// mobile that gap is not minutes, it is however long it takes a user to update, which for a
/// meaningful share of Indian handsets is never. When it happens, the sell flow must lose one field
/// rather than the whole screen, because that screen is where supply comes from.
///
/// So: a visible, labelled, deliberately unexciting placeholder carrying the field key and the
/// control name in mono — which is exactly the string support needs to hear read back.
///
/// It renders in the caution tone rather than the flag tone on purpose. Nothing is broken. This
/// build is simply behind, and the copy says so and says what to do about it.
class UnsupportedControl extends StatelessWidget {
  const UnsupportedControl(this.props, {super.key});

  final FieldProps props;

  /// The marker a widget test asserts on, so "an unknown control degrades" is a checked claim and
  /// not a comment.
  static const Key placeholderKey = Key('ah.form.unsupported');

  @override
  Widget build(BuildContext context) {
    final AhSurfaces surfaces = context.ahSurfaces;
    final FormLabels t = SchemaFormScope.of(context).labels;

    return Semantics(
      key: placeholderKey,
      container: true,
      label: '${t(FormLabelKeys.unsupportedTitle)}. ${props.field.label}',
      child: DecoratedBox(
        decoration: BoxDecoration(
          color: surfaces.sunken,
          border: const Border(left: BorderSide(color: AhColors.caution, width: 2)),
        ),
        child: Padding(
          padding: const EdgeInsets.symmetric(horizontal: AhSpace.md, vertical: AhSpace.sm),
          child: Column(
            crossAxisAlignment: CrossAxisAlignment.start,
            children: <Widget>[
              AhLabelText(t(FormLabelKeys.unsupportedTitle)),
              const SizedBox(height: AhSpace.xs),
              Text(
                t(FormLabelKeys.unsupportedBody),
                style: AhText.bodySm.copyWith(color: surfaces.textPrimary),
              ),
              const SizedBox(height: AhSpace.xs),
              Text(
                '${props.field.key} · ${props.field.control.name}',
                style: AhText.label.copyWith(color: surfaces.textSecondary),
              ),
            ],
          ),
        ),
      ),
    );
  }
}
