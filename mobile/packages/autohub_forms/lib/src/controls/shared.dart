/// Value coercion for controls.
///
/// Form values arrive from four directions — a registry `defaultValue`, a draft off the device, a
/// server-side prefill, and the user's own typing — and only the last has a type this code chose.
/// Every control therefore treats its value as `Object?` and narrows it here, once, in one place.
///
/// A control that cast instead of coerced would crash on the first draft written by a build with a
/// slightly different opinion about whether an odometer reading is a number or a string. That crash
/// would happen on a phone, in a tunnel, at the end of a twenty-minute form.
library;

import 'dart:math' as math;

import '../types.dart';


String asString(Object? value) {
  if (value == null) return '';
  if (value is String) return value;
  if (value is num || value is bool) return '$value';
  return '';
}

num? asNumber(Object? value) {
  if (value is num) return value.isFinite ? value : null;
  if (value is String && value.trim().isNotEmpty) {
    final num? parsed = num.tryParse(value.trim());
    return (parsed != null && parsed.isFinite) ? parsed : null;
  }
  return null;
}

/// Integer minor units. Money is never a `double` anywhere in this product.
int? asMinorUnits(Object? value) {
  final num? parsed = asNumber(value);
  return parsed?.round();
}

bool asBoolean(Object? value) {
  if (value is bool) return value;
  if (value is String) return value == 'true';
  return false;
}

List<String> asStringList(Object? value) {
  if (value is! List) return const <String>[];
  return <String>[
    for (final Object? entry in value)
      if (entry is String) entry,
  ];
}

Map<String, Object?> asRecord(Object? value) {
  if (value is Map) {
    return <String, Object?>{
      for (final MapEntry<Object?, Object?> entry in value.entries)
        if (entry.key is String) entry.key! as String: entry.value,
    };
  }
  return <String, Object?>{};
}

/// Reads a media list out of a form value.
///
/// Accepts both the live shape and the persisted shape. The media controls always *write* plain
/// JSON maps so that a value can survive the draft store and the wire unchanged, but a caller that
/// seeded the form with typed [MediaItem]s should not be silently ignored.
List<MediaItem> asMediaItems(Object? value) {
  if (value is! List) return const <MediaItem>[];
  final List<MediaItem> items = <MediaItem>[];
  for (final Object? entry in value) {
    if (entry is MediaItem) {
      items.add(entry);
    } else if (entry is Map) {
      final Map<String, Object?> json = asRecord(entry);
      if (json['id'] is String) items.add(MediaItem.fromJson(json));
    }
  }
  return items;
}

/// The wire and draft form of a media list. Always JSON, never object instances.
List<Map<String, Object?>> mediaItemsToValue(List<MediaItem> items) =>
    <Map<String, Object?>>[for (final MediaItem item in items) item.toJson()];

final RegExp _nonDigits = RegExp(r'\D');

/// Digits only. Used wherever a user is transcribing a number off a document or a plate.
String digitsOnly(String value) => value.replaceAll(_nonDigits, '');

/// Rupees typed by a human → paise as an `int`.
///
/// String arithmetic, not `(value * 100).round()`. `8.4 * 100` is `840.0000000000001` in binary
/// floating point; a listing price that is one paise out is a settlement that does not reconcile,
/// and the reconciliation happens weeks later in a different system.
int? rupeesToMinor(String input) {
  final String cleaned = input.replaceAll(RegExp('[^0-9.-]'), '');
  if (cleaned.isEmpty || cleaned == '-' || cleaned == '.') return null;

  final bool negative = cleaned.startsWith('-');
  final List<String> parts = cleaned.replaceAll('-', '').split('.');
  final String whole = parts.isEmpty || parts.first.isEmpty ? '0' : parts.first;
  final String fraction = parts.length > 1 ? parts[1] : '';
  final String paise = '${fraction}00'.substring(0, 2);

  final int? parsed = int.tryParse('$whole$paise');
  if (parsed == null) return null;
  return negative ? -parsed : parsed;
}

/// Paise → the rupee string a human edits. Trailing `.00` is dropped; a vehicle price has no paise.
String minorToRupees(int? minorUnits) {
  if (minorUnits == null) return '';
  final bool negative = minorUnits < 0;
  final int absolute = minorUnits.abs();
  final int whole = absolute ~/ 100;
  final int remainder = absolute % 100;
  final String body =
      remainder == 0 ? '$whole' : '$whole.${remainder.toString().padLeft(2, '0')}';
  return negative ? '-$body' : body;
}

final math.Random _random = math.Random();

/// A client-side id for a media item.
///
/// Stable across reorders and across a process restart, because it is written into the draft with
/// the item. Never sent as an entity id — the server mints those.
String localMediaId() {
  final String stamp = DateTime.now().microsecondsSinceEpoch.toRadixString(36);
  final String noise = _random.nextInt(1 << 32).toRadixString(36);
  return 'm-$stamp-$noise';
}

/// `accept` is a list of MIME types and/or extensions, exactly as the contract writes them
/// (`["application/pdf", "image/jpeg"]`, or `["image/*"]`, or `[".pdf"]`).
bool isAcceptedFile(String fileName, String contentType, List<String> accept) {
  if (accept.isEmpty) return true;
  final String name = fileName.toLowerCase();
  final String type = contentType.toLowerCase();
  return accept.any((String entry) {
    final String pattern = entry.trim().toLowerCase();
    if (pattern.isEmpty) return true;
    if (pattern.startsWith('.')) return name.endsWith(pattern);
    if (pattern.endsWith('/*')) return type.startsWith(pattern.substring(0, pattern.length - 1));
    return type == pattern;
  });
}
