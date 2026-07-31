/// Every shape mobile consumes from `docs/contracts/registry-api.md`.
///
/// The wire is **camelCase** — `docs/09-decisions-log.md` D1 settled that against `04`'s
/// snake_case, and `contracts/registry-api.md` is authoritative for anyone writing code. Field
/// names here therefore match the JSON exactly, and where a Dart keyword forces a rename
/// (`required` → `isRequired`) the `@JsonKey(name:)` carries the wire spelling.
///
/// Two decoding policies run through all of it, and both are lessons already paid for:
///
/// **Unknown fields are ignored.** The server adds fields into the current major without
/// announcement, and a client that rejects them is a client that breaks on a Tuesday.
///
/// **Unknown *values* degrade, they do not fail.** An unrecognised facet style, control, validation
/// rule, workflow category or auction event decodes to an explicit fallback case rather than
/// throwing. On the web this was learned the expensive way: one photoless listing failed the parse
/// for an entire search page and the marketplace rendered empty. On a handset the same class of bug
/// cannot be fixed by a deploy — it waits for an app-store review.
library;

export 'auction.dart';
export 'auth.dart';
export 'common.dart';
export 'cursor_page.dart';
export 'form.dart';
export 'listing.dart';
export 'platform.dart';
export 'pricing.dart';
export 'problem.dart';
export 'search.dart';
export 'taxonomy.dart';
export 'valuation.dart';
export 'workflow.dart';
