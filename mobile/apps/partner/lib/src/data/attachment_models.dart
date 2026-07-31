/// The attachment module's projections, as this app consumes them.
///
/// **PROVISIONAL**, in exactly the sense `ListingDetail` in `autohub_core` uses the word:
/// `docs/contracts/registry-api.md` pins the platform registries, search, valuation and the
/// per-entity workflow read, and `docs/modules/12-rental-lease-attachment.md` §5.12 pins the
/// attachment routes — but neither publishes the response bodies field by field. Everything below
/// is therefore composed from shapes the contract *does* define — `Registration`, `SpecItem`,
/// `Media`, `WorkflowState`, `WorkflowSnapshot`, `PricingQuote` — so reconciling with the published
/// schema is additive rather than a rewrite.
///
/// What none of it does is invent a business constant. There is no contract type here, no rate
/// basis, no cycle length, no deduction order, no commission percentage and no compliance
/// vocabulary. Every one of those is a registry row that arrives already labelled and already
/// categorised, because §5.3 of the module spec is explicit that nine very different commercial
/// arrangements share this module *only* because the differences are rows.
library;

import 'package:autohub_core/autohub_core.dart';
import 'package:flutter/foundation.dart';

import 'wire.dart';
import 'wire_dates.dart';

/// One document a contract or an assignment depends on, with the registry's own verdict on it.
///
/// The critical design point, and the one worth defending: **this app never decides whether a
/// document is "expiring"**. The thresholds are registry data — `attach.dl_min_validity_days`,
/// `attach.min_permit_validity_days`, the T−45/T−15/T−0 sweep — and they differ per contract type
/// (a school-transport police verification goes stale at 180 days where an aggregator's goes stale
/// at 365). A client that hardcoded 30 days would mark a document green that the server has already
/// used to suspend the contract, which is the worst possible direction for that error to point.
///
/// So [status] arrives as a `WorkflowState` and drives the tone through `StatusPill`, and
/// [validTill] is rendered as a date and counted down as a number. The client contributes
/// arithmetic; the registry contributes judgement.
@immutable
final class ComplianceDocument {
  const ComplianceDocument({
    required this.code,
    required this.label,
    required this.status,
    this.validTill,
    this.holderLabel,
    this.criticalityLabel,
  });

  factory ComplianceDocument.fromJson(Map<String, Object?> json) => ComplianceDocument(
        code: requiredStr(json['code'], 'code'),
        label: requiredStr(json['label'], 'label'),
        status: workflowStateOf(json['status']),
        validTill: str(json['validTill']),
        holderLabel: str(json['holderLabel']),
        criticalityLabel: str(json['criticalityLabel']),
      );

  /// The `platform.intent_requirements` requirement code. Used as a key, never as a word.
  final String code;

  /// Already localised by the server — "Contract carriage permit", "PSV badge".
  final String label;

  /// Registry state and category. The category is what colours the chip.
  final WorkflowState status;

  /// ISO date, verbatim. `null` for a one-time document like an Aadhaar KYC.
  final String? validTill;

  /// Whose document it is — the vehicle's or a named driver's — already resolved to a phrase.
  final String? holderLabel;

  /// "Critical" / "High" as the registry weights it. Rendered as supplied; a single lapsed critical
  /// document lapses the whole assignment regardless of the weighted score, and the server has
  /// already applied that rule by the time [status] arrives.
  final String? criticalityLabel;
}

/// A vehicle this owner has published as attachment supply.
@immutable
final class SupplyPost {
  const SupplyPost({
    required this.id,
    required this.title,
    required this.registration,
    required this.state,
    this.listingId,
    this.city,
    this.rateMinor,
    this.rateLabel,
    this.specStrip = const <SpecItem>[],
    this.media = const Media(),
    this.compliance = const <ComplianceDocument>[],
    this.openMatchCount,
  });

