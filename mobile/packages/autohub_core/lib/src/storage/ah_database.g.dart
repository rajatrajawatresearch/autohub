// GENERATED CODE - DO NOT MODIFY BY HAND

part of 'ah_database.dart';

// ignore_for_file: type=lint
class $CachedRegistriesTable extends CachedRegistries
    with TableInfo<$CachedRegistriesTable, CachedRegistry> {
  @override
  final GeneratedDatabase attachedDatabase;
  final String? _alias;
  $CachedRegistriesTable(this.attachedDatabase, [this._alias]);
  static const VerificationMeta _nameMeta = const VerificationMeta('name');
  @override
  late final GeneratedColumn<String> name = GeneratedColumn<String>(
    'name',
    aliasedName,
    false,
    type: DriftSqlType.string,
    requiredDuringInsert: true,
  );
  static const VerificationMeta _versionMeta = const VerificationMeta(
    'version',
  );
  @override
  late final GeneratedColumn<int> version = GeneratedColumn<int>(
    'version',
    aliasedName,
    false,
    type: DriftSqlType.int,
    requiredDuringInsert: true,
  );
  static const VerificationMeta _etagMeta = const VerificationMeta('etag');
  @override
  late final GeneratedColumn<String> etag = GeneratedColumn<String>(
    'etag',
    aliasedName,
    true,
    type: DriftSqlType.string,
    requiredDuringInsert: false,
  );
  static const VerificationMeta _bodyMeta = const VerificationMeta('body');
  @override
  late final GeneratedColumn<String> body = GeneratedColumn<String>(
    'body',
    aliasedName,
    false,
    type: DriftSqlType.string,
    requiredDuringInsert: true,
  );
  static const VerificationMeta _fetchedAtMeta = const VerificationMeta(
    'fetchedAt',
  );
  @override
  late final GeneratedColumn<DateTime> fetchedAt = GeneratedColumn<DateTime>(
    'fetched_at',
    aliasedName,
    false,
    type: DriftSqlType.dateTime,
    requiredDuringInsert: true,
  );
  @override
  List<GeneratedColumn> get $columns => [name, version, etag, body, fetchedAt];
  @override
  String get aliasedName => _alias ?? actualTableName;
  @override
  String get actualTableName => $name;
  static const String $name = 'cached_registries';
  @override
  VerificationContext validateIntegrity(
    Insertable<CachedRegistry> instance, {
    bool isInserting = false,
  }) {
    final context = VerificationContext();
    final data = instance.toColumns(true);
    if (data.containsKey('name')) {
      context.handle(
        _nameMeta,
        name.isAcceptableOrUnknown(data['name']!, _nameMeta),
      );
    } else if (isInserting) {
      context.missing(_nameMeta);
    }
    if (data.containsKey('version')) {
      context.handle(
        _versionMeta,
        version.isAcceptableOrUnknown(data['version']!, _versionMeta),
      );
    } else if (isInserting) {
      context.missing(_versionMeta);
    }
    if (data.containsKey('etag')) {
      context.handle(
        _etagMeta,
        etag.isAcceptableOrUnknown(data['etag']!, _etagMeta),
      );
    }
    if (data.containsKey('body')) {
      context.handle(
        _bodyMeta,
        body.isAcceptableOrUnknown(data['body']!, _bodyMeta),
      );
    } else if (isInserting) {
      context.missing(_bodyMeta);
    }
    if (data.containsKey('fetched_at')) {
      context.handle(
        _fetchedAtMeta,
        fetchedAt.isAcceptableOrUnknown(data['fetched_at']!, _fetchedAtMeta),
      );
    } else if (isInserting) {
      context.missing(_fetchedAtMeta);
    }
    return context;
  }

  @override
  Set<GeneratedColumn> get $primaryKey => {name};
  @override
  CachedRegistry map(Map<String, dynamic> data, {String? tablePrefix}) {
    final effectivePrefix = tablePrefix != null ? '$tablePrefix.' : '';
    return CachedRegistry(
      name: attachedDatabase.typeMapping.read(
        DriftSqlType.string,
        data['${effectivePrefix}name'],
      )!,
      version: attachedDatabase.typeMapping.read(
        DriftSqlType.int,
        data['${effectivePrefix}version'],
      )!,
      etag: attachedDatabase.typeMapping.read(
        DriftSqlType.string,
        data['${effectivePrefix}etag'],
      ),
      body: attachedDatabase.typeMapping.read(
        DriftSqlType.string,
        data['${effectivePrefix}body'],
      )!,
      fetchedAt: attachedDatabase.typeMapping.read(
        DriftSqlType.dateTime,
        data['${effectivePrefix}fetched_at'],
      )!,
    );
  }

  @override
  $CachedRegistriesTable createAlias(String alias) {
    return $CachedRegistriesTable(attachedDatabase, alias);
  }
}

class CachedRegistry extends DataClass implements Insertable<CachedRegistry> {
  final String name;

  /// The server's `X-Registry-Version`. Monotonic, and the only thing that decides staleness.
  final int version;

  /// The `ETag` exactly as received, quotes included, because it goes back out verbatim in
  /// `If-None-Match`. Re-deriving it from [version] would break the moment the server changes its
  /// ETag format, and the failure mode is silent: every revalidation returns 200 and the app
  /// re-downloads the whole taxonomy on every launch.
  final String? etag;

  /// The response body, unparsed.
  ///
  /// Stored as raw JSON rather than shredded into typed columns so that a contract addition — a new
  /// field on an attribute — survives on disk even though this build of the app cannot read it yet.
  /// A user who updates the app then already has the new data, without a network round trip.
  final String body;

  /// When the body was last confirmed current — updated on a `304` as well as a `200`, because a
  /// 304 is the server saying "still correct", which is exactly as fresh as a 200.
  final DateTime fetchedAt;
  const CachedRegistry({
    required this.name,
    required this.version,
    this.etag,
    required this.body,
    required this.fetchedAt,
  });
  @override
  Map<String, Expression> toColumns(bool nullToAbsent) {
    final map = <String, Expression>{};
    map['name'] = Variable<String>(name);
    map['version'] = Variable<int>(version);
    if (!nullToAbsent || etag != null) {
      map['etag'] = Variable<String>(etag);
    }
    map['body'] = Variable<String>(body);
    map['fetched_at'] = Variable<DateTime>(fetchedAt);
    return map;
  }

  CachedRegistriesCompanion toCompanion(bool nullToAbsent) {
    return CachedRegistriesCompanion(
      name: Value(name),
      version: Value(version),
      etag: etag == null && nullToAbsent ? const Value.absent() : Value(etag),
      body: Value(body),
      fetchedAt: Value(fetchedAt),
    );
  }

  factory CachedRegistry.fromJson(
    Map<String, dynamic> json, {
    ValueSerializer? serializer,
  }) {
    serializer ??= driftRuntimeOptions.defaultSerializer;
    return CachedRegistry(
      name: serializer.fromJson<String>(json['name']),
      version: serializer.fromJson<int>(json['version']),
      etag: serializer.fromJson<String?>(json['etag']),
      body: serializer.fromJson<String>(json['body']),
      fetchedAt: serializer.fromJson<DateTime>(json['fetchedAt']),
    );
  }
  @override
  Map<String, dynamic> toJson({ValueSerializer? serializer}) {
    serializer ??= driftRuntimeOptions.defaultSerializer;
    return <String, dynamic>{
      'name': serializer.toJson<String>(name),
      'version': serializer.toJson<int>(version),
      'etag': serializer.toJson<String?>(etag),
      'body': serializer.toJson<String>(body),
      'fetchedAt': serializer.toJson<DateTime>(fetchedAt),
    };
  }

  CachedRegistry copyWith({
    String? name,
    int? version,
    Value<String?> etag = const Value.absent(),
    String? body,
    DateTime? fetchedAt,
  }) => CachedRegistry(
    name: name ?? this.name,
    version: version ?? this.version,
    etag: etag.present ? etag.value : this.etag,
    body: body ?? this.body,
    fetchedAt: fetchedAt ?? this.fetchedAt,
  );
  CachedRegistry copyWithCompanion(CachedRegistriesCompanion data) {
    return CachedRegistry(
      name: data.name.present ? data.name.value : this.name,
      version: data.version.present ? data.version.value : this.version,
      etag: data.etag.present ? data.etag.value : this.etag,
      body: data.body.present ? data.body.value : this.body,
      fetchedAt: data.fetchedAt.present ? data.fetchedAt.value : this.fetchedAt,
    );
  }

  @override
  String toString() {
    return (StringBuffer('CachedRegistry(')
          ..write('name: $name, ')
          ..write('version: $version, ')
          ..write('etag: $etag, ')
          ..write('body: $body, ')
          ..write('fetchedAt: $fetchedAt')
          ..write(')'))
        .toString();
  }

  @override
  int get hashCode => Object.hash(name, version, etag, body, fetchedAt);
  @override
  bool operator ==(Object other) =>
      identical(this, other) ||
      (other is CachedRegistry &&
          other.name == this.name &&
          other.version == this.version &&
          other.etag == this.etag &&
          other.body == this.body &&
          other.fetchedAt == this.fetchedAt);
}

class CachedRegistriesCompanion extends UpdateCompanion<CachedRegistry> {
  final Value<String> name;
  final Value<int> version;
  final Value<String?> etag;
  final Value<String> body;
  final Value<DateTime> fetchedAt;
  final Value<int> rowid;
  const CachedRegistriesCompanion({
    this.name = const Value.absent(),
    this.version = const Value.absent(),
    this.etag = const Value.absent(),
    this.body = const Value.absent(),
    this.fetchedAt = const Value.absent(),
    this.rowid = const Value.absent(),
  });
  CachedRegistriesCompanion.insert({
    required String name,
    required int version,
    this.etag = const Value.absent(),
    required String body,
    required DateTime fetchedAt,
    this.rowid = const Value.absent(),
  }) : name = Value(name),
       version = Value(version),
       body = Value(body),
       fetchedAt = Value(fetchedAt);
  static Insertable<CachedRegistry> custom({
    Expression<String>? name,
    Expression<int>? version,
    Expression<String>? etag,
    Expression<String>? body,
    Expression<DateTime>? fetchedAt,
    Expression<int>? rowid,
  }) {
    return RawValuesInsertable({
      if (name != null) 'name': name,
      if (version != null) 'version': version,
      if (etag != null) 'etag': etag,
      if (body != null) 'body': body,
      if (fetchedAt != null) 'fetched_at': fetchedAt,
      if (rowid != null) 'rowid': rowid,
    });
  }

  CachedRegistriesCompanion copyWith({
    Value<String>? name,
    Value<int>? version,
    Value<String?>? etag,
    Value<String>? body,
    Value<DateTime>? fetchedAt,
    Value<int>? rowid,
  }) {
    return CachedRegistriesCompanion(
      name: name ?? this.name,
      version: version ?? this.version,
      etag: etag ?? this.etag,
      body: body ?? this.body,
      fetchedAt: fetchedAt ?? this.fetchedAt,
      rowid: rowid ?? this.rowid,
    );
  }

