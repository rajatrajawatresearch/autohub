/// What the renderer needs that the contract does not describe.
///
/// The wire shapes themselves — `FormDefinition`, `FormStep`, `FormField`, `ValidationRule`,
/// `Option` — belong to `autohub_core`, which owns every model in
/// `docs/contracts/registry-api.md`. There is exactly one Dart definition of a form document in
/// this workspace, and it is not here. Two would be the defect this whole architecture exists to
/// prevent, arriving from the inside.
///
/// What *is* here is everything the act of rendering adds and the contract has no opinion about: a
/// fault as a user sees it, a photograph on its way to a bucket, the two ports an app fills in, and
/// the renderer's own chrome strings.
///
/// Nothing in this file encodes a business fact. There are no fuel types, no statuses, no fee
/// percentages, no option lists — those live in the registries and arrive as data.
library;

import 'dart:async';

import 'package:autohub_core/autohub_core.dart';
import 'package:flutter/foundation.dart';

/* ── Small parse helpers ───────────────────────────────────────────────────── */
//
// Used only by the shapes this package owns. Coercing rather than casting, because a draft written
// by a slightly older build is not a reason to crash on relaunch.

String? _str(Object? value) {
  if (value is String) return value;
  if (value is num || value is bool) return '$value';
  return null;
}

int? _int(Object? value) {
  if (value is num) return value.isFinite ? value.round() : null;
  if (value is String) return num.tryParse(value.trim())?.round();
  return null;
}

/* ── Faults ────────────────────────────────────────────────────────────────── */

/// One thing wrong with one field.
///
/// [code] is the registry's stable machine code and travels to the surface beside the message, so
/// support can read a fault off a screenshot instead of asking a seller to describe what they saw.
/// [message] is the registry's authored sentence, and is the only part the user reads — this
/// package never composes one from parts.
@immutable
class FieldFault {
  const FieldFault({required this.code, required this.message});

  final String code;
  final String message;

  @override
  bool operator ==(Object other) =>
      other is FieldFault && other.code == code && other.message == message;

  @override
  int get hashCode => Object.hash(code, message);

  @override
  String toString() => 'FieldFault($code)';
}

/* ── Options ───────────────────────────────────────────────────────────────── */

/// Orders a registry option list without disturbing a deliberate registry ordering.
///
/// `Option.sortOrder` defaults to `0`, so a taxonomy that did not author an order arrives as a list
/// of zeroes — and Dart's `sort` is not stable, which would shuffle `Petrol, Diesel, CNG, Electric`
/// into whatever the quicksort felt like. Tie-breaking on the original index makes it stable, which
/// is the difference between a fuel picker that reads the way operations wrote it and one that
/// reorders itself between builds.
List<Option> sortOptions(List<Option> options) {
  final List<(int, Option)> indexed = <(int, Option)>[
    for (int index = 0; index < options.length; index += 1) (index, options[index]),
  ];
  indexed.sort(((int, Option) a, (int, Option) b) {
    final int byOrder = a.$2.sortOrder.compareTo(b.$2.sortOrder);
    return byOrder != 0 ? byOrder : a.$1.compareTo(b.$1);
  });
  return <Option>[for (final (int, Option) entry in indexed) entry.$2];
}

/* ── Values ────────────────────────────────────────────────────────────────── */

/// A form's live values. Keys are field keys from the registry, unknown at build time.
///
/// `Object?` rather than a sealed union because the same map is written by eighteen controls, read
/// by a validator that treats everything as opaque, and serialised into a draft. A union here would
/// be a compile-time promise about data that arrives at runtime.
typedef FormValues = Map<String, Object?>;


/* ── Media ─────────────────────────────────────────────────────────────────── */

/// Where one file is in its journey from a camera to the media pipeline.
///
/// A closed contract vocabulary, so an enum is correct here: the states are this package's own and
/// adding one should break every `switch` that has to decide about it.
enum UploadStatus { pending, uploading, uploaded, failed }

/// What a `file` / `photo_set` field stores.
///
/// Every member is JSON-serialisable, and that is the point. On the web the preview was an object
/// URL that died with the tab; here [localPath] is a real file on the handset, so a draft written
/// before Android killed the process for memory still knows which six photographs were taken and
/// which of them had finished uploading. Restoring the draft resumes the rest.
@immutable
class MediaItem {
  const MediaItem({
    required this.id,
    required this.fileName,
    required this.sizeBytes,
    required this.contentType,
    required this.status,
    this.prompt,
    this.localPath,
    this.url,
    this.assetId,
    this.progress = 0,
    this.errorCode,
    this.errorMessage,
  });