  factory SupplyPost.fromJson(Map<String, Object?> json) => SupplyPost(
        id: requiredStr(json['id'], 'id'),
        title: requiredStr(json['title'], 'title'),
        registration: Registration.fromJson(mapOf(json['registration'])),
        state: workflowStateOf(json['state']),
        listingId: str(json['listingId']),
        city: str(json['city']),
        rateMinor: intOrNull(json['rateMinor']),
        rateLabel: str(json['rateLabel']),
        specStrip: decodeAll<SpecItem>(json['specStrip'], SpecItem.fromJson),
        media: Media.fromJson(mapOf(json['media'])),
        compliance: decodeAll<ComplianceDocument>(
          json['compliance'],
          ComplianceDocument.fromJson,
        ),
        openMatchCount: intOrNull(json['openMatchCount']),
      );

  final String id;
  final String title;
  final Registration registration;
  final WorkflowState state;
  final String? listingId;
  final String? city;

  /// Integer paise. What the owner is asking, at whatever basis the registry named.
  final int? rateMinor;

  /// What the money means — "per duty", "per month". From the server, because the rate basis is a
  /// `rate_basis` code set row and a client that spelled it would have to ship a release to add
  /// revenue share.
  final String? rateLabel;

  final List<SpecItem> specStrip;
  final Media media;

  /// Everything this vehicle's eligibility depends on. Empty is a real state — a post the server
  /// has not yet evaluated — and renders as such rather than as "compliant".
  final List<ComplianceDocument> compliance;

  final int? openMatchCount;

  /// The document that will lapse first, or `null` when nothing here expires.
  ComplianceDocument? soonestExpiry(DateTime now) => soonestToLapse(compliance, now);
}

/// The document that lapses first, or `null` when nothing in [documents] carries an expiry.
///
/// This is the value the compliance screen exists to surface. An owner does not read a list of
/// eight documents; they read the one that is about to stop their income, and they open the list
/// afterwards to find out which. Written once, here, so the list row and the detail page cannot
/// disagree about which document that is.
ComplianceDocument? soonestToLapse(List<ComplianceDocument> documents, DateTime now) {
  ComplianceDocument? soonest;
  int? fewest;
  for (final ComplianceDocument document in documents) {
    final int? days = document.daysRemaining(now);
    if (days == null) continue;
    if (fewest == null || days < fewest) {
      fewest = days;
      soonest = document;
    }
  }
  return soonest;
}

/// Days until [ComplianceDocument.validTill], against the server clock.
///
/// An extension rather than a method so the countdown lives beside the calendar rules it depends
/// on and the model stays a pure transport shape.
extension ComplianceCountdown on ComplianceDocument {
  int? daysRemaining(DateTime now) => wholeDaysUntil(validTill, now);
}

/// A requirement this operator has published.
@immutable
final class DemandPost {
  const DemandPost({
    required this.id,
    required this.title,
    required this.state,
    this.requirementId,
    this.city,
    this.quantityRequired,
    this.quantityFulfilled,
    this.rateMinor,
    this.rateLabel,
    this.startsOn,
    this.tenureLabel,
    this.matchCount,
  });

  factory DemandPost.fromJson(Map<String, Object?> json) => DemandPost(
        id: requiredStr(json['id'], 'id'),
        title: requiredStr(json['title'], 'title'),
        state: workflowStateOf(json['state']),
        requirementId: str(json['requirementId']),
        city: str(json['city']),
        quantityRequired: intOrNull(json['quantityRequired']),
        quantityFulfilled: intOrNull(json['quantityFulfilled']),
        rateMinor: intOrNull(json['rateMinor']),
        rateLabel: str(json['rateLabel']),
        startsOn: str(json['startsOn']),
        tenureLabel: str(json['tenureLabel']),
        matchCount: intOrNull(json['matchCount']),
      );

  final String id;
  final String title;
  final WorkflowState state;
  final String? requirementId;
  final String? city;

  /// One requirement, forty contracts, forty owners — the requirement board is a count, not a row
  /// per vehicle.
  final int? quantityRequired;
  final int? quantityFulfilled;

  final int? rateMinor;
  final String? rateLabel;
  final String? startsOn;
  final String? tenureLabel;
  final int? matchCount;
}

/// One factor of a match score, as the scoring rule-set emitted it.
///
/// The breakdown is shown to **both** sides. A marketplace whose whole pitch is that it replaces an
/// opaque broker cannot then be an opaque broker with a different logo, and the module spec makes
/// the point explicitly: the operator sees the same breakdown the owner does.
@immutable
final class MatchFactor {
  const MatchFactor({required this.code, required this.label, this.contribution});

