import 'dart:async';
import 'dart:collection';

import 'package:autohub_core/autohub_core.dart';
import 'package:flutter/foundation.dart';

import 'controls/shared.dart';
import 'field_config.dart';
import 'server_errors.dart';
import 'types.dart';
import 'validation.dart';
import 'visibility.dart';

/// Raised by an `onSubmit` that the server refused.
///
/// The app decodes the transport failure — it owns the HTTP client — and throws this so the form
/// can do the two things only the form can do: put each field error back on its own field, and take
/// the user to the step that holds the first of them.
class FormSubmissionRejected implements Exception {
  const FormSubmissionRejected({this.problem, this.message});

  /// The decoded RFC 9457 body, if there was one. `metadata.fieldErrors` is read out of it.
  final Object? problem;

  /// A form-level sentence for failures that belong to no field — a `409`, a gateway timeout.
  final String? message;

  @override
  String toString() => 'FormSubmissionRejected(${message ?? problem})';
}

/// The sell flow's state machine, with no widget in it.
///
/// Three things make this different from an ordinary form controller, and all three come from the
/// fact that the schema is data rather than code:
///
///   1. **Validation is rebuilt on every run.** Visibility is part of the schema, so the moment
///      `fuel_type` becomes `ev`, `battery_health_pct` becomes required. Rules captured at
///      construction would never find out.
///   2. **Faults carry the registry's machine code**, not a code this package made up, so a fault
///      raised here is indistinguishable from one the server raised.
///   3. **A step validates against its own visible field keys only.** A user is never blocked on
///      step two by something they have not been shown yet.
///
/// A [ChangeNotifier] rather than a Riverpod provider on purpose: a package that renders forms must
/// not conscript an app into a state-management choice, and a controller that is merely listenable
/// drops into a `ProviderScope`, a `ChangeNotifierProvider` or a bare `ListenableBuilder` equally
/// well. It is also what makes all of this testable without pumping a widget.
class SchemaFormController extends ChangeNotifier {
  SchemaFormController({
    required this.definition,
    required this.onSubmit,
    FormValues? initialValues,
    this.labels = const FormLabels(),
    DraftStore? draftStore,
    this.draftEntityId,
    Duration autosaveWindow = const Duration(milliseconds: 400),
  })  : _drafts = draftStore,
        _autosave = draftStore == null
            ? null
            : DraftAutosave(draftStore, window: autosaveWindow),
        _values = _seedValues(definition, initialValues) {
    _recompute();
  }

  final FormDefinition definition;

  /// Receives the payload with hidden fields already stripped. May throw [FormSubmissionRejected].
  final Future<void> Function(FormValues payload) onSubmit;

  final FormLabels labels;

  /// The entity this form is about. Two sell drafts for two different cars must not collide, and
  /// the same car reopened must find its own.
  final String? draftEntityId;

  /// `null` disables autosave entirely, which is what a preview and a widget test want.
  final DraftStore? _drafts;

  /// Coalesces per-keystroke saves into one write per quiet period. `autohub_core` owns the
  /// debounce because it owns the disk: a journal flush per character is felt as keyboard lag on
  /// the handsets this product actually ships to.
  final DraftAutosave? _autosave;

  final FormValues _values;
  Map<String, FieldFault> _faults = const <String, FieldFault>{};
  Map<String, FieldFault> _serverFaults = <String, FieldFault>{};
  final Set<String> _touched = <String>{};
  final Set<String> _revealed = <String>{};

  List<FormStep> _steps = const <FormStep>[];
  int _stepIndex = 0;
  int _furthest = 0;
  bool _submitting = false;
  bool _draftRestored = false;
  bool _draftWasStale = false;
  String? _submitError;
  bool _disposed = false;

  /* ── Reading ───────────────────────────────────────────────────────────── */

  /// Live values. Every derived list below is a function of these.
  FormValues get values => UnmodifiableMapView<String, Object?>(_values);

  Object? valueOf(String key) => _values[key];