  @override
  Map<String, Expression> toColumns(bool nullToAbsent) {
    final map = <String, Expression>{};
    if (name.present) {
      map['name'] = Variable<String>(name.value);
    }
    if (version.present) {
      map['version'] = Variable<int>(version.value);
    }
    if (etag.present) {
      map['etag'] = Variable<String>(etag.value);
    }
    if (body.present) {
      map['body'] = Variable<String>(body.value);
    }
    if (fetchedAt.present) {
      map['fetched_at'] = Variable<DateTime>(fetchedAt.value);
    }
    if (rowid.present) {
      map['rowid'] = Variable<int>(rowid.value);
    }
    return map;
  }

  @override
  String toString() {
    return (StringBuffer('CachedRegistriesCompanion(')
          ..write('name: $name, ')
          ..write('version: $version, ')
          ..write('etag: $etag, ')
          ..write('body: $body, ')
          ..write('fetchedAt: $fetchedAt, ')
          ..write('rowid: $rowid')
          ..write(')'))
        .toString();
  }
}

class $OutboxRowsTable extends OutboxRows
    with TableInfo<$OutboxRowsTable, OutboxRow> {
  @override
  final GeneratedDatabase attachedDatabase;
  final String? _alias;
  $OutboxRowsTable(this.attachedDatabase, [this._alias]);
  static const VerificationMeta _idempotencyKeyMeta = const VerificationMeta(
    'idempotencyKey',
  );
  @override
  late final GeneratedColumn<String> idempotencyKey = GeneratedColumn<String>(
    'idempotency_key',
    aliasedName,
    false,
    type: DriftSqlType.string,
    requiredDuringInsert: true,
  );
  static const VerificationMeta _endpointMeta = const VerificationMeta(
    'endpoint',
  );
  @override
  late final GeneratedColumn<String> endpoint = GeneratedColumn<String>(
    'endpoint',
    aliasedName,
    false,
    type: DriftSqlType.string,
    requiredDuringInsert: true,
  );
  static const VerificationMeta _methodMeta = const VerificationMeta('method');
  @override
  late final GeneratedColumn<String> method = GeneratedColumn<String>(
    'method',
    aliasedName,
    false,
    type: DriftSqlType.string,
    requiredDuringInsert: true,
  );
  static const VerificationMeta _bodyMeta = const VerificationMeta('body');
  @override
  late final GeneratedColumn<String> body = GeneratedColumn<String>(
    'body',
    aliasedName,
    true,
    type: DriftSqlType.string,
    requiredDuringInsert: false,
  );
  static const VerificationMeta _headersMeta = const VerificationMeta(
    'headers',
  );
  @override
  late final GeneratedColumn<String> headers = GeneratedColumn<String>(
    'headers',
    aliasedName,
    true,
    type: DriftSqlType.string,
    requiredDuringInsert: false,
  );
  static const VerificationMeta _createdAtMeta = const VerificationMeta(
    'createdAt',
  );
  @override
  late final GeneratedColumn<DateTime> createdAt = GeneratedColumn<DateTime>(
    'created_at',
    aliasedName,
    false,
    type: DriftSqlType.dateTime,
    requiredDuringInsert: true,
  );
  static const VerificationMeta _attemptsMeta = const VerificationMeta(
    'attempts',
  );
  @override
  late final GeneratedColumn<int> attempts = GeneratedColumn<int>(
    'attempts',
    aliasedName,
    false,
    type: DriftSqlType.int,
    requiredDuringInsert: false,
    defaultValue: const Constant(0),
  );
  static const VerificationMeta _nextAttemptAtMeta = const VerificationMeta(
    'nextAttemptAt',
  );
  @override
  late final GeneratedColumn<DateTime> nextAttemptAt =
      GeneratedColumn<DateTime>(
        'next_attempt_at',
        aliasedName,
        false,
        type: DriftSqlType.dateTime,
        requiredDuringInsert: true,
      );
  static const VerificationMeta _stateMeta = const VerificationMeta('state');
  @override
  late final GeneratedColumn<String> state = GeneratedColumn<String>(
    'state',
    aliasedName,
    false,
    type: DriftSqlType.string,
    requiredDuringInsert: true,
  );
  static const VerificationMeta _lastErrorMeta = const VerificationMeta(
    'lastError',
  );
  @override
  late final GeneratedColumn<String> lastError = GeneratedColumn<String>(
    'last_error',
    aliasedName,
    true,
    type: DriftSqlType.string,
    requiredDuringInsert: false,
  );
  static const VerificationMeta _lastErrorCodeMeta = const VerificationMeta(
    'lastErrorCode',
  );
  @override
  late final GeneratedColumn<String> lastErrorCode = GeneratedColumn<String>(
    'last_error_code',
    aliasedName,
    true,
    type: DriftSqlType.string,
    requiredDuringInsert: false,
  );
  static const VerificationMeta _conflictMeta = const VerificationMeta(
    'conflict',
  );
  @override
  late final GeneratedColumn<String> conflict = GeneratedColumn<String>(
    'conflict',
    aliasedName,
    true,
    type: DriftSqlType.string,
    requiredDuringInsert: false,
  );
  @override
  List<GeneratedColumn> get $columns => [
    idempotencyKey,
    endpoint,
    method,
    body,
    headers,
    createdAt,
    attempts,
    nextAttemptAt,
    state,
    lastError,
    lastErrorCode,
    conflict,
  ];
  @override
  String get aliasedName => _alias ?? actualTableName;
  @override
  String get actualTableName => $name;
  static const String $name = 'outbox_rows';
  @override
  VerificationContext validateIntegrity(
    Insertable<OutboxRow> instance, {
    bool isInserting = false,
  }) {
    final context = VerificationContext();
    final data = instance.toColumns(true);
    if (data.containsKey('idempotency_key')) {
      context.handle(
        _idempotencyKeyMeta,
        idempotencyKey.isAcceptableOrUnknown(
          data['idempotency_key']!,
          _idempotencyKeyMeta,
        ),
      );
    } else if (isInserting) {
      context.missing(_idempotencyKeyMeta);
    }
    if (data.containsKey('endpoint')) {
      context.handle(
        _endpointMeta,
        endpoint.isAcceptableOrUnknown(data['endpoint']!, _endpointMeta),
      );
    } else if (isInserting) {
      context.missing(_endpointMeta);
    }
    if (data.containsKey('method')) {
      context.handle(
        _methodMeta,
        method.isAcceptableOrUnknown(data['method']!, _methodMeta),
      );
    } else if (isInserting) {
      context.missing(_methodMeta);
    }
    if (data.containsKey('body')) {
      context.handle(
        _bodyMeta,
        body.isAcceptableOrUnknown(data['body']!, _bodyMeta),
      );
    }
    if (data.containsKey('headers')) {
      context.handle(
        _headersMeta,
        headers.isAcceptableOrUnknown(data['headers']!, _headersMeta),
      );
    }
    if (data.containsKey('created_at')) {
      context.handle(
        _createdAtMeta,
        createdAt.isAcceptableOrUnknown(data['created_at']!, _createdAtMeta),
      );
    } else if (isInserting) {
      context.missing(_createdAtMeta);
    }
    if (data.containsKey('attempts')) {
      context.handle(
        _attemptsMeta,
        attempts.isAcceptableOrUnknown(data['attempts']!, _attemptsMeta),
      );
    }
    if (data.containsKey('next_attempt_at')) {
      context.handle(
        _nextAttemptAtMeta,
        nextAttemptAt.isAcceptableOrUnknown(
          data['next_attempt_at']!,
          _nextAttemptAtMeta,
        ),
      );
    } else if (isInserting) {
      context.missing(_nextAttemptAtMeta);
    }
    if (data.containsKey('state')) {
      context.handle(
        _stateMeta,
        state.isAcceptableOrUnknown(data['state']!, _stateMeta),
      );
    } else if (isInserting) {
      context.missing(_stateMeta);
    }
    if (data.containsKey('last_error')) {
      context.handle(
        _lastErrorMeta,
        lastError.isAcceptableOrUnknown(data['last_error']!, _lastErrorMeta),
      );
    }
    if (data.containsKey('last_error_code')) {
      context.handle(
        _lastErrorCodeMeta,
        lastErrorCode.isAcceptableOrUnknown(
          data['last_error_code']!,
          _lastErrorCodeMeta,
        ),
      );
    }
    if (data.containsKey('conflict')) {
      context.handle(
        _conflictMeta,
        conflict.isAcceptableOrUnknown(data['conflict']!, _conflictMeta),
      );
    }
    return context;
  }

  @override
  Set<GeneratedColumn> get $primaryKey => {idempotencyKey};
  @override
  OutboxRow map(Map<String, dynamic> data, {String? tablePrefix}) {
    final effectivePrefix = tablePrefix != null ? '$tablePrefix.' : '';
    return OutboxRow(
      idempotencyKey: attachedDatabase.typeMapping.read(
        DriftSqlType.string,
        data['${effectivePrefix}idempotency_key'],
      )!,
      endpoint: attachedDatabase.typeMapping.read(
        DriftSqlType.string,
        data['${effectivePrefix}endpoint'],
      )!,
      method: attachedDatabase.typeMapping.read(
        DriftSqlType.string,
        data['${effectivePrefix}method'],
      )!,
      body: attachedDatabase.typeMapping.read(
        DriftSqlType.string,
        data['${effectivePrefix}body'],
      ),
      headers: attachedDatabase.typeMapping.read(
        DriftSqlType.string,
        data['${effectivePrefix}headers'],
      ),
      createdAt: attachedDatabase.typeMapping.read(
        DriftSqlType.dateTime,
        data['${effectivePrefix}created_at'],
      )!,
      attempts: attachedDatabase.typeMapping.read(
        DriftSqlType.int,
        data['${effectivePrefix}attempts'],
      )!,
      nextAttemptAt: attachedDatabase.typeMapping.read(
        DriftSqlType.dateTime,
        data['${effectivePrefix}next_attempt_at'],
      )!,
      state: attachedDatabase.typeMapping.read(
        DriftSqlType.string,
        data['${effectivePrefix}state'],
      )!,
      lastError: attachedDatabase.typeMapping.read(
        DriftSqlType.string,
        data['${effectivePrefix}last_error'],
      ),
      lastErrorCode: attachedDatabase.typeMapping.read(
        DriftSqlType.string,
        data['${effectivePrefix}last_error_code'],
      ),
      conflict: attachedDatabase.typeMapping.read(
        DriftSqlType.string,
        data['${effectivePrefix}conflict'],
      ),
    );
  }

  @override
  $OutboxRowsTable createAlias(String alias) {
    return $OutboxRowsTable(attachedDatabase, alias);
  }
}

class OutboxRow extends DataClass implements Insertable<OutboxRow> {
  /// The `Idempotency-Key` header value. See `WriteIntent.now` — it is minted when the user acts.
  final String idempotencyKey;

  /// Path only, relative to the API base — `/listings`, `/listings/019fa.../workflow/actions`.
  /// Storing an absolute URL would pin a queued write to the environment it was composed in, and a
  /// staging build's queue would replay into staging after the user was moved to production.
  final String endpoint;

  /// One of `WriteMethod`'s names. `GET` is absent from that enum, so it cannot appear here.
  final String method;

  /// JSON request body, or null for a bodyless `DELETE`.
  final String? body;

  /// Extra headers frozen at gesture time — `X-Active-City` in particular. The city the user was
  /// browsing when they submitted is the city the write belongs to, even if they have since
  /// switched to another one while the queue waited for signal.
  final String? headers;
  final DateTime createdAt;

