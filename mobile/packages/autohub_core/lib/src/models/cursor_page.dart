import 'package:flutter/foundation.dart';

/// One page of a cursor-paginated list, per §0 of `contracts/registry-api.md`.
///
/// Hand-written rather than generated: `freezed` cannot express a generic decoder without a build
/// step in every consumer, and there is nothing here worth generating — three fields and a loop.
///
/// This is the one definition of the envelope in the workspace. All three apps previously carried
/// their own, which is how a contract shape acquires three decoders that agree today and drift
/// apart on the next contract revision. The envelope is not app-specific; a page of auction lots
/// and a page of settlement lines differ only in what they carry.
@immutable
final class CursorPage<T> {
  const CursorPage({
    this.items = const <Never>[],
    this.nextCursor,
    this.totalEstimate,
  });

  /// Decodes an envelope, skipping any item that will not parse.
  ///
  /// Skipping rather than failing is the trade every decode seam in this package makes, and it was
  /// learned expensively on the web: one photoless listing failed the parse for an entire search
  /// page and the marketplace rendered empty. A row missing from a list is a support question; a
  /// list that refuses to render is an outage — and on a handset it is an outage no deploy fixes.
  factory CursorPage.fromJson(
    Map<String, Object?> json,
    T Function(Map<String, Object?> item) read,
  ) {
    final items = <T>[];
    final raw = json['items'];
    if (raw is List<Object?>) {
      for (final entry in raw) {
        if (entry is! Map<String, Object?>) continue;
        try {
          items.add(read(entry));
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
  }

  final List<T> items;

  /// Null when the list is exhausted. Passing it back is the *only* way to get the next page —
  /// re-sending the same query without it fetches page one forever.
  final String? nextCursor;

  /// An estimate by contract, never a `COUNT(*)` on a hot table.
  ///
  /// The name is kept verbatim so a call site cannot forget: the UI says "about 1,284" and never
  /// promises a precision the server did not claim. A dealer reconciling against an exact-looking
  /// figure that moves between two refreshes stops trusting the whole screen.
  final int? totalEstimate;

  /// Whether a further page exists. Read this, never `items.length` against [totalEstimate] — the
  /// estimate is allowed to disagree with the rows, and a list that stops on an item count stops
  /// early or never.
  bool get hasMore => (nextCursor ?? '').isNotEmpty;

  bool get isEmpty => items.isEmpty;

  /// Builds a decoder for `ApiClient.get`, given the item's own decoder.
  ///
  /// Structurally a `JsonReader<CursorPage<T>>`; spelled out longhand so this file — a model —
  /// does not have to import the transport layer to describe its own shape.
  static CursorPage<T> Function(Map<String, Object?> json) reader<T>(
    T Function(Map<String, Object?> json) item,
  ) =>
      (Map<String, Object?> json) => CursorPage<T>.fromJson(json, item);

  /// Appends the next page onto this one, keeping the **newer** cursor.
  ///
  /// Written here rather than at each "load more" call site because getting it wrong — keeping the
  /// old cursor — produces a list that silently repeats its second page forever. That reads to a
  /// user as duplicated stock rather than as a paging bug, so nobody reports it as one.
  CursorPage<T> concat(CursorPage<T> next) => CursorPage<T>(
        items: <T>[...items, ...next.items],
        nextCursor: next.nextCursor,
        totalEstimate: next.totalEstimate ?? totalEstimate,
      );
}
