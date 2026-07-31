/// Which side of the attachment market this device belongs to.
///
/// Attachment is the one module in AutoHub that is a genuine two-sided market: the supply side
/// publishes earning capacity — a vehicle, a driver, a route, an availability window — and the
/// demand side publishes a requirement. They are not two views of one list. They create different
/// resources (`listings.listings` + `listing_intents` against `listings.requirements`), they see
/// the match board from opposite ends, and they sit on opposite ends of every rupee that moves.
///
/// **Why this is a legal enum under the prime directive.** It is not a registry mirror. A side is
/// a *route shape* — the same justification `ListingIntent` carries in `autohub_core`: each side
/// is a set of screens somebody wrote, so a third side would need screens, and a compile error at
/// every `switch` is the outcome we want rather than a silent gap in navigation. Nothing here
/// encodes what a side may *do*; that is `GET /platform/features` and the server's
/// `availableActions`, evaluated per user, per city, per contract.
enum PartnerSide {
  /// A taxi owner with one to five vehicles, publishing a vehicle + driver + route + availability.
  supply,

  /// A fleet operator, travel company or corporate transport vendor, publishing a requirement.
  demand;

  /// The value persisted in [PartnerSide.storageKey].
  ///
  /// Spelled out rather than using `name` so a rename of the Dart identifier cannot silently
  /// orphan every device that already chose a side — they would land back on onboarding with no
  /// explanation, which reads as the app having forgotten them.
  String get storageValue => switch (this) {
        PartnerSide.supply => 'supply',
        PartnerSide.demand => 'demand',
      };

  /// The key in `DevicePreferences`. Device-scoped and not a secret: which side of a marketplace
  /// somebody is on is not worth a hardware-backed decrypt on the render path.
  static const String storageKey = 'partner_side';

  /// `null` for anything unrecognised, and that is deliberate.
  ///
  /// A stored value this build does not know — written by a newer version and then downgraded —
  /// must send the user back through the one-tap chooser, not silently pick a side for them. An
  /// owner dropped into the operator's app sees a requirement board with none of their vehicles
  /// on it and concludes their listings were deleted.
  static PartnerSide? tryParse(String? stored) => switch (stored) {
        'supply' => PartnerSide.supply,
        'demand' => PartnerSide.demand,
        _ => null,
      };
}
