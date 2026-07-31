import 'plate_block.dart';

/// Maps the API's `registration.plateClass` string onto the enum [PlateBlock] paints with.
///
/// This lives in `autohub_ui` — and exactly once — because the mapping is a *rendering* decision:
/// the wire value is a taxonomy row and the five painted plate classes are a property of Indian
/// roads. `autohub_core` deliberately decodes `plateClass` as a nullable `String` so that a series
/// introduced after this build shipped still decodes, still appears in search, and still renders.
///
/// Anything unrecognised falls back to [PlateClass.private] rather than throwing. The failure mode
/// matters: a new plate class rendered as a white plate is a cosmetic inaccuracy on one card, and
/// a thrown exception is a search page that renders empty for every user whose results happened to
/// include one. That trade was already made once on the web — see the `Media` model's note in
/// `autohub_core` about a photoless listing failing an entire page's parse.
///
/// The one thing this function must never become is a `switch` that some screen copies inline. If
/// a sixth class ships, it is added here and every surface gets it.
PlateClass plateClassFromWire(String? wire) => switch (wire?.trim().toLowerCase()) {
      'commercial' => PlateClass.commercial,
      'ev' => PlateClass.ev,
      'government' => PlateClass.government,
      'defence' => PlateClass.defence,
      'private' => PlateClass.private,
      _ => PlateClass.private,
    };
