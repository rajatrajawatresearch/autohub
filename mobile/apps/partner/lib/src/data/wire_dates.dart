/// Calendar arithmetic for the dates this app counts down to.
///
/// A permit, a fitness certificate, a PSV badge and a driving licence all expire on a *date*, not
/// at an instant. That distinction is the whole content of this file: subtracting two `DateTime`s
/// and reading `.inDays` answers "how many whole 24-hour periods", which is off by one for half of
/// every day and turns a document that expires tomorrow into one that expires today.
///
/// Dates are rendered verbatim, in mono, exactly as the server sent them (`2026-12-12`). Nothing
/// here formats one. Month names are localisation-registry content, and inventing an English
/// `12 Dec 2026` in the client is how a Kannada-speaking owner-driver ends up reading an English
/// month name on the one screen that decides whether their income pauses. An ISO date is
/// unambiguous in all eight languages the product serves and is what the document itself is
/// checked against.
library;

/// The offset every date in this product is anchored to.
///
/// A fixed +05:30 rather than a timezone database, for the same reason `formatIstStamp` in
/// `autohub_ui` uses one: India has a single zone and has never observed daylight saving, so the
/// database would be 900KB to encode a constant. A permit's last valid day is a day in IST, and
/// evaluating it against a handset in UTC bills the owner a day early for half the year.
const Duration istOffset = Duration(hours: 5, minutes: 30);

/// Parses `2026-12-12` — or a full ISO instant — into the IST calendar day it names.
///
/// Returns `null` for anything unparseable rather than throwing. A malformed date on one document
/// row must cost that row its countdown, not the compliance screen.
DateTime? parseIstDay(String? value) {
  final String? text = value?.trim();
  if (text == null || text.isEmpty) return null;

  final DateTime? parsed = DateTime.tryParse(text);
  if (parsed == null) return null;

  // A bare `2026-12-12` parses as local midnight and a full instant parses with its own zone;
  // normalising both through IST is what makes the two comparable.
  final DateTime ist = parsed.isUtc ? parsed.add(istOffset) : parsed;
  return DateTime.utc(ist.year, ist.month, ist.day);
}

/// Whole calendar days from [now] until [value], in IST. Negative once the date has passed.
///
/// [now] is the **server** clock — `AhServerClock.now()` — and never `DateTime.now()`. A handset
/// whose owner set the date by hand would otherwise decide for itself that a permit is still valid,
/// and the first the owner would hear of it is a paused contract and a blocked payout.
int? wholeDaysUntil(String? value, DateTime now) {
  final DateTime? deadline = parseIstDay(value);
  if (deadline == null) return null;

  final DateTime ist = now.toUtc().add(istOffset);
  final DateTime today = DateTime.utc(ist.year, ist.month, ist.day);
  return deadline.difference(today).inDays;
}