  factory MatchFactor.fromJson(Map<String, Object?> json) => MatchFactor(
        code: requiredStr(json['code'], 'code'),
        label: requiredStr(json['label'], 'label'),
        contribution: doubleOrNull(json['contribution']),
      );

  final String code;

  /// Already localised — "Route overlap 92%", "Two previous contracts with this operator".
  final String label;

  /// Points this factor put on the score. Display only; nothing here recomputes a score.
  final double? contribution;
}

/// A proposed match, with what it will cost stated before it can be accepted.
@immutable
final class AttachmentMatch {
  const AttachmentMatch({
    required this.id,
    required this.title,
    required this.state,
    required this.isEligible,
    this.proposalId,
    this.postId,
    this.postTitle,
    this.counterpartyName,
    this.registration,
    this.score,
    this.factors = const <MatchFactor>[],
    this.rateMinor,
    this.rateLabel,
    this.commission,
    this.denyReasonCode,
    this.denyReasonLabel,
    this.specStrip = const <SpecItem>[],
    this.city,
  });

  factory AttachmentMatch.fromJson(Map<String, Object?> json) {
    final Object? commission = json['commission'];
    return AttachmentMatch(
      id: requiredStr(json['id'], 'id'),
      title: requiredStr(json['title'], 'title'),
      state: workflowStateOf(json['state']),
      isEligible: boolOr(json['eligible'], fallback: json['denyReasonCode'] == null),
      proposalId: str(json['proposalId']),
      postId: str(json['postId']),
      postTitle: str(json['postTitle']),
      counterpartyName: str(json['counterpartyName']),
      registration: json['registration'] == null
          ? null
          : Registration.fromJson(mapOf(json['registration'])),
      score: doubleOrNull(json['score']),
      factors: decodeAll<MatchFactor>(json['factors'], MatchFactor.fromJson),
      rateMinor: intOrNull(json['rateMinor']),
      rateLabel: str(json['rateLabel']),
      commission: commission == null ? null : PricingQuote.fromJson(mapOf(commission)),
      denyReasonCode: str(json['denyReasonCode']),
      denyReasonLabel: str(json['denyReasonLabel']),
      specStrip: decodeAll<SpecItem>(json['specStrip'], SpecItem.fromJson),
      city: str(json['city']),
    );
  }

  final String id;
  final String title;
  final WorkflowState state;

  /// `false` for a candidate the hard filters rejected. Those rows are **kept**, not discarded —
  /// "you matched 0 of 40 requirements, and 31 of them rejected you because your permit expires in
  /// 22 days" is the single most useful sentence this module can say to an owner, and a matching
  /// engine that silently returns nothing teaches them nothing.
  final bool isEligible;

  /// Present once a proposal exists. Acceptance targets the proposal, not the match: a match is an
  /// opinion, a proposal is an offer with terms and an expiry.
  final String? proposalId;

  final String? postId;
  final String? postTitle;
  final String? counterpartyName;
  final Registration? registration;

  /// 0–100, from the registry-weighted model. Rendered, never recomputed.
  final double? score;

  final List<MatchFactor> factors;
  final int? rateMinor;
  final String? rateLabel;

  /// What AutoHub will charge, resolved from the fee registry **before** acceptance.
  ///
  /// `null` means the server has not quoted it, and that is not a rendering detail — see
  /// [canAccept]. The commercial promise of this module is that the cut is on the table before the
  /// owner signs, not discovered on the first statement, and a `null` here is the app being unable
  /// to keep that promise.
  final PricingQuote? commission;

  final String? denyReasonCode;

  /// Already localised — "Permit expires within 45 days". The code beside it is for support.
  final String? denyReasonLabel;

  final List<SpecItem> specStrip;
  final String? city;

  /// Whether this match may be accepted from this screen.
  ///
  /// Three conditions, and the third is the one that matters: there must be a proposal to accept,
  /// the candidate must have survived the hard filters, and **the commission must have been
  /// quoted**. Letting a partner accept a match whose cut the app could not state would make the
  /// platform indistinguishable from the ₹5,000-line-charge broker it exists to replace — the
  /// number would surface on the first monthly statement, which is precisely the arbitrage this
  /// module is supposed to close.
  bool get canAccept => isEligible && proposalId != null && commission != null;
}

