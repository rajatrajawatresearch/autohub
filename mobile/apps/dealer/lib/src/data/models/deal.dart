import 'package:autohub_core/autohub_core.dart';
import 'package:flutter/foundation.dart';

/// A party to a deal. `role` is a registry string — `buyer`, `seller`, `agent`, `financier` — and
/// is never switched on here; it is rendered.
@immutable
final class DealParty {
  const DealParty({
    required this.id,
    required this.displayName,
    this.role = '',
    this.isYou = false,
    this.phone,
  });

  final String id;
  final String displayName;
  final String role;
  final bool isYou;
  final String? phone;

  static DealParty fromJson(JsonMap json) => DealParty(
        id: json['id'] as String? ?? '',
        displayName: json['displayName'] as String? ?? '',
        role: json['role'] as String? ?? '',
        isYou: json['isYou'] as bool? ?? false,
        phone: json['phone'] as String?,
      );
}

/// The card projection of a deal.
///
/// **PROVISIONAL**, mirroring `packages/api/src/schemas/deals.ts` field for field: the deals read
/// model is composed from shapes the contract *does* pin — a listing summary, a workflow state,
/// integer minor units — so reconciling with the published endpoint is additive rather than a
/// rewrite.
///
/// Note what is not modelled: a status string, a stage name, a commission percentage. The stage is
/// [state], which carries the registry's own `category`, and the commission is a set of
/// [PricingLine]s on the detail. Nothing in this file names a fee.
@immutable
final class DealSummary {
  const DealSummary({
    required this.dealId,
    required this.reference,
    required this.listing,
    required this.amountMinor,
    required this.state,
    this.currency = 'INR',
    this.counterparty,
    this.createdAt,
    this.updatedAt,
  });

  final String dealId;

  /// The human-quotable id — what a dealer reads down the phone to support.
  final String reference;

  final ListingSummary listing;

  /// Integer paise. The ledger's figure, not a running total kept on a row somewhere.
  final int amountMinor;

  final String currency;

  /// The pipeline stage. Its `code` is a registry row and its `category` is the closed contract
  /// enum that decides the chip's tone.
  final WorkflowState state;

  final DealParty? counterparty;
  final DateTime? createdAt;
  final DateTime? updatedAt;

  static DealSummary fromJson(JsonMap json) {
    final listing = json['listing'];
    final state = json['state'];
    final party = json['counterparty'];

    return DealSummary(
      dealId: json['dealId'] as String? ?? '',
      reference: json['reference'] as String? ?? '',
      listing: SearchItem.fromJson(
        listing is Map<String, Object?> ? listing : const <String, Object?>{},
      ),
      amountMinor: json['amountMinor'] as int? ?? 0,
      currency: json['currency'] as String? ?? 'INR',
      state: WorkflowState.fromJson(
        state is Map<String, Object?>
            ? state
            // A deal with no state is not renderable as a pipeline card, and the contract always
            // sends one. Decoding to an explicit unknown rather than throwing keeps a single bad
            // row from emptying the board — and `draft` is the one category that promises nothing.
            : const <String, Object?>{'code': 'unknown', 'name': 'Unknown', 'category': 'draft'},
      ),
      counterparty: party is Map<String, Object?> ? DealParty.fromJson(party) : null,
      createdAt: _dateOrNull(json['createdAt']),
      updatedAt: _dateOrNull(json['updatedAt']),
    );
  }

  static DateTime? _dateOrNull(Object? value) =>
      value is String ? DateTime.tryParse(value) : null;
}

/// A document the deal is waiting on. `code` is `rc`, `form_29`, `noc` — codes, never labels.
@immutable
final class DealDocument {
  const DealDocument({
    required this.id,
    required this.code,
    required this.label,
    this.status = '',
    this.url,
  });

  final String id;
  final String code;

  /// Already localised by the server. This client does not own a word for `form_29`.
  final String label;

  final String status;
  final String? url;

  static DealDocument fromJson(JsonMap json) => DealDocument(
        id: json['id'] as String? ?? '',
        code: json['code'] as String? ?? '',
        label: json['label'] as String? ?? '',
        status: json['status'] as String? ?? '',
        url: json['url'] as String?,
      );
}

/// The full deal — and the only honest source of a commission figure in this app.
///
/// [fees] is a list of [PricingLine]s the **pricing registry** produced, each carrying the rule
/// that produced it. Nothing here multiplies an amount by a rate: a dealer disputing a commission
/// gets `ruleName` and `explanation`, which is an answer support can give, rather than a number
/// this binary invented and cannot account for.
@immutable
final class DealDetail {
  const DealDetail({
    required this.summary,
    this.parties = const <DealParty>[],
    this.fees = const <PricingLine>[],
    this.totalFeesMinor = 0,
    this.payableMinor = 0,
    this.escrowHeldMinor,
    this.documents = const <DealDocument>[],
    this.workflow,
  });

  final DealSummary summary;
  final List<DealParty> parties;

  /// Every rupee, with the rule that produced it.
  final List<PricingLine> fees;

  final int totalFeesMinor;
  final int payableMinor;
  final int? escrowHeldMinor;
  final List<DealDocument> documents;

  /// What this dealer may do to this deal right now, already evaluated server-side.
  final WorkflowSnapshot? workflow;

  static DealDetail fromJson(JsonMap json) => DealDetail(
        summary: DealSummary.fromJson(json),
        parties: _list(json['parties'], DealParty.fromJson),
        fees: _list(json['fees'], PricingLine.fromJson),
        totalFeesMinor: json['totalFeesMinor'] as int? ?? 0,
        payableMinor: json['payableMinor'] as int? ?? 0,
        escrowHeldMinor: json['escrowHeldMinor'] as int?,
        documents: _list(json['documents'], DealDocument.fromJson),
        workflow: switch (json['workflow']) {
          final Map<String, Object?> snapshot => WorkflowSnapshot.fromJson(snapshot),
          _ => null,
        },
      );

  /// Drops entries it cannot read rather than failing the deal. A fee line this build does not
  /// understand must not stop a dealer opening the deal it belongs to — the total the server sent
  /// is still authoritative and still rendered.
  static List<T> _list<T>(Object? raw, T Function(JsonMap json) decode) {
    if (raw is! List<Object?>) return const <Never>[];
    final out = <T>[];
    for (final entry in raw) {
      if (entry is! Map<String, Object?>) continue;
      try {
        out.add(decode(entry));
      } on Object {
        continue;
      }
    }
    return out;
  }
}

/// Which side of the table the caller is on. A closed query parameter from the deals endpoint, not
/// a registry — `side=selling` is grammar the server defines.
enum DealSide {
  buying,
  selling;

  String get wire => name;
}

/// Query for `GET /deals/deals`.
@immutable
final class DealListParams {
  const DealListParams({this.side, this.state, this.cursor, this.limit});

  final DealSide? side;

  /// A workflow state *code*. Which codes exist is the workflow registry's answer, never a
  /// constant in this file.
  final String? state;

  final String? cursor;
  final int? limit;

  Map<String, Object?> toQuery() => <String, Object?>{
        'side': side?.wire,
        'state': state,
        'cursor': cursor,
        'limit': limit,
      }..removeWhere((_, Object? value) => value == null || value == '');
}