  /// Counts *server rejections only*. A tunnel does not increment it — see `OutboxDrainer`.
  final int attempts;

  /// Backoff gate. Rows are invisible to a drain pass until wall-clock passes this.
  final DateTime nextAttemptAt;

  /// One of `OutboxState`'s names: `pending`, `conflicted`, `poisoned`.
  ///
  /// There is deliberately no `inFlight` state persisted here. If the process is killed mid-send,
  /// an `inFlight` row is a listing the seller believes they submitted that no drain will ever pick
  /// up again. Re-sending a row that actually reached the server is harmless — that is the entire
  /// job of [idempotencyKey] — so the safe failure is to re-send, not to strand.
  final String state;

  /// Last failure, for the queue screen. Human text; never branched on.
  final String? lastError;

  /// The `code` from the last Problem Details response. This is what code branches on.
  final String? lastErrorCode;

  /// The server's view of the world at the moment of a `409`, verbatim from `metadata`.
  ///
  /// Kept so the conflict screen can show the user what actually happened — "this listing is
  /// already live" — rather than a generic failure. Guessing here means guessing about money.
  final String? conflict;
  const OutboxRow({
    required this.idempotencyKey,
    required this.endpoint,
    required this.method,
    this.body,
    this.headers,
    required this.createdAt,
    required this.attempts,
    required this.nextAttemptAt,
    required this.state,
    this.lastError,
    this.lastErrorCode,
    this.conflict,
  });
  @override
  Map<String, Expression> toColumns(bool nullToAbsent) {
    final map = <String, Expression>{};
    map['idempotency_key'] = Variable<String>(idempotencyKey);
    map['endpoint'] = Variable<String>(endpoint);
    map['method'] = Variable<String>(method);
    if (!nullToAbsent || body != null) {
      map['body'] = Variable<String>(body);
    }
    if (!nullToAbsent || headers != null) {
      map['headers'] = Variable<String>(headers);
    }
    map['created_at'] = Variable<DateTime>(createdAt);
    map['attempts'] = Variable<int>(attempts);
    map['next_attempt_at'] = Variable<DateTime>(nextAttemptAt);
    map['state'] = Variable<String>(state);
    if (!nullToAbsent || lastError != null) {
      map['last_error'] = Variable<String>(lastError);
    }
    if (!nullToAbsent || lastErrorCode != null) {
      map['last_error_code'] = Variable<String>(lastErrorCode);
    }
    if (!nullToAbsent || conflict != null) {
      map['conflict'] = Variable<String>(conflict);
    }
    return map;
  }

  OutboxRowsCompanion toCompanion(bool nullToAbsent) {
    return OutboxRowsCompanion(
      idempotencyKey: Value(idempotencyKey),
      endpoint: Value(endpoint),
      method: Value(method),
      body: body == null && nullToAbsent ? const Value.absent() : Value(body),
      headers: headers == null && nullToAbsent
          ? const Value.absent()
          : Value(headers),
      createdAt: Value(createdAt),
      attempts: Value(attempts),
      nextAttemptAt: Value(nextAttemptAt),
      state: Value(state),
      lastError: lastError == null && nullToAbsent
          ? const Value.absent()
          : Value(lastError),
      lastErrorCode: lastErrorCode == null && nullToAbsent
          ? const Value.absent()
          : Value(lastErrorCode),
      conflict: conflict == null && nullToAbsent
          ? const Value.absent()
          : Value(conflict),
    );
  }

  factory OutboxRow.fromJson(
    Map<String, dynamic> json, {
    ValueSerializer? serializer,
  }) {
    serializer ??= driftRuntimeOptions.defaultSerializer;
    return OutboxRow(
      idempotencyKey: serializer.fromJson<String>(json['idempotencyKey']),
      endpoint: serializer.fromJson<String>(json['endpoint']),
      method: serializer.fromJson<String>(json['method']),
      body: serializer.fromJson<String?>(json['body']),
      headers: serializer.fromJson<String?>(json['headers']),
      createdAt: serializer.fromJson<DateTime>(json['createdAt']),
      attempts: serializer.fromJson<int>(json['attempts']),
      nextAttemptAt: serializer.fromJson<DateTime>(json['nextAttemptAt']),
      state: serializer.fromJson<String>(json['state']),
      lastError: serializer.fromJson<String?>(json['lastError']),
      lastErrorCode: serializer.fromJson<String?>(json['lastErrorCode']),
      conflict: serializer.fromJson<String?>(json['conflict']),
    );
  }
  @override
  Map<String, dynamic> toJson({ValueSerializer? serializer}) {
    serializer ??= driftRuntimeOptions.defaultSerializer;
    return <String, dynamic>{
      'idempotencyKey': serializer.toJson<String>(idempotencyKey),
      'endpoint': serializer.toJson<String>(endpoint),
      'method': serializer.toJson<String>(method),
      'body': serializer.toJson<String?>(body),
      'headers': serializer.toJson<String?>(headers),
      'createdAt': serializer.toJson<DateTime>(createdAt),
      'attempts': serializer.toJson<int>(attempts),
      'nextAttemptAt': serializer.toJson<DateTime>(nextAttemptAt),
      'state': serializer.toJson<String>(state),
      'lastError': serializer.toJson<String?>(lastError),
      'lastErrorCode': serializer.toJson<String?>(lastErrorCode),
      'conflict': serializer.toJson<String?>(conflict),
    };
  }

  OutboxRow copyWith({
    String? idempotencyKey,
    String? endpoint,
    String? method,
    Value<String?> body = const Value.absent(),
    Value<String?> headers = const Value.absent(),
    DateTime? createdAt,
    int? attempts,
    DateTime? nextAttemptAt,
    String? state,
    Value<String?> lastError = const Value.absent(),
    Value<String?> lastErrorCode = const Value.absent(),
    Value<String?> conflict = const Value.absent(),
  }) => OutboxRow(
    idempotencyKey: idempotencyKey ?? this.idempotencyKey,
    endpoint: endpoint ?? this.endpoint,
    method: method ?? this.method,
    body: body.present ? body.value : this.body,
    headers: headers.present ? headers.value : this.headers,
    createdAt: createdAt ?? this.createdAt,
    attempts: attempts ?? this.attempts,
    nextAttemptAt: nextAttemptAt ?? this.nextAttemptAt,
    state: state ?? this.state,
    lastError: lastError.present ? lastError.value : this.lastError,
    lastErrorCode: lastErrorCode.present
        ? lastErrorCode.value
        : this.lastErrorCode,
    conflict: conflict.present ? conflict.value : this.conflict,
  );
  OutboxRow copyWithCompanion(OutboxRowsCompanion data) {
    return OutboxRow(
      idempotencyKey: data.idempotencyKey.present
          ? data.idempotencyKey.value
          : this.idempotencyKey,
      endpoint: data.endpoint.present ? data.endpoint.value : this.endpoint,
      method: data.method.present ? data.method.value : this.method,
      body: data.body.present ? data.body.value : this.body,
      headers: data.headers.present ? data.headers.value : this.headers,
      createdAt: data.createdAt.present ? data.createdAt.value : this.createdAt,
      attempts: data.attempts.present ? data.attempts.value : this.attempts,
      nextAttemptAt: data.nextAttemptAt.present
          ? data.nextAttemptAt.value
          : this.nextAttemptAt,
      state: data.state.present ? data.state.value : this.state,
      lastError: data.lastError.present ? data.lastError.value : this.lastError,
      lastErrorCode: data.lastErrorCode.present
          ? data.lastErrorCode.value
          : this.lastErrorCode,
      conflict: data.conflict.present ? data.conflict.value : this.conflict,
    );
  }

  @override
  String toString() {
    return (StringBuffer('OutboxRow(')
          ..write('idempotencyKey: $idempotencyKey, ')
          ..write('endpoint: $endpoint, ')
          ..write('method: $method, ')
          ..write('body: $body, ')
          ..write('headers: $headers, ')
          ..write('createdAt: $createdAt, ')
          ..write('attempts: $attempts, ')
          ..write('nextAttemptAt: $nextAttemptAt, ')
          ..write('state: $state, ')
          ..write('lastError: $lastError, ')
          ..write('lastErrorCode: $lastErrorCode, ')
          ..write('conflict: $conflict')
          ..write(')'))
        .toString();
  }

  @override
  int get hashCode => Object.hash(
    idempotencyKey,
    endpoint,
    method,
    body,
    headers,
    createdAt,
    attempts,
    nextAttemptAt,
    state,
    lastError,
    lastErrorCode,
    conflict,
  );
  @override
  bool operator ==(Object other) =>
      identical(this, other) ||
      (other is OutboxRow &&
          other.idempotencyKey == this.idempotencyKey &&
          other.endpoint == this.endpoint &&
          other.method == this.method &&
          other.body == this.body &&
          other.headers == this.headers &&
          other.createdAt == this.createdAt &&
          other.attempts == this.attempts &&
          other.nextAttemptAt == this.nextAttemptAt &&
          other.state == this.state &&
          other.lastError == this.lastError &&
          other.lastErrorCode == this.lastErrorCode &&
          other.conflict == this.conflict);
}

class OutboxRowsCompanion extends UpdateCompanion<OutboxRow> {
  final Value<String> idempotencyKey;
  final Value<String> endpoint;
  final Value<String> method;
  final Value<String?> body;
  final Value<String?> headers;
  final Value<DateTime> createdAt;
  final Value<int> attempts;
  final Value<DateTime> nextAttemptAt;
  final Value<String> state;
  final Value<String?> lastError;
  final Value<String?> lastErrorCode;
  final Value<String?> conflict;
  final Value<int> rowid;
  const OutboxRowsCompanion({
    this.idempotencyKey = const Value.absent(),
    this.endpoint = const Value.absent(),
    this.method = const Value.absent(),
    this.body = const Value.absent(),
    this.headers = const Value.absent(),
    this.createdAt = const Value.absent(),
    this.attempts = const Value.absent(),
    this.nextAttemptAt = const Value.absent(),
    this.state = const Value.absent(),
    this.lastError = const Value.absent(),
    this.lastErrorCode = const Value.absent(),
    this.conflict = const Value.absent(),
    this.rowid = const Value.absent(),
  });
  OutboxRowsCompanion.insert({
    required String idempotencyKey,
    required String endpoint,
    required String method,
    this.body = const Value.absent(),
    this.headers = const Value.absent(),
    required DateTime createdAt,
    this.attempts = const Value.absent(),
    required DateTime nextAttemptAt,
    required String state,
    this.lastError = const Value.absent(),
    this.lastErrorCode = const Value.absent(),
    this.conflict = const Value.absent(),
    this.rowid = const Value.absent(),
  }) : idempotencyKey = Value(idempotencyKey),
       endpoint = Value(endpoint),
       method = Value(method),
       createdAt = Value(createdAt),
       nextAttemptAt = Value(nextAttemptAt),
       state = Value(state);
  static Insertable<OutboxRow> custom({
    Expression<String>? idempotencyKey,
    Expression<String>? endpoint,
    Expression<String>? method,
    Expression<String>? body,
    Expression<String>? headers,
    Expression<DateTime>? createdAt,
    Expression<int>? attempts,
    Expression<DateTime>? nextAttemptAt,
    Expression<String>? state,
    Expression<String>? lastError,
    Expression<String>? lastErrorCode,
    Expression<String>? conflict,
    Expression<int>? rowid,
  }) {
    return RawValuesInsertable({
      if (idempotencyKey != null) 'idempotency_key': idempotencyKey,
      if (endpoint != null) 'endpoint': endpoint,
      if (method != null) 'method': method,
      if (body != null) 'body': body,
      if (headers != null) 'headers': headers,
      if (createdAt != null) 'created_at': createdAt,
      if (attempts != null) 'attempts': attempts,
      if (nextAttemptAt != null) 'next_attempt_at': nextAttemptAt,
      if (state != null) 'state': state,
      if (lastError != null) 'last_error': lastError,
      if (lastErrorCode != null) 'last_error_code': lastErrorCode,
      if (conflict != null) 'conflict': conflict,
      if (rowid != null) 'rowid': rowid,
    });
  }

