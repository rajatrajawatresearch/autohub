/// The six categories the workflow registry emits, and one of the very few enums this workspace
/// is allowed to contain.
///
/// The distinction that makes it legal: a workflow's **states** are registry rows — an operations
/// user inserts "Legal hold" into the deal workflow on a Tuesday and it must render on a handset
/// that shipped in March. Its **category** is not. Category is a field in
/// `contracts/registry-api.md`, closed and versioned with the contract, and the whole point of
/// modelling it as an enum is that adding a seventh one is a compile error at every place that
/// has to decide what colour it wears.
///
/// So: never switch on a state `code`. Always switch on its category.
enum AhStateCategory {
  /// Not yet submitted. The user still owns it.
  draft,

  /// Waiting on a human — review, approval, a document upload, a payment.
  awaitingAction,

  /// Moving under its own power. Live, in transit, under inspection.
  inProgress,

  /// Terminal and good. Sold, settled, passed.
  succeeded,

  /// Terminal and bad. Rejected, expired, failed.
  failed,

  /// Terminal and neither. Withdrawn by the party that owned it.
  cancelled;

  /// The `snake_case` spelling on the wire.
  String get wire => switch (this) {
        AhStateCategory.draft => 'draft',
        AhStateCategory.awaitingAction => 'awaiting_action',
        AhStateCategory.inProgress => 'in_progress',
        AhStateCategory.succeeded => 'succeeded',
        AhStateCategory.failed => 'failed',
        AhStateCategory.cancelled => 'cancelled',
      };

  /// Returns `null` for a value this build does not know.
  ///
  /// Null rather than a throw, because the caller is decoding a registry document on a phone that
  /// cannot be hot-fixed: a seventh category shipped by the server must degrade to a neutral chip
  /// on an old build, not take down the screen that renders it. Use [parseOr] at the decode seam.
  static AhStateCategory? tryParse(String wire) => switch (wire) {
        'draft' => AhStateCategory.draft,
        'awaiting_action' => AhStateCategory.awaitingAction,
        'in_progress' => AhStateCategory.inProgress,
        'succeeded' => AhStateCategory.succeeded,
        'failed' => AhStateCategory.failed,
        'cancelled' => AhStateCategory.cancelled,
        _ => null,
      };

  /// [tryParse] with an explicit, visible fallback. The default is [draft] because it is the one
  /// category that promises nothing — an unknown state must never read as "succeeded".
  static AhStateCategory parseOr(
    String wire, [
    AhStateCategory fallback = AhStateCategory.draft,
  ]) =>
      tryParse(wire) ?? fallback;
}
