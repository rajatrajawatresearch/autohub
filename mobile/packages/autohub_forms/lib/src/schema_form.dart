import 'dart:async';

import 'package:autohub_core/autohub_core.dart';
import 'package:autohub_ui/autohub_ui.dart';
import 'package:flutter/material.dart' hide FormField;

import 'control_registry.dart';
import 'controls/field_chrome.dart';
import 'form_controller.dart';
import 'form_scope.dart';
import 'types.dart';

/// One widget. Every form in the product.
///
/// The sell flow, dealer onboarding, the 200-point inspection checklist and the loan application
/// are this file rendering different JSON from the Form Schema Registry. Nothing here knows what a
/// car is, what a fuel type is, or what a dealer has to prove — it knows steps, fields, controls and
/// rules, and every one of those arrives over the wire.
///
/// This is the claim in `docs/10-mobile-architecture.md` §1 made testable: an operations user adds
/// `battery_health_pct` to the EV category, and the field appears here on the next launch with its
/// validation, its unit and its conditional visibility — nothing rebuilt, nothing submitted to an
/// app store. If that ever stops being true, every registry in the platform is decoration.
///
/// Shape on screen: an RC card. An uppercase letter-spaced caption above every value, hairline
/// rules for structure, near-square corners, no elevation. The progress rail is a row of stamps
/// rather than a bar filling up — a form is a document being completed, not a game.
///
/// Motion: none. Steps cut, they do not slide. A transition between steps of a twenty-minute form
/// is time spent watching an animation instead of reading the next question.
///
/// The widget lays out a column and does not scroll. The page decides that, because the page knows
/// whether this form shares a screen with anything else.
class SchemaForm extends StatefulWidget {
  const SchemaForm({
    required this.definition,
    required this.onSubmit,
    this.initialValues,
    this.labels = const FormLabels(),
    this.loadOptions,
    this.uploader,
    this.draftStore,
    this.draftEntityId,
    this.showTitle = true,
    this.error,
    this.onStepChanged,
    this.onControllerCreated,
    super.key,
  });

  /// Straight from `GET /platform/forms/{key}`.
  final FormDefinition definition;

  /// Receives the payload with hidden fields already removed.
  ///
  /// Throw [FormSubmissionRejected] to hand a `422` back: the form puts each field error on its own
  /// field and walks the user to the step that holds the first one.
  final Future<void> Function(FormValues payload) onSubmit;

  final FormValues? initialValues;

  /// Localisation-registry strings for the renderer's own chrome. Field labels come from the schema.
  final FormLabels labels;

  /// Fetches option lists from the taxonomy and geography registries.
  final FormOptionsLoader? loadOptions;

  /// Uploads files for `file` and `photo_set`.
  final FormFileUploader? uploader;

  /// `autohub_core`'s draft store. `null` disables autosave, which is what a preview wants.
  final DraftStore? draftStore;

  /// The entity this form is about, so two drafts for two cars do not collide.
  final String? draftEntityId;

  /// The page usually owns the heading; set `false` when it does.
  final bool showTitle;

  /// A failure the form itself could not have caught — a gateway timeout, a `409`.
  final String? error;

  final void Function(FormStep step, int index)? onStepChanged;

  /// Handed the controller once, so a page can read values, save on background, discard the draft
  /// after a write lands, or apply a problem body it decoded itself.
  final void Function(SchemaFormController controller)? onControllerCreated;

  @override
  State<SchemaForm> createState() => _SchemaFormState();
}

class _SchemaFormState extends State<SchemaForm> with WidgetsBindingObserver {
  late SchemaFormController _controller;
  int _lastStep = -1;

  @override
  void initState() {
    super.initState();
    WidgetsBinding.instance.addObserver(this);
    _install(_create());
  }

  SchemaFormController _create() => SchemaFormController(
        definition: widget.definition,
        onSubmit: widget.onSubmit,
        initialValues: widget.initialValues,
        labels: widget.labels,
        draftStore: widget.draftStore,
        draftEntityId: widget.draftEntityId,
      );

