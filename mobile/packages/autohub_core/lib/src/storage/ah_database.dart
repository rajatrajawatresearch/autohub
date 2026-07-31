import 'package:drift/drift.dart';

part 'ah_database.g.dart';

/// The registry snapshot cache — one row per registry document the app has ever seen.
///
/// [name] is the cache key and it encodes the *parameters*, not just the endpoint:
/// `forms.sell_vehicle`, `forms.sell_vehicle?context=category:suv`, `i18n.mr-IN`,
/// `taxonomy.attributes?category=suv`. Two different query strings are two different documents and
/// caching them under one key is how a user picking SUV gets a truck's attribute list.
///
/// The primary key is [name] alone rather than `(name, version)`. Keeping superseded versions on a
/// handset costs storage that a ₹6,000 Android does not have, and nothing ever reads them: once
/// taxonomy v42 lands, v41 is not a fallback, it is a wrong answer. Version-pinning where it
/// genuinely matters — a half-finished sell form composed against form v7 — is handled by
/// [DraftRows.schemaVersion], which records the version the *user's data* was shaped by.
class CachedRegistries extends Table {
  TextColumn get name => text()();

  /// The server's `X-Registry-Version`. Monotonic, and the only thing that decides staleness.
  IntColumn get version => integer()();

  /// The `ETag` exactly as received, quotes included, because it goes back out verbatim in
  /// `If-None-Match`. Re-deriving it from [version] would break the moment the server changes its
  /// ETag format, and the failure mode is silent: every revalidation returns 200 and the app
  /// re-downloads the whole taxonomy on every launch.
  TextColumn get etag => text().nullable()();

  /// The response body, unparsed.
  ///
  /// Stored as raw JSON rather than shredded into typed columns so that a contract addition — a new
  /// field on an attribute — survives on disk even though this build of the app cannot read it yet.
  /// A user who updates the app then already has the new data, without a network round trip.
  TextColumn get body => text()();

  /// When the body was last confirmed current — updated on a `304` as well as a `200`, because a
  /// 304 is the server saying "still correct", which is exactly as fresh as a 200.
  DateTimeColumn get fetchedAt => dateTime()();

  @override
  Set<Column<Object>> get primaryKey => {name};
}

/// The write queue. Every mutation the user made that the network has not yet accepted.
///
/// The primary key is [idempotencyKey] and that is a deliberate structural choice: the key
/// identifies the *gesture*, so a double-tap that reaches this table twice writes one row instead
/// of two. The uniqueness that protects the server also protects the queue.
class OutboxRows extends Table {
  /// The `Idempotency-Key` header value. See `WriteIntent.now` — it is minted when the user acts.
  TextColumn get idempotencyKey => text()();

  /// Path only, relative to the API base — `/listings`, `/listings/019fa.../workflow/actions`.
  /// Storing an absolute URL would pin a queued write to the environment it was composed in, and a
  /// staging build's queue would replay into staging after the user was moved to production.
  TextColumn get endpoint => text()();

  /// One of `WriteMethod`'s names. `GET` is absent from that enum, so it cannot appear here.
  TextColumn get method => text()();

  /// JSON request body, or null for a bodyless `DELETE`.
  TextColumn get body => text().nullable()();

  /// Extra headers frozen at gesture time — `X-Active-City` in particular. The city the user was
  /// browsing when they submitted is the city the write belongs to, even if they have since
  /// switched to another one while the queue waited for signal.
  TextColumn get headers => text().nullable()();

  DateTimeColumn get createdAt => dateTime()();

  /// Counts *server rejections only*. A tunnel does not increment it — see `OutboxDrainer`.
  IntColumn get attempts => integer().withDefault(const Constant(0))();

  /// Backoff gate. Rows are invisible to a drain pass until wall-clock passes this.
  DateTimeColumn get nextAttemptAt => dateTime()();