  OutboxRowsCompanion copyWith({
    Value<String>? idempotencyKey,
    Value<String>? endpoint,
    Value<String>? method,
    Value<String?>? body,
    Value<String?>? headers,
    Value<DateTime>? createdAt,
    Value<int>? attempts,
    Value<DateTime>? nextAttemptAt,
    Value<String>? state,
    Value<String?>? lastError,
    Value<String?>? lastErrorCode,
    Value<String?>? conflict,
    Value<int>? rowid,
  }) {
    return OutboxRowsCompanion(
      idempotencyKey: idempotencyKey ?? this.idempotencyKey,
      endpoint: endpoint ?? this.endpoint,
      method: method ?? this.method,
      body: body ?? this.body,
      headers: headers ?? this.headers,
      createdAt: createdAt ?? this.createdAt,
      attempts: attempts ?? this.attempts,
      nextAttemptAt: nextAttemptAt ?? this.nextAttemptAt,
      state: state ?? this.state,
      lastError: lastError ?? this.lastError,
      lastErrorCode: lastErrorCode ?? this.lastErrorCode,
      conflict: conflict ?? this.conflict,
      rowid: rowid ?? this.rowid,
    );
  }

  @override
  Map<String, Expression> toColumns(bool nullToAbsent) {
    final map = <String, Expression>{};
    if (idempotencyKey.present) {
      map['idempotency_key'] = Variable<String>(idempotencyKey.value);
    }
    if (endpoint.present) {
      map['endpoint'] = Variable<String>(endpoint.value);
    }
    if (method.present) {
      map['method'] = Variable<String>(method.value);
    }
    if (body.present) {
      map['body'] = Variable<String>(body.value);
    }
    if (headers.present) {
      map['headers'] = Variable<String>(headers.value);
    }
    if (createdAt.present) {
      map['created_at'] = Variable<DateTime>(createdAt.value);
    }
    if (attempts.present) {
      map['attempts'] = Variable<int>(attempts.value);
    }
    if (nextAttemptAt.present) {
      map['next_attempt_at'] = Variable<DateTime>(nextAttemptAt.value);
    }
    if (state.present) {
      map['state'] = Variable<String>(state.value);
    }
    if (lastError.present) {
      map['last_error'] = Variable<String>(lastError.value);
    }
    if (lastErrorCode.present) {
      map['last_error_code'] = Variable<String>(lastErrorCode.value);
    }
    if (conflict.present) {
      map['conflict'] = Variable<String>(conflict.value);
    }
    if (rowid.present) {
      map['rowid'] = Variable<int>(rowid.value);
    }
    return map;
  }

  @override
  String toString() {
    return (StringBuffer('OutboxRowsCompanion(')
          ..write('idempotencyKey: $idempotencyKey, ')
          ..write('endpoint: $endpoint, ')
          ..write('method: $method, ')
          ..write('body: $body, ')
          ..write('headers: $headers, ')
          ..write('createdAt: $createdAt, ')
          ..write('attempts: $attempts, ')
          ..write('nextAttemptAt: $nextAttemptAt, ')
          ..write('state: $state, ')
          ..write('lastError: $lastError, ')
          ..write('lastErrorCode: $lastErrorCode, ')
          ..write('conflict: $conflict, ')
          ..write('rowid: $rowid')
          ..write(')'))
        .toString();
  }
}

class $DraftRowsTable extends DraftRows
    with TableInfo<$DraftRowsTable, DraftRow> {
  @override
  final GeneratedDatabase attachedDatabase;
  final String? _alias;
  $DraftRowsTable(this.attachedDatabase, [this._alias]);
  static const VerificationMeta _formKeyMeta = const VerificationMeta(
    'formKey',
  );
  @override
  late final GeneratedColumn<String> formKey = GeneratedColumn<String>(
    'form_key',
    aliasedName,
    false,
    type: DriftSqlType.string,
    requiredDuringInsert: true,
  );
  static const VerificationMeta _entityIdMeta = const VerificationMeta(
    'entityId',
  );
  @override
  late final GeneratedColumn<String> entityId = GeneratedColumn<String>(
    'entity_id',
    aliasedName,
    false,
    type: DriftSqlType.string,
    requiredDuringInsert: true,
  );
  static const VerificationMeta _valuesMeta = const VerificationMeta('values');
  @override
  late final GeneratedColumn<String> values = GeneratedColumn<String>(
    'values',
    aliasedName,
    false,
    type: DriftSqlType.string,
    requiredDuringInsert: true,
  );
  static const VerificationMeta _schemaVersionMeta = const VerificationMeta(
    'schemaVersion',
  );
  @override
  late final GeneratedColumn<int> schemaVersion = GeneratedColumn<int>(
    'schema_version',
    aliasedName,
    true,
    type: DriftSqlType.int,
    requiredDuringInsert: false,
  );
  static const VerificationMeta _updatedAtMeta = const VerificationMeta(
    'updatedAt',
  );
  @override
  late final GeneratedColumn<DateTime> updatedAt = GeneratedColumn<DateTime>(
    'updated_at',
    aliasedName,
    false,
    type: DriftSqlType.dateTime,
    requiredDuringInsert: true,
  );
  @override
  List<GeneratedColumn> get $columns => [
    formKey,
    entityId,
    values,
    schemaVersion,
    updatedAt,
  ];
  @override
  String get aliasedName => _alias ?? actualTableName;
  @override
  String get actualTableName => $name;
  static const String $name = 'draft_rows';
  @override
  VerificationContext validateIntegrity(
    Insertable<DraftRow> instance, {
    bool isInserting = false,
  }) {
    final context = VerificationContext();
    final data = instance.toColumns(true);
    if (data.containsKey('form_key')) {
      context.handle(
        _formKeyMeta,
        formKey.isAcceptableOrUnknown(data['form_key']!, _formKeyMeta),
      );
    } else if (isInserting) {
      context.missing(_formKeyMeta);
    }
    if (data.containsKey('entity_id')) {
      context.handle(
        _entityIdMeta,
        entityId.isAcceptableOrUnknown(data['entity_id']!, _entityIdMeta),
      );
    } else if (isInserting) {
      context.missing(_entityIdMeta);
    }
    if (data.containsKey('values')) {
      context.handle(
        _valuesMeta,
        values.isAcceptableOrUnknown(data['values']!, _valuesMeta),
      );
    } else if (isInserting) {
      context.missing(_valuesMeta);
    }
    if (data.containsKey('schema_version')) {
      context.handle(
        _schemaVersionMeta,
        schemaVersion.isAcceptableOrUnknown(
          data['schema_version']!,
          _schemaVersionMeta,
        ),
      );
    }
    if (data.containsKey('updated_at')) {
      context.handle(
        _updatedAtMeta,
        updatedAt.isAcceptableOrUnknown(data['updated_at']!, _updatedAtMeta),
      );
    } else if (isInserting) {
      context.missing(_updatedAtMeta);
    }
    return context;
  }

  @override
  Set<GeneratedColumn> get $primaryKey => {formKey, entityId};
  @override
  DraftRow map(Map<String, dynamic> data, {String? tablePrefix}) {
    final effectivePrefix = tablePrefix != null ? '$tablePrefix.' : '';
    return DraftRow(
      formKey: attachedDatabase.typeMapping.read(
        DriftSqlType.string,
        data['${effectivePrefix}form_key'],
      )!,
      entityId: attachedDatabase.typeMapping.read(
        DriftSqlType.string,
        data['${effectivePrefix}entity_id'],
      )!,
      values: attachedDatabase.typeMapping.read(
        DriftSqlType.string,
        data['${effectivePrefix}values'],
      )!,
      schemaVersion: attachedDatabase.typeMapping.read(
        DriftSqlType.int,
        data['${effectivePrefix}schema_version'],
      ),
      updatedAt: attachedDatabase.typeMapping.read(
        DriftSqlType.dateTime,
        data['${effectivePrefix}updated_at'],
      )!,
    );
  }

  @override
  $DraftRowsTable createAlias(String alias) {
    return $DraftRowsTable(attachedDatabase, alias);
  }
}

class DraftRow extends DataClass implements Insertable<DraftRow> {
  /// `sell_vehicle`, `inspection_checklist`, `dealer_onboarding`.
  final String formKey;

  /// The entity being edited, or the empty string for "a new one".
  ///
  /// SQLite does not treat NULL as a value in a composite primary key, so two drafts for two new
  /// listings would both be "the NULL row" and silently overwrite each other. The sentinel is not
  /// prettiness, it is the difference between one draft and two.
  final String entityId;

  /// The partial payload as JSON. Whatever the user has typed so far, hidden fields included —
  /// stripping those is the submit path's job, not the draft's, because a field that is hidden now
  /// becomes visible again the moment they change `fuel_type` back.
  final String values;

  /// The form definition version this data was shaped by.
  ///
  /// Recovered drafts are checked against the current form version. A draft composed against v7 and
  /// recovered under v9 may be missing a field that is now required, and the renderer needs to know
  /// that rather than submitting a payload the server will reject for reasons the user cannot see.
  final int? schemaVersion;
  final DateTime updatedAt;
  const DraftRow({
    required this.formKey,
    required this.entityId,
    required this.values,
    this.schemaVersion,
    required this.updatedAt,
  });
  @override
  Map<String, Expression> toColumns(bool nullToAbsent) {
    final map = <String, Expression>{};
    map['form_key'] = Variable<String>(formKey);
    map['entity_id'] = Variable<String>(entityId);
    map['values'] = Variable<String>(values);
    if (!nullToAbsent || schemaVersion != null) {
      map['schema_version'] = Variable<int>(schemaVersion);
    }
    map['updated_at'] = Variable<DateTime>(updatedAt);
    return map;
  }

  DraftRowsCompanion toCompanion(bool nullToAbsent) {
    return DraftRowsCompanion(
      formKey: Value(formKey),
      entityId: Value(entityId),
      values: Value(values),
      schemaVersion: schemaVersion == null && nullToAbsent
          ? const Value.absent()
          : Value(schemaVersion),
      updatedAt: Value(updatedAt),
    );
  }

