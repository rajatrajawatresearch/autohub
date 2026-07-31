import 'dart:async';

import 'package:autohub_core/autohub_core.dart';
import 'package:flutter/material.dart' hide FormField;

import 'form_controller.dart';
import 'types.dart';

/// The services a rendered form needs but must not own.
///
/// A form renderer that fetched its own option lists would be a form renderer that knew about the
/// taxonomy API; one that uploaded its own files would be one that knew about the media pipeline.
/// Both are injected, which is what keeps this package a pure function of registry JSON — and is
/// what makes the sell flow, the 200-point inspection and the loan application the same widget.
///
/// Density is deliberately **not** here. It travels in the theme as `AhDensityTheme`, so a control
/// asks `context.ahDensity` and the dealer binary is compact everywhere without a single call site
/// knowing which app it is in.
class SchemaFormScope extends InheritedWidget {
  const SchemaFormScope({
    required this.controller,
    required super.child,
    this.labels = const FormLabels(),
    this.loadOptions,
    this.uploader,
    super.key,
  });

  final SchemaFormController controller;

  /// Chrome strings. Field labels and validation sentences come from the schema, not from here.
  final FormLabels labels;

  /// Supplied by the app — a taxonomy/geography fetch through `autohub_core`'s registry client.
  final FormOptionsLoader? loadOptions;

  /// Supplied by the app — presigned upload, resumable multipart, whatever the pipeline is.
  final FormFileUploader? uploader;

  static SchemaFormScope of(BuildContext context) {
    final SchemaFormScope? scope = context.dependOnInheritedWidgetOfExactType<SchemaFormScope>();
    assert(scope != null, 'A form control was built outside a SchemaForm.');
    return scope!;
  }

  @override
  bool updateShouldNotify(SchemaFormScope oldWidget) =>
      controller != oldWidget.controller ||
      labels != oldWidget.labels ||
      loadOptions != oldWidget.loadOptions ||
      uploader != oldWidget.uploader;
}

/// Everything a control receives. No control knows which form it is in, which step it is on, or
/// what its value means — which is precisely why one set of eighteen controls covers every form in
/// the product.
@immutable
class FieldProps {
  const FieldProps({
    required this.field,
    required this.value,
    required this.onChanged,
    required this.onBlur,
    required this.invalid,
    required this.disabled,
    this.error,
  });

  final FormField field;
  final Object? value;

  /// `null` clears the value. A cleared optional field must be absent from the payload, not null.
  final ValueChanged<Object?> onChanged;

  /// Called when the user leaves the control. Until then its fault stays quiet.
  final VoidCallback onBlur;

  final bool invalid;
  final bool disabled;

  /// The current fault. Most controls ignore it — the field shell draws the message. It is here for
  /// the few that own their own error affordance: the plate, a photo slot.
  final FieldFault? error;

  /// The registry's own read-only flag, lifted for convenience — every control needs it and
  /// `props.field.readOnly` reads like a detail of the definition rather than a state of the
  /// control.
  bool get readOnly => field.readOnly;

  /// `true` when the control must render but refuse input, from either the registry row or a
  /// submission in flight.
  bool get locked => disabled || readOnly;
}

/* ── Option resolution ─────────────────────────────────────────────────────── */

/// What a control knows about its option list at one moment.
@immutable
class FieldOptionsState {
  const FieldOptionsState({
    this.options = const <Option>[],
    this.loading = false,
    this.failed = false,
  });

  final List<Option> options;
  final bool loading;

  /// Set when the loader threw. The control offers a retry rather than pretending the taxonomy is
  /// empty — "No fuel types" and "the network is down" are different sentences, and a seller who
  /// reads the first one closes the app.
  final bool failed;
}