  factory MediaItem.fromJson(Map<String, Object?> json) => MediaItem(
        id: _str(json['id']) ?? '',
        fileName: _str(json['fileName']) ?? '',
        sizeBytes: _int(json['sizeBytes']) ?? 0,
        contentType: _str(json['contentType']) ?? '',
        status: UploadStatus.values.firstWhere(
          (UploadStatus candidate) => candidate.name == _str(json['status']),
          orElse: () => UploadStatus.pending,
        ),
        prompt: _str(json['prompt']),
        localPath: _str(json['localPath']),
        url: _str(json['url']),
        assetId: _str(json['assetId']),
        progress: _int(json['progress']) ?? 0,
        errorCode: _str(json['errorCode']),
        errorMessage: _str(json['errorMessage']),
      );

  /// Client-side id, stable across reorders. Never sent as an entity id.
  final String id;
  final String fileName;
  final int sizeBytes;
  final String contentType;

  /// The prompt this shot satisfies, when it fills a prompted slot.
  final String? prompt;

  /// Path on the device. Survives a process kill, which an in-memory preview does not.
  final String? localPath;

  /// Where the file now lives, once the uploader has finished.
  final String? url;
  final String? assetId;

  final UploadStatus status;

  /// 0–100.
  final int progress;

  final String? errorCode;
  final String? errorMessage;

  MediaItem copyWith({
    String? prompt,
    bool clearPrompt = false,
    String? url,
    String? assetId,
    UploadStatus? status,
    int? progress,
    String? errorCode,
    String? errorMessage,
    bool clearError = false,
  }) =>
      MediaItem(
        id: id,
        fileName: fileName,
        sizeBytes: sizeBytes,
        contentType: contentType,
        status: status ?? this.status,
        prompt: clearPrompt ? null : (prompt ?? this.prompt),
        localPath: localPath,
        url: url ?? this.url,
        assetId: assetId ?? this.assetId,
        progress: progress ?? this.progress,
        errorCode: clearError ? null : (errorCode ?? this.errorCode),
        errorMessage: clearError ? null : (errorMessage ?? this.errorMessage),
      );

  Map<String, Object?> toJson() => <String, Object?>{
        'id': id,
        'fileName': fileName,
        'sizeBytes': sizeBytes,
        'contentType': contentType,
        'status': status.name,
        'progress': progress,
        if (prompt != null) 'prompt': prompt,
        if (localPath != null) 'localPath': localPath,
        if (url != null) 'url': url,
        if (assetId != null) 'assetId': assetId,
        if (errorCode != null) 'errorCode': errorCode,
        if (errorMessage != null) 'errorMessage': errorMessage,
      };
}

/// Cooperative cancellation for an in-flight upload.
///
/// Dart has no `AbortSignal`, and dio's `CancelToken` cannot appear here — a package that renders
/// forms must not know which HTTP client the app chose. This is the smallest thing that lets an
/// uploader stop when a photo is removed mid-flight.
class UploadCancellation {
  final Completer<void> _completer = Completer<void>();

  bool get isCancelled => _completer.isCompleted;

  /// Completes when [cancel] is called. An uploader can race this against its own request.
  Future<void> get whenCancelled => _completer.future;

  void cancel() {
    if (!_completer.isCompleted) _completer.complete();
  }
}

@immutable
class UploadRequest {
  const UploadRequest({
    required this.field,
    required this.localPath,
    required this.fileName,
    required this.contentType,
    required this.onProgress,
    required this.cancellation,
    this.prompt,
  });

  final FormField field;
  final String localPath;
  final String fileName;
  final String contentType;
  final String? prompt;

  /// 0–100.
  final void Function(int percent) onProgress;
  final UploadCancellation cancellation;
}

@immutable
class UploadResult {
  const UploadResult({this.url, this.assetId});

  final String? url;
  final String? assetId;
}

/// The transport is injected, never implemented here.
///
/// A package that renders forms has no business knowing about presigned URLs, resumable multipart,
/// or which bucket an inspection photo lands in. With no uploader wired the files stay local and
/// `pending`, which is exactly what a widget test wants.
typedef FormFileUploader = Future<UploadResult> Function(UploadRequest request);

/* ── Options loading ───────────────────────────────────────────────────────── */

@immutable
class OptionsRequest {
  const OptionsRequest({
    required this.field,
    required this.params,
    this.attribute,
    this.level,
    this.source,
  });

  final FormField field;

  /// The taxonomy attribute code from `optionsFromAttribute`, when the field named one.
  final String? attribute;

