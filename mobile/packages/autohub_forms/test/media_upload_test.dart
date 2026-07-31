import 'dart:async';

import 'package:autohub_core/autohub_core.dart';
import 'package:autohub_forms/autohub_forms.dart';
import 'package:flutter_test/flutter_test.dart';

/// Forty photographs, one basement, and an operating system that kills backgrounded apps.
///
/// `docs/10-mobile-architecture.md` §4 says media is uploaded out of band so a 40-photo inspection
/// survives the app being backgrounded. The strategy has two halves and this file checks both:
/// every state change rewrites the form value (so the draft store carries it across a process
/// death), and anything left mid-flight is re-enqueued rather than left looking busy forever.
void main() {
  final FormField photos = FormField.fromJson(<String, Object?>{
    'key': 'photos',
    'label': 'Photos',
    'control': 'photo_set',
    'required': true,
    'config': <String, Object?>{
      'min': 6,
      'max': 20,
      'accept': <Object?>['image/*'],
      'maxSizeMb': 8,
      'prompts': <Object?>['Front 3/4', 'Odometer'],
    },
  });

  PickedMedia shot(String name, {String type = 'image/jpeg', int bytes = 2 * 1024 * 1024}) =>
      PickedMedia(path: '/tmp/$name', fileName: name, contentType: type, sizeBytes: bytes);

  /// Captures what the control writes back into the form value, which is exactly what the draft
  /// store would persist.
  ({MediaUploadController media, List<Object?> Function() value}) harness({
    FormFileUploader? uploader,
    List<MediaItem> restored = const <MediaItem>[],
  }) {
    List<Object?> latest = mediaItemsToValue(restored);
    final MediaUploadController media = MediaUploadController(
      field: photos,
      labels: const FormLabels(),
      uploader: uploader,
      onChanged: (List<MediaItem> items) => latest = mediaItemsToValue(items),
    )..adopt(restored);
    addTearDown(media.dispose);
    return (media: media, value: () => latest);
  }

  test('a photograph reaches the form value as plain JSON', () {
    final ({MediaUploadController media, List<Object?> Function() value}) h = harness();
    h.media.add(<PickedMedia>[shot('front.jpg')], prompt: 'Front 3/4');

    final Object? first = h.value().single;
    // JSON, never object instances: the value has to survive `jsonEncode` into the draft row and
    // come back out on relaunch pointing at a file that is still on disk.
    expect(first, isA<Map<String, Object?>>());
    final Map<String, Object?> item = first! as Map<String, Object?>;
    expect(item['prompt'], 'Front 3/4');
    expect(item['localPath'], '/tmp/front.jpg');
    // No uploader wired, so it stays local and pending — which is what an offline handset does.
    expect(item['status'], 'pending');
  });

  test('a rejected file enters the list visibly rejected rather than vanishing', () {
    final ({MediaUploadController media, List<Object?> Function() value}) h = harness();
    h.media.add(<PickedMedia>[
      shot('scan.pdf', type: 'application/pdf'),
      shot('huge.jpg', bytes: 20 * 1024 * 1024),
    ]);

    // Silently dropping a photograph a user just took is the worst available answer: they cannot
    // tell it was rejected, so they take it again, and again.
    expect(h.media.items.length, 2);
    expect(h.media.items[0].status, UploadStatus.failed);
    expect(h.media.items[0].errorCode, 'photos.wrong_type');
    expect(h.media.items[1].errorCode, 'photos.too_large');
  });

  test('progress and completion are written back through the form value', () async {
    final Completer<UploadResult> finish = Completer<UploadResult>();
    final ({MediaUploadController media, List<Object?> Function() value}) h = harness(
      uploader: (UploadRequest request) {
        request.onProgress(40);
        return finish.future;
      },
    );

    h.media.add(<PickedMedia>[shot('front.jpg')]);
    expect(h.media.items.single.status, UploadStatus.uploading);
    expect(h.media.items.single.progress, 40);

    finish.complete(const UploadResult(url: 'https://cdn/front.avif', assetId: 'asset-1'));
    await Future<void>.delayed(Duration.zero);

    expect(h.media.items.single.status, UploadStatus.uploaded);
    expect((h.value().single! as Map<String, Object?>)['url'], 'https://cdn/front.avif');
  });

  test('a failed upload offers a retry rather than taking the step down', () async {
    int attempts = 0;
    final ({MediaUploadController media, List<Object?> Function() value}) h = harness(
      uploader: (UploadRequest request) async {
        attempts += 1;
        if (attempts == 1) throw const SocketFailure();
        return const UploadResult(url: 'https://cdn/front.avif');
      },
    );

    h.media.add(<PickedMedia>[shot('front.jpg')]);
    await Future<void>.delayed(Duration.zero);
    expect(h.media.items.single.status, UploadStatus.failed);
    expect(h.media.items.single.errorCode, 'photos.upload_failed');

    h.media.retry(h.media.items.single.id);
    await Future<void>.delayed(Duration.zero);
    expect(h.media.items.single.status, UploadStatus.uploaded);
  });

  test('an upload interrupted by the process dying is picked back up on relaunch', () async {
    // This is the case the whole design exists for. Android evicts an app sitting behind the
    // camera; the draft comes back with an item still marked `uploading` and no future behind it,
    // and without this it sits at 60% forever until the seller gives up on a listing they had
    // already photographed.
    final List<MediaItem> restored = <MediaItem>[
      const MediaItem(
        id: 'm-1',
        fileName: 'front.jpg',
        sizeBytes: 2048,
        contentType: 'image/jpeg',
        localPath: '/tmp/front.jpg',
        status: UploadStatus.uploading,
        progress: 60,
      ),
      const MediaItem(
        id: 'm-2',
        fileName: 'rear.jpg',
        sizeBytes: 2048,
        contentType: 'image/jpeg',
        localPath: '/tmp/rear.jpg',
        status: UploadStatus.uploaded,
        progress: 100,
        url: 'https://cdn/rear.avif',
      ),
    ];

    final List<String> uploaded = <String>[];
    final ({MediaUploadController media, List<Object?> Function() value}) h = harness(
      restored: restored,
      uploader: (UploadRequest request) async {
        uploaded.add(request.fileName);
        return const UploadResult(url: 'https://cdn/resumed.avif');
      },
    );

    h.media.resumeInterrupted();
    await Future<void>.delayed(Duration.zero);

    // The interrupted one is retried; the finished one is left alone rather than re-uploaded, which
    // on a 40-photo inspection is the difference between a minute and twenty.
    expect(uploaded, <String>['front.jpg']);
    expect(h.media.items[0].status, UploadStatus.uploaded);
  });

  test('order is information, and reordering is available without a drag', () {
    final ({MediaUploadController media, List<Object?> Function() value}) h = harness();
    h.media.add(<PickedMedia>[shot('a.jpg'), shot('b.jpg'), shot('c.jpg')]);

    // The first item is the cover — it decides which photograph a buyer sees in a search result —
    // so this is a real operation and not a preference.
    h.media.move(h.media.items[2].id, -2);
    expect(h.media.items.map((MediaItem item) => item.fileName).toList(),
        <String>['c.jpg', 'a.jpg', 'b.jpg']);
  });

  test('assigning a prompt evicts whatever held that slot', () {
    final ({MediaUploadController media, List<Object?> Function() value}) h = harness();
    h.media
      ..add(<PickedMedia>[shot('a.jpg')], prompt: 'Odometer')
      ..add(<PickedMedia>[shot('b.jpg')]);

    h.media.assign(h.media.items[1].id, 'Odometer');
    // Two photographs claiming "Odometer" is a shot list that cannot be checked off.
    expect(h.media.items[0].prompt, isNull);
    expect(h.media.items[1].prompt, 'Odometer');
  });
}

/// Stands in for whatever the app's transport throws in a basement.
class SocketFailure implements Exception {
  const SocketFailure();
}
