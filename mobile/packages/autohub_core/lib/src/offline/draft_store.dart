import 'dart:async';
import 'dart:convert';

import 'package:drift/drift.dart';
import 'package:flutter/foundation.dart';

import '../storage/ah_database.dart';

/// One autosaved schema form.
@immutable
final class FormDraft {
  const FormDraft({
    required this.formKey,
    required this.values,
    required this.updatedAt,
    this.entityId,
    this.schemaVersion,
  });

  /// The registry key of the form — `sell_vehicle`, `inspection_checklist`.
  final String formKey;

  /// The entity being edited, or null for a new one.
  final String? entityId;

  /// Everything the user has typed, including values for fields that are currently hidden.
  ///
  /// Hidden fields are kept on purpose. `visibleWhen` is JsonLogic over the form's own values, so a
  /// field hidden right now becomes visible again the moment the user switches `fuel_type` back to
  /// `ev` — and discarding its value here would silently erase what they had already entered.
  /// Stripping hidden fields is the *submit* path's job, where it is correct exactly once.
  final Map<String, dynamic> values;

  /// The form definition version this data was shaped by. See `DraftRows.schemaVersion`.
  final int? schemaVersion;

  final DateTime updatedAt;

  /// True when the live form definition has moved past what this draft was composed against.
  ///
  /// Not a reason to throw the draft away — twenty minutes of a user's typing is worth more than a
  /// clean payload — but a reason for the renderer to re-run validation before letting them submit,
  /// so a newly-required field is caught on the phone rather than as a `422` they cannot explain.
  bool isStaleAgainst(int currentSchemaVersion) =>
      schemaVersion != null && schemaVersion! < currentSchemaVersion;

  FormDraft copyWith({Map<String, dynamic>? values, int? schemaVersion, DateTime? updatedAt}) =>
      FormDraft(
        formKey: formKey,
        entityId: entityId,
        values: values ?? this.values,
        schemaVersion: schemaVersion ?? this.schemaVersion,
        updatedAt: updatedAt ?? this.updatedAt,
      );

  @override
  String toString() => 'FormDraft($formKey/${entityId ?? 'new'}, '
      'v$schemaVersion, ${values.length} values)';
}

/// Where a half-finished form lives between the tunnel and the other side of it.
///
/// The requirement from §4 is short and absolute: recovered on relaunch **without asking**. A
/// "restore your draft?" dialog is a question the user cannot answer — they have no idea what is in
/// the draft until they see it — and answering it wrong loses an inspection that took twenty
/// minutes to fill in standing in a car park.
final class DraftStore {
  DraftStore(this._db, {DateTime Function()? clock}) : _clock = clock ?? _wallClock;

  static DateTime _wallClock() => DateTime.now().toUtc();

  /// SQLite composite primary keys do not treat NULL as a value, so two drafts for two new
  /// listings would both be "the NULL row" and overwrite each other. See `DraftRows.entityId`.
  static const String _newEntity = '';

  final AhDatabase _db;
  final DateTime Function() _clock;

  Future<void> save(FormDraft draft) {
    return _db.into(_db.draftRows).insertOnConflictUpdate(
          DraftRowsCompanion.insert(
            formKey: draft.formKey,
            entityId: draft.entityId ?? _newEntity,
            values: jsonEncode(draft.values),
            schemaVersion: Value(draft.schemaVersion),
            updatedAt: _clock(),
          ),
        );
  }

  /// Convenience for the renderer's per-keystroke path.
  Future<void> record({
    required String formKey,
    required Map<String, dynamic> values,
    String? entityId,
    int? schemaVersion,
  }) =>
      save(
        FormDraft(
          formKey: formKey,
          entityId: entityId,
          values: values,
          schemaVersion: schemaVersion,
          updatedAt: _clock(),
        ),
      );

  Future<FormDraft?> recover({required String formKey, String? entityId}) async {
    final query = _db.select(_db.draftRows)
      ..where((row) => row.formKey.equals(formKey))
      ..where((row) => row.entityId.equals(entityId ?? _newEntity));
    final row = await query.getSingleOrNull();
    return row == null ? null : _toDraft(row);
  }