  /// Set for cascading controls — the `vehicle_picker` level or `address` part being filled.
  final String? level;
  final String? source;

  /// The values the loader may scope its query by — the picker's upstream selections, the form's
  /// category. A loader turns these into `?category=suv` or `?make=maruti-suzuki`.
  final Map<String, Object?> params;
}

/// Options come from the taxonomy and geography registries. This package asks; the app fetches.
///
/// That single function type is the whole no-hardcoding rule, expressed in the type system: there
/// is nowhere in `autohub_forms` for a list of fuel types to live.
typedef FormOptionsLoader = Future<List<Option>> Function(OptionsRequest request);

/* ── Chrome strings ────────────────────────────────────────────────────────── */

/// Keys for the renderer's own chrome.
///
/// The registry supplies field labels and every validation sentence; somebody still has to say
/// "Back". These keys are shaped like the localisation registry's
/// (`GET /platform/i18n/{locale}?namespace=web`) so an app hands its resource bundle straight in.
///
/// Deliberately `String` constants rather than an enum: [FormLabels] takes any key, so a bundle
/// with a key this build has never seen still resolves, and a key this build asks for that the
/// bundle lacks falls back rather than crashing. That is the contract's rule — locale, then
/// `en-IN`, then the key itself, and never an empty string.
abstract final class FormLabelKeys {
  static const String actionBack = 'form.action.back';
  static const String actionNext = 'form.action.next';
  static const String actionSubmit = 'form.action.submit';
  static const String actionWorking = 'form.action.working';
  static const String stepProgress = 'form.step.progress';
  static const String fieldOptional = 'form.field.optional';
  static const String validationRequired = 'form.validation.required';
  static const String validationInvalid = 'form.validation.invalid';
  static const String validationMin = 'form.validation.min';
  static const String validationMax = 'form.validation.max';
  static const String validationMinItems = 'form.validation.min_items';
  static const String validationMaxItems = 'form.validation.max_items';
  static const String unsupportedTitle = 'form.control.unsupported.title';
  static const String unsupportedBody = 'form.control.unsupported.body';
  static const String selectPlaceholder = 'form.select.placeholder';
  static const String selectLoading = 'form.select.loading';
  static const String selectEmpty = 'form.select.empty';
  static const String selectError = 'form.select.error';
  static const String selectLocked = 'form.select.locked';
  static const String multiselectCount = 'form.multiselect.count';
  static const String moneySymbol = 'form.money.symbol';
  static const String phoneDialCode = 'form.phone.dial_code';
  static const String otpHint = 'form.otp.hint';
  static const String fileChoose = 'form.file.choose';
  static const String fileRemove = 'form.file.remove';
  static const String fileRetry = 'form.file.retry';
  static const String fileUploading = 'form.file.uploading';
  static const String fileFailed = 'form.file.failed';
  static const String fileTooLarge = 'form.file.too_large';
  static const String fileWrongType = 'form.file.wrong_type';
  static const String photoRequiredShots = 'form.photo.required_shots';
  static const String photoMoreShots = 'form.photo.more_shots';
  static const String photoAdd = 'form.photo.add';
  static const String photoCamera = 'form.photo.camera';
  static const String photoGallery = 'form.photo.gallery';
  static const String photoCount = 'form.photo.count';
  static const String photoCover = 'form.photo.cover';
  static const String photoEmptySlot = 'form.photo.empty_slot';
  static const String photoMoveEarlier = 'form.photo.move_earlier';
  static const String photoMoveLater = 'form.photo.move_later';
  static const String photoAssign = 'form.photo.assign';
  static const String photoUnassigned = 'form.photo.unassigned';
  static const String addressLine1 = 'form.address.line1';
  static const String addressLine2 = 'form.address.line2';
  static const String addressCity = 'form.address.city';
  static const String addressState = 'form.address.state';
  static const String addressPincode = 'form.address.pincode';
  static const String addressLandmark = 'form.address.landmark';
  static const String richTextCount = 'form.richtext.count';
  static const String draftRestored = 'form.draft.restored';
}