  /// The steps that apply right now. A step whose fields are all hidden is not in this list.
  List<FormStep> get steps => _steps;

  FormStep? get step => _stepIndex < _steps.length ? _steps[_stepIndex] : null;

  int get stepIndex => _stepIndex;

  /// Visible fields of the current step, in registry order.
  List<FormField> get fields {
    final FormStep? current = step;
    return current == null ? const <FormField>[] : visibleFields(current, _values);
  }

  bool get isFirst => _stepIndex == 0;

  bool get isLast => _steps.isEmpty || _stepIndex == _steps.length - 1;

  /// The furthest step reached, so the progress rail knows what may be jumped back to.
  int get furthest => _furthest;

  bool get submitting => _submitting;

  /// A failure `onSubmit` itself raised — a network fault, a `409`, a server rule the client cannot
  /// see. Distinct from a field fault because it belongs to the form, not to a field.
  String? get submitError => _submitError;

  /// `true` once a saved draft has been folded in, so the form can say so rather than leaving a
  /// seller wondering why half their answers were already filled in.
  bool get draftRestored => _draftRestored;

  /// `true` when the restored draft was composed against an older schema version.
  ///
  /// Not a reason to discard twenty minutes of typing, but a reason to reveal every fault
  /// immediately rather than waiting for the user to touch each field: a newly-required field
  /// should be caught on the phone, not as a `422` the seller cannot explain.
  bool get draftWasStale => _draftWasStale;

  /// `true` while any media in the form is still in flight. The app decides what to do about it;
  /// this package will not silently block a submit on a photograph that may never finish.
  bool get hasUploadsInFlight => _values.values.any(
        (Object? value) => asMediaItems(value)
            .any((MediaItem item) => item.status == UploadStatus.uploading),
      );

  /// The fault to draw beside a field, or `null`.
  ///
  /// A server fault outranks a local one: the server saw something the phone could not — a
  /// duplicate registration, a blocked account — and replacing its answer with a locally-derived
  /// guess would be telling the user something less true than what we were told.
  ///
  /// Local faults surface only once the field has been touched or a gate has revealed them.
  /// Nothing shouts at a user who has not finished typing, and everything corrects itself the
  /// moment they have.
  FieldFault? faultFor(String key) {
    final FieldFault? fromServer = _serverFaults[key];
    if (fromServer != null) return fromServer;
    if (!_touched.contains(key) && !_revealed.contains(key)) return null;
    return _faults[key];
  }

  bool get isCurrentStepValid {
    final FormStep? current = step;
    if (current == null) return true;
    return validateForm(
      definition,
      _values,
      fieldKeys: _keysOf(current),
      labels: labels,
    ).isValid;
  }

  /// The payload, with hidden fields already removed. What `onSubmit` receives.
  FormValues payload() => stripHidden(definition, _values);

  Set<String> _keysOf(FormStep step) => <String>{
        for (final FormField field in visibleFields(step, _values)) field.key,
      };

  /* ── Writing ───────────────────────────────────────────────────────────── */

  /// Records a value and reschedules the autosave.
  ///
  /// The server fault on this field is dropped: the user has changed the thing the server
  /// complained about, so the complaint no longer describes what is on screen. Leaving it would
  /// park a stale rejection under a field the seller has already fixed.
  void setValue(String key, Object? value) {
    if (value == null) {
      if (!_values.containsKey(key)) return;
      _values.remove(key);
    } else {
      if (identical(_values[key], value)) return;
      _values[key] = value;
    }
    _serverFaults.remove(key);
    _recompute();
    _record();
    notifyListeners();
  }

  /// Marks a field as interacted-with, so its fault may now be shown.
  void touch(String key) {
    if (!_touched.add(key)) return;
    notifyListeners();
  }

  void setSubmitError(String? message) {
    if (_submitError == message) return;
    _submitError = message;
    notifyListeners();
  }

  /* ── Navigation ────────────────────────────────────────────────────────── */