  Stream<FormDraft?> watch({required String formKey, String? entityId}) {
    final query = _db.select(_db.draftRows)
      ..where((row) => row.formKey.equals(formKey))
      ..where((row) => row.entityId.equals(entityId ?? _newEntity));
    return query.watchSingleOrNull().map((row) => row == null ? null : _toDraft(row));
  }

  /// Removes a draft. Called after a successful submit — and only then.
  ///
  /// Not after *enqueuing* a submit: until the outbox says the server accepted it, the draft is
  /// still the only copy of what the user typed, and a poisoned write with no draft behind it is
  /// the twenty minutes gone.
  Future<void> discard({required String formKey, String? entityId}) async {
    final delete = _db.delete(_db.draftRows)
      ..where((row) => row.formKey.equals(formKey))
      ..where((row) => row.entityId.equals(entityId ?? _newEntity));
    await delete.go();
  }

  Future<List<FormDraft>> all() async {
    final query = _db.select(_db.draftRows)
      ..orderBy([(row) => OrderingTerm.desc(row.updatedAt)]);
    return (await query.get()).map(_toDraft).toList();
  }

  /// Housekeeping for drafts nobody came back to.
  ///
  /// Run on launch with a generous window. A month is not a guess about attention span — it is
  /// long enough that anything older is a form the user has forgotten exists, and short enough
  /// that an abandoned 40-photo inspection stops occupying a budget handset's storage forever.
  Future<int> purgeOlderThan(Duration age) {
    final cutoff = _clock().subtract(age);
    final delete = _db.delete(_db.draftRows)
      ..where((row) => row.updatedAt.isSmallerThanValue(cutoff));
    return delete.go();
  }

  Future<int> clear() => _db.delete(_db.draftRows).go();

  static FormDraft _toDraft(DraftRow row) => FormDraft(
        formKey: row.formKey,
        entityId: row.entityId == _newEntity ? null : row.entityId,
        values: _decodeValues(row.values),
        schemaVersion: row.schemaVersion,
        updatedAt: row.updatedAt,
      );

  static Map<String, dynamic> _decodeValues(String raw) {
    try {
      final decoded = jsonDecode(raw);
      return decoded is Map<String, dynamic> ? decoded : const {};
    } on FormatException {
      // An unreadable draft is an empty form, not a crash on the screen that creates supply.
      return const {};
    }
  }
}

/// Coalesces per-keystroke saves into one write per quiet period.
///
/// A `SchemaForm` fires a change on every character. Writing SQLite on each one is a synchronous
/// journal flush per keystroke, which on a ₹6,000 Android is felt as the keyboard lagging behind
/// the user's thumb — and the sell flow is the one screen in the product that must never feel
/// cheap, because it is the screen that creates supply.
///
/// The window is short enough that "how much can I lose?" has a boring answer: at most the last
/// few hundred milliseconds of typing, and [flush] closes even that whenever the form is
/// backgrounded or a step is completed.
final class DraftAutosave {
  DraftAutosave(this._store, {this.window = const Duration(milliseconds: 400)});

  final DraftStore _store;
  final Duration window;

  Timer? _timer;
  FormDraft? _latest;
  Future<void> _lastWrite = Future<void>.value();
  bool _disposed = false;

  /// Records the newest form state. Only the newest survives — an intermediate keystroke has no
  /// value once a later one exists.
  void record(FormDraft draft) {
    if (_disposed) return;
    _latest = draft;
    _timer?.cancel();
    _timer = Timer(window, () => unawaited(flush()));
  }

  /// Writes the pending draft now and waits for it.
  ///
  /// Call from `AppLifecycleState.paused`. Android can kill a backgrounded process without further
  /// warning, and the debounce timer will not fire in a process that no longer exists.
  Future<void> flush() {
    _timer?.cancel();
    _timer = null;

    final pending = _latest;
    if (pending == null) return _lastWrite;
    _latest = null;

    // Chained rather than concurrent so two flushes cannot race into writing the older state last.
    _lastWrite = _lastWrite.then((_) => _store.save(pending));
    return _lastWrite;
  }

  Future<void> dispose() async {
    await flush();
    _disposed = true;
    _timer?.cancel();
    _timer = null;
  }
}