  void _install(SchemaFormController controller) {
    _controller = controller;
    widget.onControllerCreated?.call(controller);
    controller.addListener(_onControllerChanged);
    unawaited(controller.restoreDraft());
  }

  @override
  void didChangeAppLifecycleState(AppLifecycleState state) {
    // Android kills a backgrounded process without further warning, and a debounce timer does not
    // fire in a process that no longer exists. This is the write that makes "a half-finished sell
    // form survives the tunnel" true rather than aspirational.
    if (state == AppLifecycleState.paused || state == AppLifecycleState.inactive) {
      unawaited(_controller.saveDraft());
    }
  }

  @override
  void didUpdateWidget(SchemaForm oldWidget) {
    super.didUpdateWidget(oldWidget);
    // A different document is a different form — a registry publish landing while the screen is
    // open, or a category change swapping `sell_vehicle` for its commercial variant. Rebuilding the
    // controller is right; carrying answers across two different documents is not.
    if (oldWidget.definition.key == widget.definition.key &&
        oldWidget.definition.version == widget.definition.version) {
      return;
    }
    _controller
      ..removeListener(_onControllerChanged)
      ..dispose();
    _install(_create());
  }

  void _onControllerChanged() {
    final int index = _controller.stepIndex;
    if (index == _lastStep) return;
    _lastStep = index;
    final FormStep? step = _controller.step;
    if (step != null) widget.onStepChanged?.call(step, index);
  }

  @override
  void dispose() {
    WidgetsBinding.instance.removeObserver(this);
    _controller
      ..removeListener(_onControllerChanged)
      // Disposing flushes whatever the debounce is still holding. A seller backing out of the sell
      // flow has not decided to throw away the four answers they just gave.
      ..dispose();
    super.dispose();
  }

  @override
  Widget build(BuildContext context) => SchemaFormScope(
        controller: _controller,
        labels: widget.labels,
        loadOptions: widget.loadOptions,
        uploader: widget.uploader,
        child: ListenableBuilder(
          listenable: _controller,
          builder: (BuildContext context, Widget? _) => _FormBody(
            controller: _controller,
            labels: widget.labels,
            showTitle: widget.showTitle,
            error: widget.error,
          ),
        ),
      );
}

class _FormBody extends StatelessWidget {
  const _FormBody({
    required this.controller,
    required this.labels,
    required this.showTitle,
    required this.error,
  });

  final SchemaFormController controller;
  final FormLabels labels;
  final bool showTitle;
  final String? error;

  @override
  Widget build(BuildContext context) {
    final AhSurfaces surfaces = context.ahSurfaces;
    final FormDefinition definition = controller.definition;
    final FormStep? step = controller.step;
    final bool multiStep = controller.steps.length > 1;
    final String? failure = error ?? controller.submitError;

    return Column(
      crossAxisAlignment: CrossAxisAlignment.stretch,
      children: <Widget>[
        if (showTitle && definition.title.isNotEmpty) ...<Widget>[
          Text(definition.title, style: AhText.heading.copyWith(color: surfaces.textPrimary)),
          if (definition.description != null) ...<Widget>[
            const SizedBox(height: AhSpace.xs),
            Text(
              definition.description!,
              style: AhText.body.copyWith(color: surfaces.textSecondary),
            ),
          ],
          const SizedBox(height: AhSpace.lg),
          Divider(height: 1, thickness: 1, color: surfaces.borderHairline),
          const SizedBox(height: AhSpace.lg),
        ],
        if (multiStep) ...<Widget>[
          _ProgressRail(controller: controller, labels: labels),
          const SizedBox(height: AhSpace.lg),
        ],
        if (step == null)
          AhQuietText(labels(FormLabelKeys.selectEmpty))
        else ...<Widget>[
          Text(step.title, style: AhText.subheading.copyWith(color: surfaces.textPrimary)),
          if (step.description != null) ...<Widget>[
            const SizedBox(height: AhSpace.xs),
            Text(
              step.description!,
              style: AhText.bodySm.copyWith(color: surfaces.textSecondary),
            ),
          ],
          const SizedBox(height: AhSpace.lg),
          for (final FormField field in controller.fields) ...<Widget>[
            _FieldRow(controller: controller, field: field, labels: labels),
            const SizedBox(height: AhSpace.xl),
          ],
        ],
        if (controller.draftRestored) ...<Widget>[
          AhQuietText(labels(FormLabelKeys.draftRestored)),
          const SizedBox(height: AhSpace.md),
        ],
        if (failure != null) ...<Widget>[
          AhFaultBlock(message: failure, emphasis: true),
          const SizedBox(height: AhSpace.md),
        ],
        Divider(height: 1, thickness: 1, color: surfaces.borderHairline),
        const SizedBox(height: AhSpace.md),
        _Footer(controller: controller, labels: labels, multiStep: multiStep),
      ],
    );
  }
}

