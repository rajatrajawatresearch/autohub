import 'dart:async';

import 'package:autohub_core/autohub_core.dart';
import 'package:flutter/foundation.dart';

import '../field_config.dart';
import '../types.dart';
import 'shared.dart';

/// One file the user just chose, in the shape this package cares about.
///
/// Deliberately not `XFile`. The picker is an implementation detail — camera today, a document
/// provider tomorrow, a test harness in between — and the upload lifecycle should not have to know
/// which one produced the bytes.
@immutable
class PickedMedia {
  const PickedMedia({
    required this.path,
    required this.fileName,
    required this.contentType,
    required this.sizeBytes,
  });

  final String path;
  final String fileName;
  final String contentType;
  final int sizeBytes;
}

/// Upload lifecycle for `file` and `photo_set`.
///
/// **The value is the list.** Progress, failure, retry and reorder all rewrite the same array the
/// form already holds, which means every one of them is autosaved into the draft store by the
/// controller that owns the form. That is the whole survival strategy, and it is worth stating
/// plainly:
///
///   * A half-uploaded set survives a step change and a Back — an inspector on 3G beside a car does
///     not lose six photographs because they tapped the wrong thing.
///   * It survives the process being killed while backgrounded, which Android does routinely to an
///     app sitting behind a camera. `localPath` points at a real file that is still there, so
///     [resumeInterrupted] can pick the transfer back up on relaunch rather than asking a seller to
///     photograph their car a second time.
///
/// The transport itself is injected. With no uploader wired the files stay local and `pending`,
/// which is exactly what a widget test wants and exactly what an offline handset should do.
class MediaUploadController {
  MediaUploadController({
    required this.field,
    required this.labels,
    required this.onChanged,
    this.uploader,
  });

  final FormField field;
  final FormLabels labels;
  final FormFileUploader? uploader;

  /// Emits the whole list on every change. The caller writes it straight into the form value.
  final void Function(List<MediaItem> items) onChanged;

  List<MediaItem> _items = const <MediaItem>[];
  final Map<String, UploadCancellation> _inFlight = <String, UploadCancellation>{};
  bool _disposed = false;

  List<MediaItem> get items => _items;

  bool get busy => _items.any((MediaItem item) => item.status == UploadStatus.uploading);

  /// Takes the list the form currently holds.
  ///
  /// Compared by id sequence rather than deeply: a progress tick that this controller just emitted
  /// comes straight back through the form on the next build, and adopting it would clobber the tick
  /// after it. A genuinely external change — a draft restore, a reset — changes which ids exist.
  void adopt(List<MediaItem> external) {
    if (_sameIds(external, _items)) return;
    _items = List<MediaItem>.of(external);
  }

  static bool _sameIds(List<MediaItem> a, List<MediaItem> b) {
    if (a.length != b.length) return false;
    for (int index = 0; index < a.length; index += 1) {
      if (a[index].id != b[index].id) return false;
    }
    return true;
  }

  void _emit(List<MediaItem> next) {
    _items = next;
    if (_disposed) return;
    onChanged(next);
  }

  void _patch(String id, MediaItem Function(MediaItem item) change) {
    final List<MediaItem> next = <MediaItem>[];
    bool found = false;
    for (final MediaItem item in _items) {
      if (item.id == id) {
        found = true;
        next.add(change(item));
      } else {
        next.add(item);
      }
    }
    if (!found) return;
    _emit(next);
  }

  /// Adds files, rejecting the ones the registry's `accept` and `maxSizeMb` disallow.
  ///
  /// A rejected file still enters the list, visibly rejected. Silently dropping a photograph a user
  /// just took is the worst available answer — they cannot tell it was rejected, so they take it
  /// again, and again.
  void add(List<PickedMedia> picked, {String? prompt}) {
    if (picked.isEmpty) return;
    final num? maxSizeMb = field.maxSizeMb;
    final List<MediaItem> created = <MediaItem>[];

    for (final PickedMedia file in picked) {
      UploadStatus status = UploadStatus.pending;
      String? errorCode;
      String? errorMessage;

      if (!isAcceptedFile(file.fileName, file.contentType, field.accept)) {
        status = UploadStatus.failed;
        errorCode = '${field.key}.wrong_type';
        errorMessage = labels(FormLabelKeys.fileWrongType);
      } else if (maxSizeMb != null && file.sizeBytes > maxSizeMb * 1024 * 1024) {
        status = UploadStatus.failed;
        errorCode = '${field.key}.too_large';
        errorMessage = labels(FormLabelKeys.fileTooLarge, <String, Object?>{'value': maxSizeMb});
      }

      created.add(
        MediaItem(
          id: localMediaId(),
          fileName: file.fileName,
          sizeBytes: file.sizeBytes,
          contentType: file.contentType,
          localPath: file.path,
          prompt: prompt,
          status: status,
          errorCode: errorCode,
          errorMessage: errorMessage,
        ),
      );
    }

    _emit(<MediaItem>[..._items, ...created]);
    for (final MediaItem item in created) {
      if (item.status == UploadStatus.pending) _upload(item.id);
    }
  }