  /// One of `OutboxState`'s names: `pending`, `conflicted`, `poisoned`.
  ///
  /// There is deliberately no `inFlight` state persisted here. If the process is killed mid-send,
  /// an `inFlight` row is a listing the seller believes they submitted that no drain will ever pick
  /// up again. Re-sending a row that actually reached the server is harmless — that is the entire
  /// job of [idempotencyKey] — so the safe failure is to re-send, not to strand.
  TextColumn get state => text()();

  /// Last failure, for the queue screen. Human text; never branched on.
  TextColumn get lastError => text().nullable()();

  /// The `code` from the last Problem Details response. This is what code branches on.
  TextColumn get lastErrorCode => text().nullable()();

  /// The server's view of the world at the moment of a `409`, verbatim from `metadata`.
  ///
  /// Kept so the conflict screen can show the user what actually happened — "this listing is
  /// already live" — rather than a generic failure. Guessing here means guessing about money.
  TextColumn get conflict => text().nullable()();

  @override
  Set<Column<Object>> get primaryKey => {idempotencyKey};
}

/// Autosaved schema-form state, so a tunnel costs nobody twenty minutes.
///
/// Keyed by form key plus entity id, per `docs/10-mobile-architecture.md` §4.
class DraftRows extends Table {
  /// `sell_vehicle`, `inspection_checklist`, `dealer_onboarding`.
  TextColumn get formKey => text()();

  /// The entity being edited, or the empty string for "a new one".
  ///
  /// SQLite does not treat NULL as a value in a composite primary key, so two drafts for two new
  /// listings would both be "the NULL row" and silently overwrite each other. The sentinel is not
  /// prettiness, it is the difference between one draft and two.
  TextColumn get entityId => text()();

  /// The partial payload as JSON. Whatever the user has typed so far, hidden fields included —
  /// stripping those is the submit path's job, not the draft's, because a field that is hidden now
  /// becomes visible again the moment they change `fuel_type` back.
  TextColumn get values => text()();

  /// The form definition version this data was shaped by.
  ///
  /// Recovered drafts are checked against the current form version. A draft composed against v7 and
  /// recovered under v9 may be missing a field that is now required, and the renderer needs to know
  /// that rather than submitting a payload the server will reject for reasons the user cannot see.
  IntColumn get schemaVersion => integer().nullable()();

  DateTimeColumn get updatedAt => dateTime()();

  @override
  Set<Column<Object>> get primaryKey => {formKey, entityId};
}

/// Small non-secret device preferences — the active city, and nothing that would hurt if read.
///
/// Anything that authenticates a user goes to the keychain via `SecureTokenStore` instead.
class PreferenceRows extends Table {
  TextColumn get key => text()();
  TextColumn get value => text()();
  DateTimeColumn get updatedAt => dateTime()();

  @override
  Set<Column<Object>> get primaryKey => {key};
}

/// The single on-device database.
///
/// One file, not three. The registry cache and the outbox are read together on every launch — the
/// shell asks "what do I know?" and "what do I owe?" in the same frame — and three SQLite files
/// means three opens, three journals and three chances for one of them to be corrupt in a way the
/// other two do not notice.
@DriftDatabase(tables: [CachedRegistries, OutboxRows, DraftRows, PreferenceRows])
class AhDatabase extends _$AhDatabase {
  AhDatabase(super.e);

  /// Timestamps are stored as ISO-8601 text rather than unix seconds.
  ///
  /// Second granularity is not enough for the outbox: two writes from one rapid sequence of taps
  /// land in the same second, and FIFO ordering — create the listing, *then* submit it — becomes a
  /// coin flip. Text also keeps the UTC offset visible in the file, which matters the one time
  /// someone has to read a user's queue out of a bug report.
  @override
  DriftDatabaseOptions get options => const DriftDatabaseOptions(storeDateTimeAsText: true);

  /// Bump on every table change and add the matching step below.
  ///
  /// The registry cache is the one table it would be safe to drop and refetch on upgrade, and it is
  /// exactly the table where that is the wrong instinct: an app updated over a hotel wifi that then
  /// drops has thrown away the only copy of the sell form it had.
  @override
  int get schemaVersion => 1;
}