/* ── The progress rail ─────────────────────────────────────────────────────── */

/// A row of stamps, not a filling bar.
///
/// The web draws a vertical column of numbered steps on a wide screen and collapses to this on a
/// handset. There is no wide screen here, so this is the only presentation: the caption, one
/// segment per step, and the current step's title above the fields. Completed segments are
/// tappable — a seller who remembers they typed the odometer wrong should not have to finish the
/// form to fix it.
class _ProgressRail extends StatelessWidget {
  const _ProgressRail({required this.controller, required this.labels});

  final SchemaFormController controller;
  final FormLabels labels;

  @override
  Widget build(BuildContext context) {
    final AhSurfaces surfaces = context.ahSurfaces;
    final List<FormStep> steps = controller.steps;
    final int current = controller.stepIndex;
    final String progress = labels(FormLabelKeys.stepProgress, <String, Object?>{
      'current': current + 1,
      'total': steps.length,
    });

    return Semantics(
      label: progress,
      child: Column(
        crossAxisAlignment: CrossAxisAlignment.stretch,
        children: <Widget>[
          AhLabelText(progress),
          const SizedBox(height: AhSpace.sm),
          Row(
            children: <Widget>[
              for (int index = 0; index < steps.length; index += 1) ...<Widget>[
                Expanded(
                  child: GestureDetector(
                    onTap: index <= controller.furthest ? () => controller.goTo(index) : null,
                    behavior: HitTestBehavior.opaque,
                    child: Padding(
                      // The bar is 3dp; the target around it is not. A rail nobody can hit is
                      // decoration, and this one is the only way back to step one.
                      padding: const EdgeInsets.symmetric(vertical: AhSpace.md),
                      child: ColoredBox(
                        color: index <= current ? surfaces.accentPrimary : surfaces.sunken,
                        child: const SizedBox(height: 3),
                      ),
                    ),
                  ),
                ),
                if (index < steps.length - 1) const SizedBox(width: AhSpace.xs),
              ],
            ],
          ),
        ],
      ),
    );
  }
}

/* ── The field shell ───────────────────────────────────────────────────────── */

/// Caption, control, help, fault — the RC-card atom, wrapped around whatever the registry named.
///
/// Two of the four are conditional on what the control already draws. A plate wears its own caption
/// and its own message; a toggle's caption *is* the row it sits in. The shell always contributes
/// the machine code, because that is the part no control knows to draw and the part support needs:
/// a seller sends a screenshot, support reads `kms.too_high`, and nobody has to guess which of four
/// validation rules fired.
class _FieldRow extends StatelessWidget {
  const _FieldRow({required this.controller, required this.field, required this.labels});

  final SchemaFormController controller;
  final FormField field;
  final FormLabels labels;