  factory DraftRow.fromJson(
    Map<String, dynamic> json, {
    ValueSerializer? serializer,
  }) {
    serializer ??= driftRuntimeOptions.defaultSerializer;
    return DraftRow(
      formKey: serializer.fromJson<String>(json['formKey']),
      entityId: serializer.fromJson<String>(json['entityId']),
      values: serializer.fromJson<String>(json['values']),
      schemaVersion: serializer.fromJson<int?>(json['schemaVersion']),
      updatedAt: serializer.fromJson<DateTime>(json['updatedAt']),
    );
  }
  @override
  Map<String, dynamic> toJson({ValueSerializer? serializer}) {
    serializer ??= driftRuntimeOptions.defaultSerializer;
    return <String, dynamic>{
      'formKey': serializer.toJson<String>(formKey),
      'entityId': serializer.toJson<String>(entityId),
      'values': serializer.toJson<String>(values),
      'schemaVersion': serializer.toJson<int?>(schemaVersion),
      'updatedAt': serializer.toJson<DateTime>(updatedAt),
    };
  }

  DraftRow copyWith({
    String? formKey,
    String? entityId,
    String? values,
    Value<int?> schemaVersion = const Value.absent(),
    DateTime? updatedAt,
  }) => DraftRow(
    formKey: formKey ?? this.formKey,
    entityId: entityId ?? this.entityId,
    values: values ?? this.values,
    schemaVersion: schemaVersion.present
        ? schemaVersion.value
        : this.schemaVersion,
    updatedAt: updatedAt ?? this.updatedAt,
  );
  DraftRow copyWithCompanion(DraftRowsCompanion data) {
    return DraftRow(
      formKey: data.formKey.present ? data.formKey.value : this.formKey,
      entityId: data.entityId.present ? data.entityId.value : this.entityId,
      values: data.values.present ? data.values.value : this.values,
      schemaVersion: data.schemaVersion.present
          ? data.schemaVersion.value
          : this.schemaVersion,
      updatedAt: data.updatedAt.present ? data.updatedAt.value : this.updatedAt,
    );
  }

  @override
  String toString() {
    return (StringBuffer('DraftRow(')
          ..write('formKey: $formKey, ')
          ..write('entityId: $entityId, ')
          ..write('values: $values, ')
          ..write('schemaVersion: $schemaVersion, ')
          ..write('updatedAt: $updatedAt')
          ..write(')'))
        .toString();
  }

  @override
  int get hashCode =>
      Object.hash(formKey, entityId, values, schemaVersion, updatedAt);
  @override
  bool operator ==(Object other) =>
      identical(this, other) ||
      (other is DraftRow &&
          other.formKey == this.formKey &&
          other.entityId == this.entityId &&
          other.values == this.values &&
          other.schemaVersion == this.schemaVersion &&
          other.updatedAt == this.updatedAt);
}

class DraftRowsCompanion extends UpdateCompanion<DraftRow> {
  final Value<String> formKey;
  final Value<String> entityId;
  final Value<String> values;
  final Value<int?> schemaVersion;
  final Value<DateTime> updatedAt;
  final Value<int> rowid;
  const DraftRowsCompanion({
    this.formKey = const Value.absent(),
    this.entityId = const Value.absent(),
    this.values = const Value.absent(),
    this.schemaVersion = const Value.absent(),
    this.updatedAt = const Value.absent(),
    this.rowid = const Value.absent(),
  });
  DraftRowsCompanion.insert({
    required String formKey,
    required String entityId,
    required String values,
    this.schemaVersion = const Value.absent(),
    required DateTime updatedAt,
    this.rowid = const Value.absent(),
  }) : formKey = Value(formKey),
       entityId = Value(entityId),
       values = Value(values),
       updatedAt = Value(updatedAt);
  static Insertable<DraftRow> custom({
    Expression<String>? formKey,
    Expression<String>? entityId,
    Expression<String>? values,
    Expression<int>? schemaVersion,
    Expression<DateTime>? updatedAt,
    Expression<int>? rowid,
  }) {
    return RawValuesInsertable({
      if (formKey != null) 'form_key': formKey,
      if (entityId != null) 'entity_id': entityId,
      if (values != null) 'values': values,
      if (schemaVersion != null) 'schema_version': schemaVersion,
      if (updatedAt != null) 'updated_at': updatedAt,
      if (rowid != null) 'rowid': rowid,
    });
  }

  DraftRowsCompanion copyWith({
    Value<String>? formKey,
    Value<String>? entityId,
    Value<String>? values,
    Value<int?>? schemaVersion,
    Value<DateTime>? updatedAt,
    Value<int>? rowid,
  }) {
    return DraftRowsCompanion(
      formKey: formKey ?? this.formKey,
      entityId: entityId ?? this.entityId,
      values: values ?? this.values,
      schemaVersion: schemaVersion ?? this.schemaVersion,
      updatedAt: updatedAt ?? this.updatedAt,
      rowid: rowid ?? this.rowid,
    );
  }

  @override
  Map<String, Expression> toColumns(bool nullToAbsent) {
    final map = <String, Expression>{};
    if (formKey.present) {
      map['form_key'] = Variable<String>(formKey.value);
    }
    if (entityId.present) {
      map['entity_id'] = Variable<String>(entityId.value);
    }
    if (values.present) {
      map['values'] = Variable<String>(values.value);
    }
    if (schemaVersion.present) {
      map['schema_version'] = Variable<int>(schemaVersion.value);
    }
    if (updatedAt.present) {
      map['updated_at'] = Variable<DateTime>(updatedAt.value);
    }
    if (rowid.present) {
      map['rowid'] = Variable<int>(rowid.value);
    }
    return map;
  }

  @override
  String toString() {
    return (StringBuffer('DraftRowsCompanion(')
          ..write('formKey: $formKey, ')
          ..write('entityId: $entityId, ')
          ..write('values: $values, ')
          ..write('schemaVersion: $schemaVersion, ')
          ..write('updatedAt: $updatedAt, ')
          ..write('rowid: $rowid')
          ..write(')'))
        .toString();
  }
}

class $PreferenceRowsTable extends PreferenceRows
    with TableInfo<$PreferenceRowsTable, PreferenceRow> {
  @override
  final GeneratedDatabase attachedDatabase;
  final String? _alias;
  $PreferenceRowsTable(this.attachedDatabase, [this._alias]);
  static const VerificationMeta _keyMeta = const VerificationMeta('key');
  @override
  late final GeneratedColumn<String> key = GeneratedColumn<String>(
    'key',
    aliasedName,
    false,
    type: DriftSqlType.string,
    requiredDuringInsert: true,
  );
  static const VerificationMeta _valueMeta = const VerificationMeta('value');
  @override
  late final GeneratedColumn<String> value = GeneratedColumn<String>(
    'value',
    aliasedName,
    false,
    type: DriftSqlType.string,
    requiredDuringInsert: true,
  );
  static const VerificationMeta _updatedAtMeta = const VerificationMeta(
    'updatedAt',
  );
  @override
  late final GeneratedColumn<DateTime> updatedAt = GeneratedColumn<DateTime>(
    'updated_at',
    aliasedName,
    false,
    type: DriftSqlType.dateTime,
    requiredDuringInsert: true,
  );
  @override
  List<GeneratedColumn> get $columns => [key, value, updatedAt];
  @override
  String get aliasedName => _alias ?? actualTableName;
  @override
  String get actualTableName => $name;
  static const String $name = 'preference_rows';
  @override
  VerificationContext validateIntegrity(
    Insertable<PreferenceRow> instance, {
    bool isInserting = false,
  }) {
    final context = VerificationContext();
    final data = instance.toColumns(true);
    if (data.containsKey('key')) {
      context.handle(
        _keyMeta,
        key.isAcceptableOrUnknown(data['key']!, _keyMeta),
      );
    } else if (isInserting) {
      context.missing(_keyMeta);
    }
    if (data.containsKey('value')) {
      context.handle(
        _valueMeta,
        value.isAcceptableOrUnknown(data['value']!, _valueMeta),
      );
    } else if (isInserting) {
      context.missing(_valueMeta);
    }
    if (data.containsKey('updated_at')) {
      context.handle(
        _updatedAtMeta,
        updatedAt.isAcceptableOrUnknown(data['updated_at']!, _updatedAtMeta),
      );
    } else if (isInserting) {
      context.missing(_updatedAtMeta);
    }
    return context;
  }

  @override
  Set<GeneratedColumn> get $primaryKey => {key};
  @override
  PreferenceRow map(Map<String, dynamic> data, {String? tablePrefix}) {
    final effectivePrefix = tablePrefix != null ? '$tablePrefix.' : '';
    return PreferenceRow(
      key: attachedDatabase.typeMapping.read(
        DriftSqlType.string,
        data['${effectivePrefix}key'],
      )!,
      value: attachedDatabase.typeMapping.read(
        DriftSqlType.string,
        data['${effectivePrefix}value'],
      )!,
      updatedAt: attachedDatabase.typeMapping.read(
        DriftSqlType.dateTime,
        data['${effectivePrefix}updated_at'],
      )!,
    );
  }

  @override
  $PreferenceRowsTable createAlias(String alias) {
    return $PreferenceRowsTable(attachedDatabase, alias);
  }
}

class PreferenceRow extends DataClass implements Insertable<PreferenceRow> {
  final String key;
  final String value;
  final DateTime updatedAt;
  const PreferenceRow({
    required this.key,
    required this.value,
    required this.updatedAt,
  });
  @override
  Map<String, Expression> toColumns(bool nullToAbsent) {
    final map = <String, Expression>{};
    map['key'] = Variable<String>(key);
    map['value'] = Variable<String>(value);
    map['updated_at'] = Variable<DateTime>(updatedAt);
    return map;
  }

  PreferenceRowsCompanion toCompanion(bool nullToAbsent) {
    return PreferenceRowsCompanion(
      key: Value(key),
      value: Value(value),
      updatedAt: Value(updatedAt),
    );
  }

  factory PreferenceRow.fromJson(
    Map<String, dynamic> json, {
    ValueSerializer? serializer,
  }) {
    serializer ??= driftRuntimeOptions.defaultSerializer;
    return PreferenceRow(
      key: serializer.fromJson<String>(json['key']),
      value: serializer.fromJson<String>(json['value']),
      updatedAt: serializer.fromJson<DateTime>(json['updatedAt']),
    );
  }
  @override
  Map<String, dynamic> toJson({ValueSerializer? serializer}) {
    serializer ??= driftRuntimeOptions.defaultSerializer;
    return <String, dynamic>{
      'key': serializer.toJson<String>(key),
      'value': serializer.toJson<String>(value),
      'updatedAt': serializer.toJson<DateTime>(updatedAt),
    };
  }

  PreferenceRow copyWith({String? key, String? value, DateTime? updatedAt}) =>
      PreferenceRow(
        key: key ?? this.key,
        value: value ?? this.value,
        updatedAt: updatedAt ?? this.updatedAt,
      );
  PreferenceRow copyWithCompanion(PreferenceRowsCompanion data) {
    return PreferenceRow(
      key: data.key.present ? data.key.value : this.key,
      value: data.value.present ? data.value.value : this.value,
      updatedAt: data.updatedAt.present ? data.updatedAt.value : this.updatedAt,
    );
  }

  @override
  String toString() {
    return (StringBuffer('PreferenceRow(')
          ..write('key: $key, ')
          ..write('value: $value, ')
          ..write('updatedAt: $updatedAt')
          ..write(')'))
        .toString();
  }