  void remove(String id) {
    _inFlight.remove(id)?.cancel();
    _emit(<MediaItem>[
      for (final MediaItem item in _items)
        if (item.id != id) item,
    ]);
  }

  void retry(String id) => _upload(id);

  /// Absolute reorder — what a drag knows, since a drop target knows its own index and not yours.
  ///
  /// The first item is the cover, so order is information rather than preference: it decides which
  /// photograph a buyer sees in a search result.
  void moveTo(String id, int targetIndex) {
    final int from = _items.indexWhere((MediaItem item) => item.id == id);
    if (from < 0) return;
    final int to = targetIndex.clamp(0, _items.length - 1);
    if (to == from) return;
    final List<MediaItem> next = List<MediaItem>.of(_items);
    next.insert(to, next.removeAt(from));
    _emit(next);
  }

  void move(String id, int delta) {
    final int from = _items.indexWhere((MediaItem item) => item.id == id);
    if (from < 0) return;
    moveTo(id, from + delta);
  }

  /// Points a shot at a prompted slot, evicting whatever held that slot back into the loose pile.
  void assign(String id, String? prompt) {
    _emit(<MediaItem>[
      for (final MediaItem item in _items)
        if (item.id == id)
          item.copyWith(prompt: prompt, clearPrompt: prompt == null)
        else if (prompt != null && item.prompt == prompt)
          item.copyWith(clearPrompt: true)
        else
          item,
    ]);
  }

  /// Re-enqueues anything that was mid-flight when the app last stopped running.
  ///
  /// Called on mount and again whenever the app returns to the foreground. An item left `uploading`
  /// has no live future behind it — the isolate that owned it is gone — so it would otherwise sit
  /// at 60% forever, looking busy, until the seller gave up. An item left `pending` never started.
  void resumeInterrupted() {
    for (final MediaItem item in _items) {
      if (item.localPath == null) continue;
      if (item.status == UploadStatus.uploaded || item.status == UploadStatus.failed) continue;
      if (_inFlight.containsKey(item.id)) continue;
      _upload(item.id);
    }
  }

  void _upload(String id) {
    final FormFileUploader? transport = uploader;
    if (transport == null) return;

    MediaItem? target;
    for (final MediaItem item in _items) {
      if (item.id == id) target = item;
    }
    final String? localPath = target?.localPath;
    if (target == null || localPath == null) return;

    _inFlight.remove(id)?.cancel();
    final UploadCancellation cancellation = UploadCancellation();
    _inFlight[id] = cancellation;

    _patch(
      id,
      (MediaItem item) =>
          item.copyWith(status: UploadStatus.uploading, progress: 0, clearError: true),
    );

    unawaited(
      _run(
        transport,
        UploadRequest(
          field: field,
          localPath: localPath,
          fileName: target.fileName,
          contentType: target.contentType,
          prompt: target.prompt,
          cancellation: cancellation,
          onProgress: (int percent) {
            if (cancellation.isCancelled || _disposed) return;
            _patch(id, (MediaItem item) => item.copyWith(progress: percent.clamp(0, 100)));
          },
        ),
        id,
        cancellation,
      ),
    );
  }

  Future<void> _run(
    FormFileUploader transport,
    UploadRequest request,
    String id,
    UploadCancellation cancellation,
  ) async {
    try {
      final UploadResult result = await transport(request);
      if (cancellation.isCancelled || _disposed) return;
      _patch(
        id,
        (MediaItem item) => item.copyWith(
          status: UploadStatus.uploaded,
          progress: 100,
          url: result.url,
          assetId: result.assetId,
          clearError: true,
        ),
      );
    } on Object {
      // Broad on purpose. An uploader is app code talking to a flaky network from a basement; every
      // way it can fail lands the user in the same place, and that place must offer a retry rather
      // than an unhandled error that takes the step down.
      if (cancellation.isCancelled || _disposed) return;
      _patch(
        id,
        (MediaItem item) => item.copyWith(
          status: UploadStatus.failed,
          errorCode: '${field.key}.upload_failed',
          errorMessage: labels(FormLabelKeys.fileFailed),
        ),
      );
    } finally {
      _inFlight.remove(id);
    }
  }

  /// Cancels in-flight transfers. The *items* survive in the form value and in the draft, so a
  /// disposed control loses a connection, never a photograph.
  void dispose() {
    _disposed = true;
    for (final UploadCancellation cancellation in _inFlight.values) {
      cancellation.cancel();
    }
    _inFlight.clear();
  }
}
