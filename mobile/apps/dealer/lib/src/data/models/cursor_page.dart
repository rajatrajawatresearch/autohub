import 'package:autohub_core/autohub_core.dart';
import 'package:flutter/foundation.dart';

/// One page of a cursor-paginated list, per §0 of `contracts/registry-api.md`.
///
/// Hand-written rather than generated because `freezed` cannot express a generic union without a
/// build step, and the dealer binary carries no `build_runner`. There is nothing here worth
/// generating: three fields and a decoder.
///
/// [totalEstimate] keeps the contract's word in its name. It is an estimate — never a `COUNT(*)`
/// on a hot table — so the UI says "about 1,284" and never promises a precision the server did not
/// claim. A dealer who reconciles against an exact-looking figure that moves between two refreshes
/// stops trusting the whole screen.
@immutable
final class CursorPage<T> {
  const CursorPage({
    this.items = const <Never>[],
    this.nextCursor,
    this.totalEstimate,
  });

  final List<T> items;

  /// Null when the list is exhausted. Passing it back is the *only* way to get the next page —
  /// re-sending the same query without it fetches page one forever.
  final String? nextCursor;

  final int? totalEstimate;

  bool get hasMore => nextCursor != null && nextCursor!.isNotEmpty;

  /// Builds a reader for `ApiClient.get`, given the item's own decoder.
  ///
  /// A row that will not decode is **dropped, not fatal**. This is the same trade the `Media` model
  /// in `autohub_core` documents: on the web one photoless listing failed the parse for an entire
  /// search page and the marketplace rendered empty. A dealer's inventory list missing one row is a
  /// cosmetic defect; the same list refusing to render is a dealer who cannot work.
  static JsonReader<CursorPage<T>> reader<T>(T Function(JsonMap json) item) {
    return (JsonMap json) {
      final raw = json['items'];
      final items = <T>[];
      if (raw is List<Object?>) {
        for (final entry in raw) {
          if (entry is! Map<String, Object?>) continue;
          try {
            items.add(item(entry));
          } on Object {
            continue;
          }
        }
      }

      final cursor = json['nextCursor'];
      final total = json['totalEstimate'];

      return CursorPage<T>(
        items: items,
        nextCursor: cursor is String && cursor.isNotEmpty ? cursor : null,
        totalEstimate: total is int ? total : null,
      );
    };
  }

  /// Appends the next page onto this one, keeping the newer cursor.
  ///
  /// Used by the inventory list's "load more". Written here rather than at the call site because
  /// getting it wrong — keeping the *old* cursor — produces an infinite list that silently repeats
  /// its second page, which reads as duplicated stock rather than as a paging bug.
  CursorPage<T> concat(CursorPage<T> next) => CursorPage<T>(
        items: <T>[...items, ...next.items],
        nextCursor: next.nextCursor,
        totalEstimate: next.totalEstimate ?? totalEstimate,
      );
}