  @override
  int get hashCode => Object.hash(key, value, updatedAt);
  @override
  bool operator ==(Object other) =>
      identical(this, other) ||
      (other is PreferenceRow &&
          other.key == this.key &&
          other.value == this.value &&
          other.updatedAt == this.updatedAt);
}

class PreferenceRowsCompanion extends UpdateCompanion<PreferenceRow> {
  final Value<String> key;
  final Value<String> value;
  final Value<DateTime> updatedAt;
  final Value<int> rowid;
  const PreferenceRowsCompanion({
    this.key = const Value.absent(),
    this.value = const Value.absent(),
    this.updatedAt = const Value.absent(),
    this.rowid = const Value.absent(),
  });
  PreferenceRowsCompanion.insert({
    required String key,
    required String value,
    required DateTime updatedAt,
    this.rowid = const Value.absent(),
  }) : key = Value(key),
       value = Value(value),
       updatedAt = Value(updatedAt);
  static Insertable<PreferenceRow> custom({
    Expression<String>? key,
    Expression<String>? value,
    Expression<DateTime>? updatedAt,
    Expression<int>? rowid,
  }) {
    return RawValuesInsertable({
      if (key != null) 'key': key,
      if (value != null) 'value': value,
      if (updatedAt != null) 'updated_at': updatedAt,
      if (rowid != null) 'rowid': rowid,
    });
  }

  PreferenceRowsCompanion copyWith({
    Value<String>? key,
    Value<String>? value,
    Value<DateTime>? updatedAt,
    Value<int>? rowid,
  }) {
    return PreferenceRowsCompanion(
      key: key ?? this.key,
      value: value ?? this.value,
      updatedAt: updatedAt ?? this.updatedAt,
      rowid: rowid ?? this.rowid,
    );
  }

  @override
  Map<String, Expression> toColumns(bool nullToAbsent) {
    final map = <String, Expression>{};
    if (key.present) {
      map['key'] = Variable<String>(key.value);
    }
    if (value.present) {
      map['value'] = Variable<String>(value.value);
    }
    if (updatedAt.present) {
      map['updated_at'] = Variable<DateTime>(updatedAt.value);
    }
    if (rowid.present) {
      map['rowid'] = Variable<int>(rowid.value);
    }
    return map;
  }

  @override
  String toString() {
    return (StringBuffer('PreferenceRowsCompanion(')
          ..write('key: $key, ')
          ..write('value: $value, ')
          ..write('updatedAt: $updatedAt, ')
          ..write('rowid: $rowid')
          ..write(')'))
        .toString();
  }
}

abstract class _$AhDatabase extends GeneratedDatabase {
  _$AhDatabase(QueryExecutor e) : super(e);
  $AhDatabaseManager get managers => $AhDatabaseManager(this);
  late final $CachedRegistriesTable cachedRegistries = $CachedRegistriesTable(
    this,
  );
  late final $OutboxRowsTable outboxRows = $OutboxRowsTable(this);
  late final $DraftRowsTable draftRows = $DraftRowsTable(this);
  late final $PreferenceRowsTable preferenceRows = $PreferenceRowsTable(this);
  @override
  Iterable<TableInfo<Table, Object?>> get allTables =>
      allSchemaEntities.whereType<TableInfo<Table, Object?>>();
  @override
  List<DatabaseSchemaEntity> get allSchemaEntities => [
    cachedRegistries,
    outboxRows,
    draftRows,
    preferenceRows,
  ];
}

typedef $$CachedRegistriesTableCreateCompanionBuilder =
    CachedRegistriesCompanion Function({
      required String name,
      required int version,
      Value<String?> etag,
      required String body,
      required DateTime fetchedAt,
      Value<int> rowid,
    });
typedef $$CachedRegistriesTableUpdateCompanionBuilder =
    CachedRegistriesCompanion Function({
      Value<String> name,
      Value<int> version,
      Value<String?> etag,
      Value<String> body,
      Value<DateTime> fetchedAt,
      Value<int> rowid,
    });

class $$CachedRegistriesTableFilterComposer
    extends Composer<_$AhDatabase, $CachedRegistriesTable> {
  $$CachedRegistriesTableFilterComposer({
    required super.$db,
    required super.$table,
    super.joinBuilder,
    super.$addJoinBuilderToRootComposer,
    super.$removeJoinBuilderFromRootComposer,
  });
  ColumnFilters<String> get name => $composableBuilder(
    column: $table.name,
    builder: (column) => ColumnFilters(column),
  );

  ColumnFilters<int> get version => $composableBuilder(
    column: $table.version,
    builder: (column) => ColumnFilters(column),
  );

  ColumnFilters<String> get etag => $composableBuilder(
    column: $table.etag,
    builder: (column) => ColumnFilters(column),
  );

  ColumnFilters<String> get body => $composableBuilder(
    column: $table.body,
    builder: (column) => ColumnFilters(column),
  );

  ColumnFilters<DateTime> get fetchedAt => $composableBuilder(
    column: $table.fetchedAt,
    builder: (column) => ColumnFilters(column),
  );
}

class $$CachedRegistriesTableOrderingComposer
    extends Composer<_$AhDatabase, $CachedRegistriesTable> {
  $$CachedRegistriesTableOrderingComposer({
    required super.$db,
    required super.$table,
    super.joinBuilder,
    super.$addJoinBuilderToRootComposer,
    super.$removeJoinBuilderFromRootComposer,
  });
  ColumnOrderings<String> get name => $composableBuilder(
    column: $table.name,
    builder: (column) => ColumnOrderings(column),
  );

  ColumnOrderings<int> get version => $composableBuilder(
    column: $table.version,
    builder: (column) => ColumnOrderings(column),
  );

  ColumnOrderings<String> get etag => $composableBuilder(
    column: $table.etag,
    builder: (column) => ColumnOrderings(column),
  );

  ColumnOrderings<String> get body => $composableBuilder(
    column: $table.body,
    builder: (column) => ColumnOrderings(column),
  );

  ColumnOrderings<DateTime> get fetchedAt => $composableBuilder(
    column: $table.fetchedAt,
    builder: (column) => ColumnOrderings(column),
  );
}

class $$CachedRegistriesTableAnnotationComposer
    extends Composer<_$AhDatabase, $CachedRegistriesTable> {
  $$CachedRegistriesTableAnnotationComposer({
    required super.$db,
    required super.$table,
    super.joinBuilder,
    super.$addJoinBuilderToRootComposer,
    super.$removeJoinBuilderFromRootComposer,
  });
  GeneratedColumn<String> get name =>
      $composableBuilder(column: $table.name, builder: (column) => column);

  GeneratedColumn<int> get version =>
      $composableBuilder(column: $table.version, builder: (column) => column);

  GeneratedColumn<String> get etag =>
      $composableBuilder(column: $table.etag, builder: (column) => column);

  GeneratedColumn<String> get body =>
      $composableBuilder(column: $table.body, builder: (column) => column);

  GeneratedColumn<DateTime> get fetchedAt =>
      $composableBuilder(column: $table.fetchedAt, builder: (column) => column);
}

class $$CachedRegistriesTableTableManager
    extends
        RootTableManager<
          _$AhDatabase,
          $CachedRegistriesTable,
          CachedRegistry,
          $$CachedRegistriesTableFilterComposer,
          $$CachedRegistriesTableOrderingComposer,
          $$CachedRegistriesTableAnnotationComposer,
          $$CachedRegistriesTableCreateCompanionBuilder,
          $$CachedRegistriesTableUpdateCompanionBuilder,
          (
            CachedRegistry,
            BaseReferences<
              _$AhDatabase,
              $CachedRegistriesTable,
              CachedRegistry
            >,
          ),
          CachedRegistry,
          PrefetchHooks Function()
        > {
  $$CachedRegistriesTableTableManager(
    _$AhDatabase db,
    $CachedRegistriesTable table,
  ) : super(
        TableManagerState(
          db: db,
          table: table,
          createFilteringComposer: () =>
              $$CachedRegistriesTableFilterComposer($db: db, $table: table),
          createOrderingComposer: () =>
              $$CachedRegistriesTableOrderingComposer($db: db, $table: table),
          createComputedFieldComposer: () =>
              $$CachedRegistriesTableAnnotationComposer($db: db, $table: table),
          updateCompanionCallback:
              ({
                Value<String> name = const Value.absent(),
                Value<int> version = const Value.absent(),
                Value<String?> etag = const Value.absent(),
                Value<String> body = const Value.absent(),
                Value<DateTime> fetchedAt = const Value.absent(),
                Value<int> rowid = const Value.absent(),
              }) => CachedRegistriesCompanion(
                name: name,
                version: version,
                etag: etag,
                body: body,
                fetchedAt: fetchedAt,
                rowid: rowid,
              ),
          createCompanionCallback:
              ({
                required String name,
                required int version,
                Value<String?> etag = const Value.absent(),
                required String body,
                required DateTime fetchedAt,
                Value<int> rowid = const Value.absent(),
              }) => CachedRegistriesCompanion.insert(
                name: name,
                version: version,
                etag: etag,
                body: body,
                fetchedAt: fetchedAt,
                rowid: rowid,
              ),
          withReferenceMapper: (p0) => p0
              .map((e) => (e.readTable(table), BaseReferences(db, table, e)))
              .toList(),
          prefetchHooksCallback: null,
        ),
      );
}

typedef $$CachedRegistriesTableProcessedTableManager =
    ProcessedTableManager<
      _$AhDatabase,
      $CachedRegistriesTable,
      CachedRegistry,
      $$CachedRegistriesTableFilterComposer,
      $$CachedRegistriesTableOrderingComposer,
      $$CachedRegistriesTableAnnotationComposer,
      $$CachedRegistriesTableCreateCompanionBuilder,
      $$CachedRegistriesTableUpdateCompanionBuilder,
      (
        CachedRegistry,
        BaseReferences<_$AhDatabase, $CachedRegistriesTable, CachedRegistry>,
      ),
      CachedRegistry,
      PrefetchHooks Function()
    >;
typedef $$OutboxRowsTableCreateCompanionBuilder =
    OutboxRowsCompanion Function({
      required String idempotencyKey,
      required String endpoint,
      required String method,
      Value<String?> body,
      Value<String?> headers,
      required DateTime createdAt,
      Value<int> attempts,
      required DateTime nextAttemptAt,
      required String state,
      Value<String?> lastError,
      Value<String?> lastErrorCode,
      Value<String?> conflict,
      Value<int> rowid,
    });
typedef $$OutboxRowsTableUpdateCompanionBuilder =
    OutboxRowsCompanion Function({
      Value<String> idempotencyKey,
      Value<String> endpoint,
      Value<String> method,
      Value<String?> body,
      Value<String?> headers,
      Value<DateTime> createdAt,
      Value<int> attempts,
      Value<DateTime> nextAttemptAt,
      Value<String> state,
      Value<String?> lastError,
      Value<String?> lastErrorCode,
      Value<String?> conflict,
      Value<int> rowid,
    });