  /// Validates the current step and advances if it passes.
  ///
  /// Returns `false` without moving when it does not, having revealed the faults so the user can
  /// see what stopped them. The gate is the explicit validation run, not a side effect of painting
  /// error text — a form that advances or refuses based on what happens to be rendered is a form
  /// whose behaviour depends on scroll position.
  bool goNext() {
    final FormStep? current = step;
    if (current == null) return false;

    final Set<String> keys = _keysOf(current);
    if (!validateForm(definition, _values, fieldKeys: keys, labels: labels).isValid) {
      _revealed.addAll(keys);
      _recompute();
      notifyListeners();
      return false;
    }

    if (_stepIndex < _steps.length - 1) _moveTo(_stepIndex + 1);
    return true;
  }

  void goBack() {
    if (_stepIndex > 0) _moveTo(_stepIndex - 1);
  }

  /// Backward jumps are always allowed; forward jumps are earned by validating.
  ///
  /// A seller who remembers they typed the odometer wrong should not have to finish the form to fix
  /// it. A seller who taps step four from step one has not answered steps two and three.
  void goTo(int index) {
    if (index < 0 || index >= _steps.length || index > _furthest) return;
    _moveTo(index);
  }

  void _moveTo(int index) {
    if (index == _stepIndex) return;
    _stepIndex = index;
    if (index > _furthest) _furthest = index;
    // A step boundary is the one moment worth paying an unconditional disk write for: it is where
    // a user puts the phone down.
    unawaited(saveDraft());
    notifyListeners();
  }

  /* ── Submission ────────────────────────────────────────────────────────── */

  /// Validates everything visible, then hands the stripped payload to [onSubmit].
  ///
  /// Returns `true` only when [onSubmit] completed without raising. A rejected submission leaves
  /// the values exactly as they were — a seller told that their registration is already listed must
  /// not also lose the six photographs they just took.
  ///
  /// The draft is **not** discarded here. Until the caller knows the server accepted the write,
  /// the draft is still the only copy of what the user typed; [clearDraft] is theirs to call.
  Future<bool> submit() async {
    if (_submitting) return false;

    final FormValidation outcome = validateForm(definition, _values, labels: labels);
    if (!outcome.isValid) {
      _revealed.addAll(outcome.fieldErrors.keys);
      _recompute();
      _jumpToFirstOf(outcome.fieldErrors.keys);
      notifyListeners();
      return false;
    }

    _submitting = true;
    _submitError = null;
    notifyListeners();

    try {
      await onSubmit(payload());
      return true;
    } on FormSubmissionRejected catch (rejection) {
      applyServerProblem(rejection.problem, message: rejection.message);
      return false;
    } finally {
      _submitting = false;
      if (!_disposed) notifyListeners();
    }
  }

  /// Applies a `422`'s `metadata.fieldErrors` back onto the right fields, and moves to the first
  /// step that holds one.
  void applyServerProblem(Object? problem, {String? message}) {
    final Map<String, FieldFault> resolved =
        faultsFromProblem(definition, problem, labels: labels);

    _serverFaults = resolved;
    _revealed.addAll(resolved.keys);

    if (message != null) {
      _submitError = message;
    } else if (resolved.isEmpty) {
      _submitError = labels(FormLabelKeys.validationInvalid);
    } else {
      _submitError = null;
    }

    if (resolved.isNotEmpty) _jumpToFirstOf(resolved.keys);
    if (!_disposed) notifyListeners();
  }

  void _jumpToFirstOf(Iterable<String> keys) {
    final int index = stepIndexForFields(_steps, keys);
    if (index < 0 || index == _stepIndex) return;
    _stepIndex = index;
    if (index > _furthest) _furthest = index;
  }

  /* ── Drafts ────────────────────────────────────────────────────────────── */

