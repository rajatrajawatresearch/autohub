import 'dart:async';

import 'package:autohub_core/autohub_core.dart';
import 'package:flutter/foundation.dart';

/// What happened to a workflow action the dealer fired.
sealed class ActionOutcome {
  const ActionOutcome();
}

/// The server applied it and answered with the entity's new state.
///
/// The screen replaces its snapshot from [snapshot] rather than assuming the transition it asked
/// for is the transition that happened. `GET` and the action `POST` return the same shape for
/// exactly this reason — there is never a need to guess.
@immutable
final class ActionApplied extends ActionOutcome {
  const ActionApplied(this.snapshot);

  final WorkflowSnapshot snapshot;
}

/// The network was not there. The action is **queued** with the idempotency key it was born with
/// and will land when signal returns.
///
/// This is the opposite decision from a bid, and the difference is the deadline. Approving a
/// listing an hour late is the same approval; a bid ninety seconds late is a bid on a lot that has
/// closed. So a workflow action queues and a bid never does.
@immutable
final class ActionQueued extends ActionOutcome {
  const ActionQueued();
}

/// The server acted, but did not answer with a snapshot this build could read.
///
/// A separate case from [ActionApplied] and emphatically not a failure: the transition happened and
/// the money, if there was any, moved. Rendering an error here would be the worst of both — the
/// dealer fires the action again against a state that has already changed. The screen refetches.
@immutable
final class ActionAccepted extends ActionOutcome {
  const ActionAccepted();
}

/// `409 platform.workflow.transition_not_allowed` — the entity moved underneath the dealer.
///
/// Carries the server's current state so the screen shows what changed and asks, rather than
/// re-applying an action against a world that no longer exists.
@immutable
final class ActionBlocked extends ActionOutcome {
  const ActionBlocked({required this.code, this.detail, this.currentState});

  final String code;
  final String? detail;
  final Map<String, dynamic>? currentState;
}

@immutable
final class ActionFailed extends ActionOutcome {
  const ActionFailed({required this.code, this.detail});

  final String code;
  final String? detail;
}

/// Fires the actions a workflow snapshot offered, through the durable queue.
///
/// The list of actions is never derived here. `availableActions` arrives from the server with its
/// guards and permissions already evaluated, and this class posts the `actionCode` it was handed —
/// a client that decides what a user may do is a client an attacker can convince, and on a handset
/// they can take their time about it.
final class WorkflowActionRunner {
  const WorkflowActionRunner({
    required Outbox outbox,
    required OutboxDrainer drainer,
    Duration wait = const Duration(seconds: 12),
  })  : _outbox = outbox,
        _drainer = drainer,
        _wait = wait;

  final Outbox _outbox;
  final OutboxDrainer _drainer;

  /// How long the screen waits for the answer before reporting the action as queued.
  ///
  /// It is a *reporting* deadline, not a cancellation: the row stays in the queue and drains
  /// whenever it can. Telling a dealer "queued" after twelve seconds is honest; leaving them on a
  /// disabled button while a slow network decides is how a screen looks broken.
  final Duration _wait;

  /// Fires [actionCode] against `/{module}/{resource}/{id}/workflow/actions`.
  ///
  /// The write is captured — and its `Idempotency-Key` minted — the moment this is called, which
  /// is the tap. A retry after a tunnel therefore carries the same key, and the server replays its
  /// original response instead of approving the same listing twice.
  Future<ActionOutcome> fire({
    required String module,
    required String resource,
    required String id,
    required String actionCode,
    String? reason,
    Map<String, Object?>? payload,
    String? activeCity,
  }) async {
    final intent = WriteIntent.now(
      endpoint: '/$module/$resource/${Uri.encodeComponent(id)}/workflow/actions',
      method: WriteMethod.post,
      body: <String, dynamic>{
        'actionCode': actionCode,
        if (reason != null && reason.isNotEmpty) 'reason': reason,
        if (payload != null) 'payload': payload,
      },
      headers: <String, String>{
        if (activeCity != null && activeCity.isNotEmpty) 'X-Active-City': activeCity,
      },
    );

    // Subscribed *before* the enqueue. A fast network can complete the drain before an awaited
    // subscription would have been installed, and the screen would then wait out the full deadline
    // for an answer that had already gone past.
    final answered = Completer<Map<String, dynamic>?>();
    final completions = _drainer.completions.listen((completion) {
      if (completion.intent.idempotencyKey != intent.idempotencyKey) return;
      if (!answered.isCompleted) answered.complete(completion.body);
    });

    try {
      await _outbox.enqueue(intent);
      await _drainer.drain();

      final body = await answered.future.timeout(_wait, onTimeout: () => null);

      if (body != null) {
        final snapshot = _snapshotOrNull(body);
        if (snapshot != null) return ActionApplied(snapshot);
      }

      // No usable body. The queue row is the authority on what actually happened: gone means the
      // server accepted it (and the caller refetches), present means it is waiting or stuck.
      final entry = await _outbox.find(intent.idempotencyKey);
      return _fromEntry(entry);
    } finally {
      await completions.cancel();
    }
  }

  static ActionOutcome _fromEntry(OutboxEntry? entry) {
    // Gone from the queue means the server accepted it. The answer simply did not decode into a
    // snapshot — reporting that as a failure would have the dealer fire the action a second time
    // against a state that has already moved.
    if (entry == null) return const ActionAccepted();

    return switch (entry.state) {
      OutboxState.pending => const ActionQueued(),
      OutboxState.conflicted => ActionBlocked(
          code: entry.lastErrorCode ?? 'platform.workflow.transition_not_allowed',
          detail: entry.lastError,
          currentState: entry.conflict,
        ),
      OutboxState.poisoned => ActionFailed(
          code: entry.lastErrorCode ?? 'common.http.500',
          detail: entry.lastError,
        ),
    };
  }

  static WorkflowSnapshot? _snapshotOrNull(Map<String, dynamic> body) {
    try {
      return WorkflowSnapshot.fromJson(body);
    } on Object {
      return null;
    }
  }
}