class $$OutboxRowsTableFilterComposer
    extends Composer<_$AhDatabase, $OutboxRowsTable> {
  $$OutboxRowsTableFilterComposer({
    required super.$db,
    required super.$table,
    super.joinBuilder,
    super.$addJoinBuilderToRootComposer,
    super.$removeJoinBuilderFromRootComposer,
  });
  ColumnFilters<String> get idempotencyKey => $composableBuilder(
    column: $table.idempotencyKey,
    builder: (column) => ColumnFilters(column),
  );

  ColumnFilters<String> get endpoint => $composableBuilder(
    column: $table.endpoint,
    builder: (column) => ColumnFilters(column),
  );

  ColumnFilters<String> get method => $composableBuilder(
    column: $table.method,
    builder: (column) => ColumnFilters(column),
  );

  ColumnFilters<String> get body => $composableBuilder(
    column: $table.body,
    builder: (column) => ColumnFilters(column),
  );

  ColumnFilters<String> get headers => $composableBuilder(
    column: $table.headers,
    builder: (column) => ColumnFilters(column),
  );

  ColumnFilters<DateTime> get createdAt => $composableBuilder(
    column: $table.createdAt,
    builder: (column) => ColumnFilters(column),
  );

  ColumnFilters<int> get attempts => $composableBuilder(
    column: $table.attempts,
    builder: (column) => ColumnFilters(column),
  );

  ColumnFilters<DateTime> get nextAttemptAt => $composableBuilder(
    column: $table.nextAttemptAt,
    builder: (column) => ColumnFilters(column),
  );

  ColumnFilters<String> get state => $composableBuilder(
    column: $table.state,
    builder: (column) => ColumnFilters(column),
  );

  ColumnFilters<String> get lastError => $composableBuilder(
    column: $table.lastError,
    builder: (column) => ColumnFilters(column),
  );

  ColumnFilters<String> get lastErrorCode => $composableBuilder(
    column: $table.lastErrorCode,
    builder: (column) => ColumnFilters(column),
  );

  ColumnFilters<String> get conflict => $composableBuilder(
    column: $table.conflict,
    builder: (column) => ColumnFilters(column),
  );
}

class $$OutboxRowsTableOrderingComposer
    extends Composer<_$AhDatabase, $OutboxRowsTable> {
  $$OutboxRowsTableOrderingComposer({
    required super.$db,
    required super.$table,
    super.joinBuilder,
    super.$addJoinBuilderToRootComposer,
    super.$removeJoinBuilderFromRootComposer,
  });
  ColumnOrderings<String> get idempotencyKey => $composableBuilder(
    column: $table.idempotencyKey,
    builder: (column) => ColumnOrderings(column),
  );

  ColumnOrderings<String> get endpoint => $composableBuilder(
    column: $table.endpoint,
    builder: (column) => ColumnOrderings(column),
  );

  ColumnOrderings<String> get method => $composableBuilder(
    column: $table.method,
    builder: (column) => ColumnOrderings(column),
  );

  ColumnOrderings<String> get body => $composableBuilder(
    column: $table.body,
    builder: (column) => ColumnOrderings(column),
  );

  ColumnOrderings<String> get headers => $composableBuilder(
    column: $table.headers,
    builder: (column) => ColumnOrderings(column),
  );

  ColumnOrderings<DateTime> get createdAt => $composableBuilder(
    column: $table.createdAt,
    builder: (column) => ColumnOrderings(column),
  );

  ColumnOrderings<int> get attempts => $composableBuilder(
    column: $table.attempts,
    builder: (column) => ColumnOrderings(column),
  );

  ColumnOrderings<DateTime> get nextAttemptAt => $composableBuilder(
    column: $table.nextAttemptAt,
    builder: (column) => ColumnOrderings(column),
  );

  ColumnOrderings<String> get state => $composableBuilder(
    column: $table.state,
    builder: (column) => ColumnOrderings(column),
  );

  ColumnOrderings<String> get lastError => $composableBuilder(
    column: $table.lastError,
    builder: (column) => ColumnOrderings(column),
  );

  ColumnOrderings<String> get lastErrorCode => $composableBuilder(
    column: $table.lastErrorCode,
    builder: (column) => ColumnOrderings(column),
  );

  ColumnOrderings<String> get conflict => $composableBuilder(
    column: $table.conflict,
    builder: (column) => ColumnOrderings(column),
  );
}

class $$OutboxRowsTableAnnotationComposer
    extends Composer<_$AhDatabase, $OutboxRowsTable> {
  $$OutboxRowsTableAnnotationComposer({
    required super.$db,
    required super.$table,
    super.joinBuilder,
    super.$addJoinBuilderToRootComposer,
    super.$removeJoinBuilderFromRootComposer,
  });
  GeneratedColumn<String> get idempotencyKey => $composableBuilder(
    column: $table.idempotencyKey,
    builder: (column) => column,
  );

  GeneratedColumn<String> get endpoint =>
      $composableBuilder(column: $table.endpoint, builder: (column) => column);

  GeneratedColumn<String> get method =>
      $composableBuilder(column: $table.method, builder: (column) => column);

  GeneratedColumn<String> get body =>
      $composableBuilder(column: $table.body, builder: (column) => column);

  GeneratedColumn<String> get headers =>
      $composableBuilder(column: $table.headers, builder: (column) => column);

  GeneratedColumn<DateTime> get createdAt =>
      $composableBuilder(column: $table.createdAt, builder: (column) => column);

  GeneratedColumn<int> get attempts =>
      $composableBuilder(column: $table.attempts, builder: (column) => column);

  GeneratedColumn<DateTime> get nextAttemptAt => $composableBuilder(
    column: $table.nextAttemptAt,
    builder: (column) => column,
  );

  GeneratedColumn<String> get state =>
      $composableBuilder(column: $table.state, builder: (column) => column);

  GeneratedColumn<String> get lastError =>
      $composableBuilder(column: $table.lastError, builder: (column) => column);

  GeneratedColumn<String> get lastErrorCode => $composableBuilder(
    column: $table.lastErrorCode,
    builder: (column) => column,
  );

  GeneratedColumn<String> get conflict =>
      $composableBuilder(column: $table.conflict, builder: (column) => column);
}

class $$OutboxRowsTableTableManager
    extends
        RootTableManager<
          _$AhDatabase,
          $OutboxRowsTable,
          OutboxRow,
          $$OutboxRowsTableFilterComposer,
          $$OutboxRowsTableOrderingComposer,
          $$OutboxRowsTableAnnotationComposer,
          $$OutboxRowsTableCreateCompanionBuilder,
          $$OutboxRowsTableUpdateCompanionBuilder,
          (
            OutboxRow,
            BaseReferences<_$AhDatabase, $OutboxRowsTable, OutboxRow>,
          ),
          OutboxRow,
          PrefetchHooks Function()
        > {
  $$OutboxRowsTableTableManager(_$AhDatabase db, $OutboxRowsTable table)
    : super(
        TableManagerState(
          db: db,
          table: table,
          createFilteringComposer: () =>
              $$OutboxRowsTableFilterComposer($db: db, $table: table),
          createOrderingComposer: () =>
              $$OutboxRowsTableOrderingComposer($db: db, $table: table),
          createComputedFieldComposer: () =>
              $$OutboxRowsTableAnnotationComposer($db: db, $table: table),
          updateCompanionCallback:
              ({
                Value<String> idempotencyKey = const Value.absent(),
                Value<String> endpoint = const Value.absent(),
                Value<String> method = const Value.absent(),
                Value<String?> body = const Value.absent(),
                Value<String?> headers = const Value.absent(),
                Value<DateTime> createdAt = const Value.absent(),
                Value<int> attempts = const Value.absent(),
                Value<DateTime> nextAttemptAt = const Value.absent(),
                Value<String> state = const Value.absent(),
                Value<String?> lastError = const Value.absent(),
                Value<String?> lastErrorCode = const Value.absent(),
                Value<String?> conflict = const Value.absent(),
                Value<int> rowid = const Value.absent(),
              }) => OutboxRowsCompanion(
                idempotencyKey: idempotencyKey,
                endpoint: endpoint,
                method: method,
                body: body,
                headers: headers,
                createdAt: createdAt,
                attempts: attempts,
                nextAttemptAt: nextAttemptAt,
                state: state,
                lastError: lastError,
                lastErrorCode: lastErrorCode,
                conflict: conflict,
                rowid: rowid,
              ),
          createCompanionCallback:
              ({
                required String idempotencyKey,
                required String endpoint,
                required String method,
                Value<String?> body = const Value.absent(),
                Value<String?> headers = const Value.absent(),
                required DateTime createdAt,
                Value<int> attempts = const Value.absent(),
                required DateTime nextAttemptAt,
                required String state,
                Value<String?> lastError = const Value.absent(),
                Value<String?> lastErrorCode = const Value.absent(),
                Value<String?> conflict = const Value.absent(),
                Value<int> rowid = const Value.absent(),
              }) => OutboxRowsCompanion.insert(
                idempotencyKey: idempotencyKey,
                endpoint: endpoint,
                method: method,
                body: body,
                headers: headers,
                createdAt: createdAt,
                attempts: attempts,
                nextAttemptAt: nextAttemptAt,
                state: state,
                lastError: lastError,
                lastErrorCode: lastErrorCode,
                conflict: conflict,
                rowid: rowid,
              ),
          withReferenceMapper: (p0) => p0
              .map((e) => (e.readTable(table), BaseReferences(db, table, e)))
              .toList(),
          prefetchHooksCallback: null,
        ),
      );
}

typedef $$OutboxRowsTableProcessedTableManager =
    ProcessedTableManager<
      _$AhDatabase,
      $OutboxRowsTable,
      OutboxRow,
      $$OutboxRowsTableFilterComposer,
      $$OutboxRowsTableOrderingComposer,
      $$OutboxRowsTableAnnotationComposer,
      $$OutboxRowsTableCreateCompanionBuilder,
      $$OutboxRowsTableUpdateCompanionBuilder,
      (OutboxRow, BaseReferences<_$AhDatabase, $OutboxRowsTable, OutboxRow>),
      OutboxRow,
      PrefetchHooks Function()
    >;
typedef $$DraftRowsTableCreateCompanionBuilder =
    DraftRowsCompanion Function({
      required String formKey,
      required String entityId,
      required String values,
      Value<int?> schemaVersion,
      required DateTime updatedAt,
      Value<int> rowid,
    });
typedef $$DraftRowsTableUpdateCompanionBuilder =
    DraftRowsCompanion Function({
      Value<String> formKey,
      Value<String> entityId,
      Value<String> values,
      Value<int?> schemaVersion,
      Value<DateTime> updatedAt,
      Value<int> rowid,
    });

class $$DraftRowsTableFilterComposer
    extends Composer<_$AhDatabase, $DraftRowsTable> {
  $$DraftRowsTableFilterComposer({
    required super.$db,
    required super.$table,
    super.joinBuilder,
    super.$addJoinBuilderToRootComposer,
    super.$removeJoinBuilderFromRootComposer,
  });
  ColumnFilters<String> get formKey => $composableBuilder(
    column: $table.formKey,
    builder: (column) => ColumnFilters(column),
  );

  ColumnFilters<String> get entityId => $composableBuilder(
    column: $table.entityId,
    builder: (column) => ColumnFilters(column),
  );

  ColumnFilters<String> get values => $composableBuilder(
    column: $table.values,
    builder: (column) => ColumnFilters(column),
  );

  ColumnFilters<int> get schemaVersion => $composableBuilder(
    column: $table.schemaVersion,
    builder: (column) => ColumnFilters(column),
  );

  ColumnFilters<DateTime> get updatedAt => $composableBuilder(
    column: $table.updatedAt,
    builder: (column) => ColumnFilters(column),
  );
}