/// The last-resort fallback bundle, in the product's voice: a knowledgeable dealer, not a
/// salesperson. Errors state the problem and the fix. Nothing apologises.
///
/// These are the one place in the workspace where a user-facing literal is correct, and only
/// because the contract demands a terminal fallback: a cold launch on a dead network has no i18n
/// bundle, and a sell form whose buttons are blank is worse than one whose buttons are in English.
/// Every one of them is overridden the moment `GET /platform/i18n/{locale}` answers.
const Map<String, String> defaultFormLabels = <String, String>{
  FormLabelKeys.actionBack: 'Back',
  FormLabelKeys.actionNext: 'Continue',
  FormLabelKeys.actionSubmit: 'Submit',
  FormLabelKeys.actionWorking: 'Working',
  FormLabelKeys.stepProgress: 'Step {current} of {total}',
  FormLabelKeys.fieldOptional: 'Optional',
  FormLabelKeys.validationRequired: 'Fill this in to continue.',
  FormLabelKeys.validationInvalid: 'Check this value.',
  FormLabelKeys.validationMin: 'Enter {value} or more.',
  FormLabelKeys.validationMax: 'Enter {value} or less.',
  FormLabelKeys.validationMinItems: 'Add at least {value}.',
  FormLabelKeys.validationMaxItems: 'Keep it to {value} or fewer.',
  FormLabelKeys.unsupportedTitle: 'Field unavailable',
  FormLabelKeys.unsupportedBody:
      'This version of the app cannot draw this field. Update the app, or finish this field from the website.',
  FormLabelKeys.selectPlaceholder: 'Select',
  FormLabelKeys.selectLoading: 'Loading options',
  FormLabelKeys.selectEmpty: 'No options available',
  FormLabelKeys.selectError: 'Options did not load. Retry.',
  FormLabelKeys.selectLocked: 'Choose {field} first',
  FormLabelKeys.multiselectCount: '{count} selected',
  FormLabelKeys.moneySymbol: '₹',
  FormLabelKeys.phoneDialCode: '+91',
  FormLabelKeys.otpHint: 'Enter the {length}-digit code.',
  FormLabelKeys.fileChoose: 'Choose file',
  FormLabelKeys.fileRemove: 'Remove',
  FormLabelKeys.fileRetry: 'Retry upload',
  FormLabelKeys.fileUploading: 'Uploading',
  FormLabelKeys.fileFailed: 'Upload failed. Retry, or pick a different file.',
  FormLabelKeys.fileTooLarge: 'File is over {value} MB. Pick a smaller one.',
  FormLabelKeys.fileWrongType: 'That file type is not accepted here.',
  FormLabelKeys.photoRequiredShots: 'Prompted shots',
  FormLabelKeys.photoMoreShots: 'More photos',
  FormLabelKeys.photoAdd: 'Add photos',
  FormLabelKeys.photoCamera: 'Camera',
  FormLabelKeys.photoGallery: 'Gallery',
  FormLabelKeys.photoCount: '{count} of {min} minimum',
  FormLabelKeys.photoCover: 'Cover',
  FormLabelKeys.photoEmptySlot: 'Not shot yet',
  FormLabelKeys.photoMoveEarlier: 'Move earlier',
  FormLabelKeys.photoMoveLater: 'Move later',
  FormLabelKeys.photoAssign: 'Use for',
  FormLabelKeys.photoUnassigned: 'Unassigned',
  FormLabelKeys.addressLine1: 'Address',
  FormLabelKeys.addressLine2: 'Area',
  FormLabelKeys.addressCity: 'City',
  FormLabelKeys.addressState: 'State',
  FormLabelKeys.addressPincode: 'Pincode',
  FormLabelKeys.addressLandmark: 'Landmark',
  FormLabelKeys.richTextCount: '{count} characters',
  FormLabelKeys.draftRestored: 'Picked up where you left off.',
};

/// Resolves a chrome string against an override bundle, then the defaults, then the key itself.
///
/// Callable, so call sites read `t(FormLabelKeys.actionBack)` — the same shape as the web's `t()`
/// and the same shape as the i18n registry's own lookup.
@immutable
class FormLabels {
  const FormLabels([this.overrides = const <String, String>{}]);

  /// Typically the `resources` map straight out of `GET /platform/i18n/{locale}`.
  final Map<String, String> overrides;

  String call(String key, [Map<String, Object?>? vars]) {
    final String? override = overrides[key];
    final String template = (override != null && override.isNotEmpty)
        ? override
        : (defaultFormLabels[key] ?? key);
    return interpolate(template, vars);
  }
}

final RegExp _placeholder = RegExp(r'\{(\w+)\}');

/// Substitutes `{name}` placeholders. An unmatched placeholder is left as written rather than
/// blanked, because a visible `{count}` is a bug report and a silent gap is not.
String interpolate(String template, [Map<String, Object?>? vars]) {
  if (vars == null || vars.isEmpty) return template;
  return template.replaceAllMapped(_placeholder, (Match match) {
    final Object? replacement = vars[match.group(1)];
    return replacement == null ? match.group(0)! : '$replacement';
  });
}