/// Resolves a control's options: inline from the registry row when it has them, otherwise through
/// the injected loader.
///
/// A widget rather than a hook because Flutter has no hooks, but the semantics are the web's,
/// including the part that matters: a request whose scope has moved on is discarded. A dealer who
/// changes make three times must not see the second answer land after the third and repopulate the
/// model list with the wrong manufacturer's cars.
class FieldOptionsBuilder extends StatefulWidget {
  const FieldOptionsBuilder({
    required this.field,
    required this.builder,
    this.inline,
    this.attribute,
    this.level,
    this.source,
    this.params = const <String, Object?>{},
    this.enabled = true,
    super.key,
  });

  final FormField field;

  /// Inline options from the registry row. Present means no fetch happens at all.
  final List<Option>? inline;

  /// The taxonomy attribute code, when the row named one instead of inlining a list.
  final String? attribute;

  /// Cascading controls name the level being loaded — `model`, `variant`, `city`.
  final String? level;
  final String? source;
  final Map<String, Object?> params;

  /// `false` while a parent level is unchosen. A model list without a make is a meaningless query.
  final bool enabled;

  final Widget Function(BuildContext context, FieldOptionsState state, VoidCallback reload) builder;

  @override
  State<FieldOptionsBuilder> createState() => _FieldOptionsBuilderState();
}

class _FieldOptionsBuilderState extends State<FieldOptionsBuilder> {
  FieldOptionsState _state = const FieldOptionsState();

  /// Monotonic request id. The only thing that decides whether a completed fetch is still wanted.
  int _generation = 0;
  String _scope = '';

  @override
  void didChangeDependencies() {
    super.didChangeDependencies();
    _syncIfScopeChanged();
  }

  @override
  void didUpdateWidget(FieldOptionsBuilder oldWidget) {
    super.didUpdateWidget(oldWidget);
    _syncIfScopeChanged();
  }

  /// A stable identity for "the query this control currently wants".
  ///
  /// Cheap string composition rather than deep equality: the params are a handful of scalar
  /// selections, and this runs on every rebuild of every picker on the step.
  String _scopeKey() {
    final List<String> parts = <String>[
      widget.field.key,
      widget.attribute ?? '',
      widget.level ?? '',
      widget.source ?? '',
      widget.enabled ? '1' : '0',
      widget.inline == null ? 'remote' : 'inline:${widget.inline!.length}',
    ];
    final List<String> keys = widget.params.keys.toList()..sort();
    for (final String key in keys) {
      parts.add('$key=${widget.params[key]}');
    }
    return parts.join('|');
  }

  void _syncIfScopeChanged() {
    final String next = _scopeKey();
    if (next == _scope) return;
    _scope = next;
    _resolve();
  }

  void _resolve() {
    _generation += 1;

    final List<Option>? inline = widget.inline;
    if (inline != null) {
      _apply(FieldOptionsState(options: sortOptions(inline)));
      return;
    }

    final FormOptionsLoader? loader = SchemaFormScope.of(context).loadOptions;
    if (!widget.enabled || loader == null) {
      _apply(const FieldOptionsState());
      return;
    }

    final int generation = _generation;
    _apply(const FieldOptionsState(loading: true));
    unawaited(_fetch(loader, generation));
  }

  Future<void> _fetch(FormOptionsLoader loader, int generation) async {
    try {
      final List<Option> options = await loader(
        OptionsRequest(
          field: widget.field,
          attribute: widget.attribute,
          level: widget.level,
          source: widget.source,
          params: widget.params,
        ),
      );
      if (!mounted || generation != _generation) return;
      _apply(FieldOptionsState(options: sortOptions(options)));
    } on Object {
      // Deliberately broad. A loader is app code talking to a network from a basement; every way it
      // can fail ends in the same place for this widget — offer a retry, do not take the step down.
      if (!mounted || generation != _generation) return;
      _apply(const FieldOptionsState(failed: true));
    }
  }

  void _apply(FieldOptionsState next) {
    if (!mounted) {
      _state = next;
      return;
    }
    setState(() => _state = next);
  }

  @override
  Widget build(BuildContext context) => widget.builder(context, _state, _reload);

  void _reload() {
    _scope = '';
    _syncIfScopeChanged();
  }
}