/// A live attachment contract, from either side.
@immutable
final class AttachmentContract {
  const AttachmentContract({
    required this.id,
    required this.title,
    required this.state,
    this.publicRef,
    this.contractTypeLabel,
    this.counterpartyName,
    this.registration,
    this.rateMinor,
    this.rateLabel,
    this.cycleLabel,
    this.startsOn,
    this.endsOn,
    this.workflowKey,
    this.workflow,
  });

  factory AttachmentContract.fromJson(Map<String, Object?> json) => AttachmentContract(
        id: requiredStr(json['id'], 'id'),
        title: requiredStr(json['title'], 'title'),
        state: workflowStateOf(json['state']),
        publicRef: str(json['publicRef']),
        contractTypeLabel: str(json['contractTypeLabel']),
        counterpartyName: str(json['counterpartyName']),
        registration: json['registration'] == null
            ? null
            : Registration.fromJson(mapOf(json['registration'])),
        rateMinor: intOrNull(json['rateMinor']),
        rateLabel: str(json['rateLabel']),
        cycleLabel: str(json['cycleLabel']),
        startsOn: str(json['startsOn']),
        endsOn: str(json['endsOn']),
        workflowKey: str(json['workflowKey']),
        workflow: json['workflow'] == null
            ? null
            : WorkflowSnapshot.fromJson(mapOf(json['workflow'])),
      );

  final String id;
  final String title;
  final WorkflowState state;

  /// `ATC-2607-4J8N`. What a partner reads down the phone to support, so it renders in mono.
  final String? publicRef;

  /// "Corporate ETS", "School transport" — an `attachment_contract_type` code row, already
  /// resolved. Nine of them exist and a tenth is a row, which is why this is a string.
  final String? contractTypeLabel;

  final String? counterpartyName;
  final Registration? registration;
  final int? rateMinor;
  final String? rateLabel;

  /// "Monthly", "Fortnightly", "Weekly" — an `attachment_cycle` code row.
  final String? cycleLabel;

  final String? startsOn;

  /// `null` on an evergreen contract that runs until notice is served.
  final String? endsOn;

  /// The workflow definition to fetch for the timeline's state vocabulary. Sent by the server so a
  /// contract type that runs on its own definition renders without a client change.
  final String? workflowKey;

  /// Live state, available actions and history, exactly as `GET /{module}/{resource}/{id}/workflow`
  /// returns it. Buttons are rendered from `availableActions` and authorisation is never
  /// re-derived here — a client that decides what a user may do is a client an attacker can
  /// convince.
  final WorkflowSnapshot? workflow;
}

/// One line of a settlement statement, signed as the server sent it.
///
/// **The sign is not flipped anywhere in this app.** A deduction arrives negative and a
/// top-up arrives positive, and the client's only job is to render and to add. A client that
/// negates what it was handed is a client that can turn a credit note into a debit, and the owner
/// discovers it a month later when the statement stops tying to their bank account.
@immutable
final class SettlementLine {
  const SettlementLine({
    required this.code,
    required this.label,
    required this.amountMinor,
    this.basisLabel,
    this.ruleName,
    this.hsnSac,
  });

  factory SettlementLine.fromJson(Map<String, Object?> json) => SettlementLine(
        code: requiredStr(json['code'], 'code'),
        label: requiredStr(json['label'], 'label'),
        amountMinor: intOr(json['amountMinor'], 0),
        basisLabel: str(json['basisLabel']),
        ruleName: str(json['ruleName']),
        hsnSac: str(json['hsnSac']),
      );

  /// The `fee_components` code. Support reads it; the owner reads [label].
  final String code;
  final String label;

  /// Integer paise, signed.
  final int amountMinor;

  final String? basisLabel;

  /// The rule that produced the line. Not decoration — an owner will ask why ₹1,500 came off, and
  /// "the code says so" is not an answer support can give.
  final String? ruleName;

  final String? hsnSac;
}