  /// Folds a saved draft into the current values.
  ///
  /// Recovered without asking, as the architecture requires — a dialogue offering to restore work
  /// the user did not know they had lost is a dialogue they dismiss by reflex, and the twenty
  /// minutes goes with it.
  ///
  /// Keys the current definition no longer declares are dropped. A schema that removed a field
  /// between the save and the restore must not resurrect it into the payload, where the server
  /// would reject the whole submission over a property nobody can see.
  Future<void> restoreDraft() async {
    final DraftStore? store = _drafts;
    if (store == null) return;

    final FormDraft? saved = await store.recover(
      formKey: definition.key,
      entityId: draftEntityId,
    );
    if (_disposed || saved == null || saved.values.isEmpty) return;

    final Set<String> known = <String>{
      for (final FormField field in definition.allFields) field.key,
    };
    bool changed = false;
    for (final MapEntry<String, Object?> entry in saved.values.entries) {
      if (!known.contains(entry.key) || entry.value == null) continue;
      _values[entry.key] = entry.value;
      changed = true;
    }
    if (!changed) return;

    _draftRestored = true;
    _draftWasStale = saved.isStaleAgainst(definition.version);
    // A draft composed against an older schema may be missing a field that has since become
    // required. Revealing everything now turns that into a visible fault on the phone instead of a
    // 422 at the end of the flow.
    if (_draftWasStale) {
      _revealed.addAll(known);
    }
    _recompute();
    notifyListeners();
  }

  /// Writes the pending draft now and waits for it.
  ///
  /// Call from `AppLifecycleState.paused`: Android kills a backgrounded process without further
  /// warning, and a debounce timer does not fire in a process that no longer exists.
  Future<void> saveDraft() async {
    _record();
    await _autosave?.flush();
  }

  /// Discards the draft. Called once a submission has actually landed — never merely because one
  /// was enqueued, since a write still sitting in the outbox is a write the user may yet have to
  /// make again.
  Future<void> clearDraft() async {
    await _drafts?.discard(formKey: definition.key, entityId: draftEntityId);
  }

  void _record() {
    final DraftAutosave? autosave = _autosave;
    if (autosave == null) return;
    autosave.record(
      FormDraft(
        formKey: definition.key,
        entityId: draftEntityId,
        // Deliberately the whole map, hidden fields included. `visibleWhen` is JsonLogic over the
        // form's own values, so a field hidden right now reappears the moment the user switches
        // `fuel_type` back to `ev` — and a draft that had stripped it would have erased their
        // answer. Stripping is the submit path's job, where it is correct exactly once.
        values: Map<String, dynamic>.of(_values),
        schemaVersion: definition.version,
        updatedAt: DateTime.now().toUtc(),
      ),
    );
  }

  /* ── Derivation ────────────────────────────────────────────────────────── */

  void _recompute() {
    _steps = visibleSteps(definition, _values);
    // A step can disappear underneath the user: answering "petrol" removes the EV step they were
    // standing on. Clamping rather than resetting keeps them as close to where they were as the
    // new shape of the form allows.
    if (_stepIndex >= _steps.length) _stepIndex = _steps.isEmpty ? 0 : _steps.length - 1;
    if (_furthest >= _steps.length) _furthest = _steps.isEmpty ? 0 : _steps.length - 1;
    _faults = validateForm(definition, _values, labels: labels).fieldErrors;
  }

  /// Registry `defaultValue`s first, then whatever the caller loaded. A saved draft always wins.
  static FormValues _seedValues(FormDefinition definition, FormValues? provided) {
    final FormValues seeded = <String, Object?>{};
    for (final FormField field in definition.allFields) {
      if (field.defaultValue != null) seeded[field.key] = field.defaultValue;
    }
    if (provided != null) {
      for (final MapEntry<String, Object?> entry in provided.entries) {
        if (entry.value == null) continue;
        seeded[entry.key] = entry.value;
      }
    }
    return seeded;
  }

  @override
  void dispose() {
    _disposed = true;
    // Flushes whatever the debounce is still holding. A seller backing out of the sell flow has
    // not decided to throw away the four answers they just gave.
    unawaited(_autosave?.dispose());
    super.dispose();
  }
}