  @override
  Widget build(BuildContext context) {
    final AhSurfaces surfaces = context.ahSurfaces;
    final ControlEntry entry = resolveControl(field.control);
    final FieldFault? fault = controller.faultFor(field.key);
    final bool ownsLabel = entry.ownsLabel?.call(field) ?? false;
    final bool ownsError = entry.ownsError?.call(field) ?? false;

    final FieldProps props = FieldProps(
      field: field,
      value: controller.valueOf(field.key),
      invalid: fault != null,
      disabled: controller.submitting,
      error: fault,
      onChanged: (Object? next) => controller.setValue(field.key, next),
      onBlur: () => controller.touch(field.key),
    );

    return Column(
      crossAxisAlignment: CrossAxisAlignment.stretch,
      children: <Widget>[
        if (!ownsLabel) ...<Widget>[
          Row(
            crossAxisAlignment: CrossAxisAlignment.end,
            children: <Widget>[
              Expanded(child: AhLabelText(field.label)),
              if (field.unit != null)
                Text(field.unit!, style: AhText.label.copyWith(color: surfaces.textSecondary)),
              if (!field.isRequired) ...<Widget>[
                const SizedBox(width: AhSpace.sm),
                Text(
                  labels(FormLabelKeys.fieldOptional),
                  style: AhText.label.copyWith(color: surfaces.textSecondary),
                ),
              ],
            ],
          ),
          const SizedBox(height: AhSpace.sm),
        ],

        // Keyed by field key so a visibility change that swaps one control for another does not
        // hand the new one the old one's State — which is how a battery slider ends up wearing an
        // odometer's text.
        KeyedSubtree(key: ValueKey<String>('ah.field.${field.key}'), child: entry.build(props)),

        if (field.helpText != null && !ownsLabel) ...<Widget>[
          const SizedBox(height: AhSpace.xs),
          AhQuietText(field.helpText!),
        ],

        if (fault != null) ...<Widget>[
          const SizedBox(height: AhSpace.sm),
          if (ownsError)
            // The control already said what is wrong, in the house style. All that is missing is
            // the code.
            Align(
              alignment: Alignment.centerLeft,
              child: Text(
                fault.code,
                style: AhText.label.copyWith(color: surfaces.textSecondary),
              ),
            )
          else
            AhFaultBlock(message: fault.message, code: fault.code),
        ],
      ],
    );
  }
}

/* ── Footer ────────────────────────────────────────────────────────────────── */

class _Footer extends StatelessWidget {
  const _Footer({required this.controller, required this.labels, required this.multiStep});

  final SchemaFormController controller;
  final FormLabels labels;
  final bool multiStep;

  @override
  Widget build(BuildContext context) {
    final AhSurfaces surfaces = context.ahSurfaces;
    final FormDefinition definition = controller.definition;
    final bool submitting = controller.submitting;
    final bool last = controller.isLast;

    return Column(
      crossAxisAlignment: CrossAxisAlignment.stretch,
      children: <Widget>[
        Row(
          children: <Widget>[
            if (multiStep && !controller.isFirst) ...<Widget>[
              AhButton(
                label: labels(FormLabelKeys.actionBack),
                variant: AhButtonVariant.outline,
                onPressed: submitting ? null : controller.goBack,
              ),
              const SizedBox(width: AhSpace.md),
            ],
            Expanded(
              child: last
                  // Green carries the flow; the plate yellow is spent once, on the act that ends
                  // it. That is the rationing rule from the design system, applied to a form.
                  ? AhButton(
                      label: submitting
                          ? labels(FormLabelKeys.actionWorking)
                          // The contract defaults `submitLabel` to "Continue", so there is always
                          // an authored string here and the chrome fallback never fires.
                          : definition.submitLabel,
                      variant: AhButtonVariant.action,
                      block: true,
                      onPressed: submitting ? null : () => unawaited(controller.submit()),
                    )
                  : AhButton(
                      label: labels(FormLabelKeys.actionNext),
                      block: true,
                      onPressed: submitting ? null : () => controller.goNext(),
                    ),
            ),
          ],
        ),
        const SizedBox(height: AhSpace.sm),
        // The schema's identity, in mono, always on screen. When a seller reports a field behaving
        // oddly, the first question is which version of the document they were looking at.
        Text(
          '${definition.key} · v${definition.version}',
          textAlign: TextAlign.right,
          style: AhText.label.copyWith(color: surfaces.textSecondary),
        ),
      ],
    );
  }
}
