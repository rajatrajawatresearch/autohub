import 'package:image_picker/image_picker.dart';

import 'media_upload.dart';

/// Where a photograph comes from.
///
/// Camera first, everywhere, and this is the reason the sell flow is worth building as an app at
/// all: a seller standing beside their car with a browser open has to leave the form, open the
/// camera, come back and hunt through a gallery. Six shots and they have abandoned the listing.
enum MediaSource { camera, gallery }

/// The picker seam.
///
/// A function type rather than a class so a widget test can hand in a stub without a plugin
/// registrant, and so the inspector app can later substitute a picker that also writes to its own
/// evidence store without any of the controls changing.
typedef FormMediaPicker = Future<List<PickedMedia>> Function(
  MediaSource source, {
  bool multiple,
});

final ImagePicker _picker = ImagePicker();

/// The default picker.
///
/// `imageQuality` and `maxWidth` are set here rather than left to the platform because the file
/// that comes off a modern handset camera is eight megabytes, and the uploader will be pushing
/// forty of them over a basement connection. Two thousand four hundred pixels is more than any
/// listing surface or inspection report renders, and it is the difference between an upload that
/// finishes and one the user closes the app on.
Future<List<PickedMedia>> defaultMediaPicker(
  MediaSource source, {
  bool multiple = true,
}) async {
  final List<XFile> files;
  if (source == MediaSource.camera) {
    final XFile? shot = await _picker.pickImage(
      source: ImageSource.camera,
      imageQuality: 85,
      maxWidth: 2400,
    );
    files = shot == null ? const <XFile>[] : <XFile>[shot];
  } else if (multiple) {
    files = await _picker.pickMultiImage(imageQuality: 85, maxWidth: 2400);
  } else {
    final XFile? one = await _picker.pickImage(
      source: ImageSource.gallery,
      imageQuality: 85,
      maxWidth: 2400,
    );
    files = one == null ? const <XFile>[] : <XFile>[one];
  }

  final List<PickedMedia> picked = <PickedMedia>[];
  for (final XFile file in files) {
    picked.add(
      PickedMedia(
        path: file.path,
        fileName: file.name,
        // A missing MIME type is common on Android for gallery picks. Falling back to the generic
        // image type keeps a legitimate photograph from being rejected by an `accept` list.
        contentType: file.mimeType ?? 'image/jpeg',
        sizeBytes: await file.length(),
      ),
    );
  }
  return picked;
}