/// One settlement cycle: the platform's commission, the deductions, and what lands in the bank.
///
/// One cycle = one `attachment_cycles` row = one `deals.deals` row = one invoice set = one payout.
/// This is where the module earns and where being wrong costs the partner money, so it is the one
/// model in this app with arithmetic on it — see [foots].
@immutable
final class SettlementCycle {
  const SettlementCycle({
    required this.id,
    required this.state,
    required this.agreedGrossMinor,
    required this.netPayableMinor,
    this.publicRef,
    this.contractId,
    this.cycleNo,
    this.periodStart,
    this.periodEnd,
    this.lines = const <SettlementLine>[],
    this.payoutOn,
    this.payoutNote,
    this.invoiceUrl,
    this.invoiceLabel,
    this.currency = 'INR',
  });

  factory SettlementCycle.fromJson(Map<String, Object?> json) => SettlementCycle(
        id: requiredStr(json['id'], 'id'),
        state: workflowStateOf(json['state']),
        agreedGrossMinor: intOr(json['agreedGrossMinor'], 0),
        netPayableMinor: intOr(json['netPayableMinor'], 0),
        publicRef: str(json['publicRef']),
        contractId: str(json['contractId']),
        cycleNo: intOrNull(json['cycleNo']),
        periodStart: str(json['periodStart']),
        periodEnd: str(json['periodEnd']),
        lines: decodeAll<SettlementLine>(json['lines'], SettlementLine.fromJson),
        payoutOn: str(json['payoutOn']),
        payoutNote: str(json['payoutNote']),
        invoiceUrl: str(json['invoiceUrl']),
        invoiceLabel: str(json['invoiceLabel']),
        currency: str(json['currency']) ?? 'INR',
      );

  final String id;
  final WorkflowState state;

  /// The billing basis after three-way reconciliation, in integer paise.
  final int agreedGrossMinor;

  /// What the server says reaches the owner's account, in integer paise. **This is the figure
  /// rendered** — never a total this app added up.
  final int netPayableMinor;

  final String? publicRef;
  final String? contractId;
  final int? cycleNo;
  final String? periodStart;
  final String? periodEnd;

  /// Every adjustment between gross and net, in the order the contract pinned at signing. Order is
  /// money: a penalty applied before or after TDS changes the owner's receipt.
  final List<SettlementLine> lines;

  /// The date the payout is expected, **as the server computed it**.
  ///
  /// The product says T+2 and that is a registry value (`attach.payout_delay_days`), tuned per
  /// market. A client that added two days to the cycle end would print a date the treasury has
  /// never heard of the first time the number changes, and an owner who plans a fuel purchase
  /// around a wrong date is an owner who stops trusting the app.
  final String? payoutOn;

  /// Why there is no date yet — the operator has not funded escrow, the bank account is
  /// unverified. Server prose; the client does not guess a reason from a null.
  final String? payoutNote;

  final String? invoiceUrl;
  final String? invoiceLabel;
  final String currency;

  /// Gross plus every line — what the statement *says* it adds up to.
  int get derivedNetPayableMinor {
    var total = agreedGrossMinor;
    for (final SettlementLine line in lines) {
      total += line.amountMinor;
    }
    return total;
  }

  /// Whether the statement reconciles to the payable figure.
  ///
  /// The module spec's pricing pipeline rounds each component and then sums the rounded values,
  /// specifically so the invoice foots; if it does not foot here, either a line is missing from the
  /// response or a rounding rule disagrees, and both are conditions the owner must not be shown a
  /// confident number for. The screen renders the disagreement and the support reference instead of
  /// silently trusting one of the two figures — a settlement UI that quietly picks the server's
  /// total while displaying lines that contradict it is how a disputed ₹1,500 penalty becomes an
  /// argument nobody can reconstruct.
  ///
  /// Integer paise throughout, so this is an exact comparison and not a tolerance. A tolerance here
  /// would be a decision to accept being wrong by an amount nobody chose.
  bool get foots => lines.isEmpty || derivedNetPayableMinor == netPayableMinor;

  /// Signed paise the statement is out by. Zero when it foots.
  int get reconciliationDeltaMinor => lines.isEmpty ? 0 : netPayableMinor - derivedNetPayableMinor;
}