class $$DraftRowsTableOrderingComposer
    extends Composer<_$AhDatabase, $DraftRowsTable> {
  $$DraftRowsTableOrderingComposer({
    required super.$db,
    required super.$table,
    super.joinBuilder,
    super.$addJoinBuilderToRootComposer,
    super.$removeJoinBuilderFromRootComposer,
  });
  ColumnOrderings<String> get formKey => $composableBuilder(
    column: $table.formKey,
    builder: (column) => ColumnOrderings(column),
  );

  ColumnOrderings<String> get entityId => $composableBuilder(
    column: $table.entityId,
    builder: (column) => ColumnOrderings(column),
  );

  ColumnOrderings<String> get values => $composableBuilder(
    column: $table.values,
    builder: (column) => ColumnOrderings(column),
  );

  ColumnOrderings<int> get schemaVersion => $composableBuilder(
    column: $table.schemaVersion,
    builder: (column) => ColumnOrderings(column),
  );

  ColumnOrderings<DateTime> get updatedAt => $composableBuilder(
    column: $table.updatedAt,
    builder: (column) => ColumnOrderings(column),
  );
}

class $$DraftRowsTableAnnotationComposer
    extends Composer<_$AhDatabase, $DraftRowsTable> {
  $$DraftRowsTableAnnotationComposer({
    required super.$db,
    required super.$table,
    super.joinBuilder,
    super.$addJoinBuilderToRootComposer,
    super.$removeJoinBuilderFromRootComposer,
  });
  GeneratedColumn<String> get formKey =>
      $composableBuilder(column: $table.formKey, builder: (column) => column);

  GeneratedColumn<String> get entityId =>
      $composableBuilder(column: $table.entityId, builder: (column) => column);

  GeneratedColumn<String> get values =>
      $composableBuilder(column: $table.values, builder: (column) => column);

  GeneratedColumn<int> get schemaVersion => $composableBuilder(
    column: $table.schemaVersion,
    builder: (column) => column,
  );

  GeneratedColumn<DateTime> get updatedAt =>
      $composableBuilder(column: $table.updatedAt, builder: (column) => column);
}

class $$DraftRowsTableTableManager
    extends
        RootTableManager<
          _$AhDatabase,
          $DraftRowsTable,
          DraftRow,
          $$DraftRowsTableFilterComposer,
          $$DraftRowsTableOrderingComposer,
          $$DraftRowsTableAnnotationComposer,
          $$DraftRowsTableCreateCompanionBuilder,
          $$DraftRowsTableUpdateCompanionBuilder,
          (DraftRow, BaseReferences<_$AhDatabase, $DraftRowsTable, DraftRow>),
          DraftRow,
          PrefetchHooks Function()
        > {
  $$DraftRowsTableTableManager(_$AhDatabase db, $DraftRowsTable table)
    : super(
        TableManagerState(
          db: db,
          table: table,
          createFilteringComposer: () =>
              $$DraftRowsTableFilterComposer($db: db, $table: table),
          createOrderingComposer: () =>
              $$DraftRowsTableOrderingComposer($db: db, $table: table),
          createComputedFieldComposer: () =>
              $$DraftRowsTableAnnotationComposer($db: db, $table: table),
          updateCompanionCallback:
              ({
                Value<String> formKey = const Value.absent(),
                Value<String> entityId = const Value.absent(),
                Value<String> values = const Value.absent(),
                Value<int?> schemaVersion = const Value.absent(),
                Value<DateTime> updatedAt = const Value.absent(),
                Value<int> rowid = const Value.absent(),
              }) => DraftRowsCompanion(
                formKey: formKey,
                entityId: entityId,
                values: values,
                schemaVersion: schemaVersion,
                updatedAt: updatedAt,
                rowid: rowid,
              ),
          createCompanionCallback:
              ({
                required String formKey,
                required String entityId,
                required String values,
                Value<int?> schemaVersion = const Value.absent(),
                required DateTime updatedAt,
                Value<int> rowid = const Value.absent(),
              }) => DraftRowsCompanion.insert(
                formKey: formKey,
                entityId: entityId,
                values: values,
                schemaVersion: schemaVersion,
                updatedAt: updatedAt,
                rowid: rowid,
              ),
          withReferenceMapper: (p0) => p0
              .map((e) => (e.readTable(table), BaseReferences(db, table, e)))
              .toList(),
          prefetchHooksCallback: null,
        ),
      );
}

typedef $$DraftRowsTableProcessedTableManager =
    ProcessedTableManager<
      _$AhDatabase,
      $DraftRowsTable,
      DraftRow,
      $$DraftRowsTableFilterComposer,
      $$DraftRowsTableOrderingComposer,
      $$DraftRowsTableAnnotationComposer,
      $$DraftRowsTableCreateCompanionBuilder,
      $$DraftRowsTableUpdateCompanionBuilder,
      (DraftRow, BaseReferences<_$AhDatabase, $DraftRowsTable, DraftRow>),
      DraftRow,
      PrefetchHooks Function()
    >;
typedef $$PreferenceRowsTableCreateCompanionBuilder =
    PreferenceRowsCompanion Function({
      required String key,
      required String value,
      required DateTime updatedAt,
      Value<int> rowid,
    });
typedef $$PreferenceRowsTableUpdateCompanionBuilder =
    PreferenceRowsCompanion Function({
      Value<String> key,
      Value<String> value,
      Value<DateTime> updatedAt,
      Value<int> rowid,
    });

class $$PreferenceRowsTableFilterComposer
    extends Composer<_$AhDatabase, $PreferenceRowsTable> {
  $$PreferenceRowsTableFilterComposer({
    required super.$db,
    required super.$table,
    super.joinBuilder,
    super.$addJoinBuilderToRootComposer,
    super.$removeJoinBuilderFromRootComposer,
  });
  ColumnFilters<String> get key => $composableBuilder(
    column: $table.key,
    builder: (column) => ColumnFilters(column),
  );

  ColumnFilters<String> get value => $composableBuilder(
    column: $table.value,
    builder: (column) => ColumnFilters(column),
  );

  ColumnFilters<DateTime> get updatedAt => $composableBuilder(
    column: $table.updatedAt,
    builder: (column) => ColumnFilters(column),
  );
}

class $$PreferenceRowsTableOrderingComposer
    extends Composer<_$AhDatabase, $PreferenceRowsTable> {
  $$PreferenceRowsTableOrderingComposer({
    required super.$db,
    required super.$table,
    super.joinBuilder,
    super.$addJoinBuilderToRootComposer,
    super.$removeJoinBuilderFromRootComposer,
  });
  ColumnOrderings<String> get key => $composableBuilder(
    column: $table.key,
    builder: (column) => ColumnOrderings(column),
  );

  ColumnOrderings<String> get value => $composableBuilder(
    column: $table.value,
    builder: (column) => ColumnOrderings(column),
  );

  ColumnOrderings<DateTime> get updatedAt => $composableBuilder(
    column: $table.updatedAt,
    builder: (column) => ColumnOrderings(column),
  );
}

class $$PreferenceRowsTableAnnotationComposer
    extends Composer<_$AhDatabase, $PreferenceRowsTable> {
  $$PreferenceRowsTableAnnotationComposer({
    required super.$db,
    required super.$table,
    super.joinBuilder,
    super.$addJoinBuilderToRootComposer,
    super.$removeJoinBuilderFromRootComposer,
  });
  GeneratedColumn<String> get key =>
      $composableBuilder(column: $table.key, builder: (column) => column);

  GeneratedColumn<String> get value =>
      $composableBuilder(column: $table.value, builder: (column) => column);

  GeneratedColumn<DateTime> get updatedAt =>
      $composableBuilder(column: $table.updatedAt, builder: (column) => column);
}

class $$PreferenceRowsTableTableManager
    extends
        RootTableManager<
          _$AhDatabase,
          $PreferenceRowsTable,
          PreferenceRow,
          $$PreferenceRowsTableFilterComposer,
          $$PreferenceRowsTableOrderingComposer,
          $$PreferenceRowsTableAnnotationComposer,
          $$PreferenceRowsTableCreateCompanionBuilder,
          $$PreferenceRowsTableUpdateCompanionBuilder,
          (
            PreferenceRow,
            BaseReferences<_$AhDatabase, $PreferenceRowsTable, PreferenceRow>,
          ),
          PreferenceRow,
          PrefetchHooks Function()
        > {
  $$PreferenceRowsTableTableManager(_$AhDatabase db, $PreferenceRowsTable table)
    : super(
        TableManagerState(
          db: db,
          table: table,
          createFilteringComposer: () =>
              $$PreferenceRowsTableFilterComposer($db: db, $table: table),
          createOrderingComposer: () =>
              $$PreferenceRowsTableOrderingComposer($db: db, $table: table),
          createComputedFieldComposer: () =>
              $$PreferenceRowsTableAnnotationComposer($db: db, $table: table),
          updateCompanionCallback:
              ({
                Value<String> key = const Value.absent(),
                Value<String> value = const Value.absent(),
                Value<DateTime> updatedAt = const Value.absent(),
                Value<int> rowid = const Value.absent(),
              }) => PreferenceRowsCompanion(
                key: key,
                value: value,
                updatedAt: updatedAt,
                rowid: rowid,
              ),
          createCompanionCallback:
              ({
                required String key,
                required String value,
                required DateTime updatedAt,
                Value<int> rowid = const Value.absent(),
              }) => PreferenceRowsCompanion.insert(
                key: key,
                value: value,
                updatedAt: updatedAt,
                rowid: rowid,
              ),
          withReferenceMapper: (p0) => p0
              .map((e) => (e.readTable(table), BaseReferences(db, table, e)))
              .toList(),
          prefetchHooksCallback: null,
        ),
      );
}

typedef $$PreferenceRowsTableProcessedTableManager =
    ProcessedTableManager<
      _$AhDatabase,
      $PreferenceRowsTable,
      PreferenceRow,
      $$PreferenceRowsTableFilterComposer,
      $$PreferenceRowsTableOrderingComposer,
      $$PreferenceRowsTableAnnotationComposer,
      $$PreferenceRowsTableCreateCompanionBuilder,
      $$PreferenceRowsTableUpdateCompanionBuilder,
      (
        PreferenceRow,
        BaseReferences<_$AhDatabase, $PreferenceRowsTable, PreferenceRow>,
      ),
      PreferenceRow,
      PrefetchHooks Function()
    >;

class $AhDatabaseManager {
  final _$AhDatabase _db;
  $AhDatabaseManager(this._db);
  $$CachedRegistriesTableTableManager get cachedRegistries =>
      $$CachedRegistriesTableTableManager(_db, _db.cachedRegistries);
  $$OutboxRowsTableTableManager get outboxRows =>
      $$OutboxRowsTableTableManager(_db, _db.outboxRows);
  $$DraftRowsTableTableManager get draftRows =>
      $$DraftRowsTableTableManager(_db, _db.draftRows);
  $$PreferenceRowsTableTableManager get preferenceRows =>
      $$PreferenceRowsTableTableManager(_db, _db.preferenceRows);
}
