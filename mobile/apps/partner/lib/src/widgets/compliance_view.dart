import 'package:autohub_ui/autohub_ui.dart';
import 'package:flutter/material.dart';

import '../app/partner_text.dart';
import '../data/attachment_models.dart';

/// Compliance, rendered without this app ever deciding what "expiring" means.
///
/// This is the screen that protects the partner's income, and the temptation it exists to resist is
/// one line of Dart: `if (daysRemaining < 30) showWarning()`. That line is wrong in every direction
/// at once. The threshold is registry data (`attach.min_permit_validity_days`,
/// `attach.dl_min_validity_days`, `attach.pvc_max_age_days`), it differs per contract type — a
/// school-transport police verification goes stale at 180 days where an aggregator's goes stale at
/// 365 — and it moves without a release. A handset that hardcoded thirty days would paint a
/// document green that the server has already used to suspend the contract and block the payout,
/// which is the worst possible direction for that error to point: the owner finds out from their
/// bank balance.
///
/// So the division of labour is exact. The **registry** decides the verdict, which arrives as a
/// `WorkflowState` and becomes a `StatusPill` tone. The **client** does arithmetic on a date, and
/// renders it as a number with its unit. Nothing here is a sentence this app composed.
class ComplianceList extends StatelessWidget {
  const ComplianceList({
    required this.documents,
    required this.now,
    required this.text,
    super.key,
  });

  final List<ComplianceDocument> documents;

  /// The server clock. Never `DateTime.now()` — a handset whose owner set the date by hand would
  /// otherwise decide for itself that a permit is still valid.
  final DateTime now;

  final PartnerText text;

  @override
  Widget build(BuildContext context) {
    final AhSurfaces surfaces = context.ahSurfaces;

    if (documents.isEmpty) {
      return EmptyState(
        title: text(PartnerKeys.complianceTitle),
        body: text(PartnerKeys.complianceBody),
        align: CrossAxisAlignment.start,
      );
    }

    return Column(
      crossAxisAlignment: CrossAxisAlignment.stretch,
      mainAxisSize: MainAxisSize.min,
      children: <Widget>[
        for (var index = 0; index < documents.length; index += 1) ...<Widget>[
          if (index > 0) Divider(height: 1, thickness: 1, color: surfaces.borderHairline),
          Padding(
            padding: const EdgeInsets.symmetric(vertical: AhSpace.md),
            child: ComplianceRow(document: documents[index], now: now, text: text),
          ),
        ],
      ],
    );
  }
}

/// One document: what it is, when it lapses, and the registry's verdict on it.
class ComplianceRow extends StatelessWidget {
  const ComplianceRow({
    required this.document,
    required this.now,
    required this.text,
    super.key,
  });

  final ComplianceDocument document;
  final DateTime now;
  final PartnerText text;

  @override
  Widget build(BuildContext context) {
    final int? days = document.daysRemaining(now);

    return Row(
      crossAxisAlignment: CrossAxisAlignment.start,
      children: <Widget>[
        Expanded(
          child: FieldBlock(
            label: document.label,
            // The date, verbatim and in mono, exactly as the server sent it. No month name is
            // invented here — see `wire_dates.dart` for why an ISO date is the honest rendering
            // across eight languages.
            value: document.validTill,
            mono: true,
            hint: document.holderLabel ?? document.criticalityLabel,
          ),
        ),
        const SizedBox(width: AhSpace.md),
        Column(
          crossAxisAlignment: CrossAxisAlignment.end,
          mainAxisSize: MainAxisSize.min,
          children: <Widget>[
            StatusPill(
              category: document.status.category,
              label: document.status.name,
              size: StatusPillSize.sm,
            ),
            if (days != null && days >= 0) ...<Widget>[
              const SizedBox(height: AhSpace.xs),
              // `22d`, in the same unit vocabulary `formatIso8601Dwell` uses in `autohub_ui`:
              // units are a technical invariant, not business language, so this is the one thing
              // the client may emit that the registry did not supply. A negative countdown is
              // deliberately not rendered — the pill already carries the registry's own word for a
              // lapsed document, and "-3d" would be this app inventing a second opinion.
              Text('${days}d', style: _countdownStyle(context)),
            ],
          ],
        ),
      ],
    );
  }

  TextStyle _countdownStyle(BuildContext context) =>
      AhText.data.copyWith(fontSize: 13, color: context.ahSurfaces.textSecondary);
}

/// The one line a partner actually reads on a list row: the document that lapses first.
///
/// An owner does not scan eight documents; they want to know whether anything is about to stop
/// their income, and only then which thing. Renders nothing at all when nothing on this vehicle
/// expires — an empty strip is the correct amount of noise for "no news".
class ComplianceSummary extends StatelessWidget {
  const ComplianceSummary({
    required this.documents,
    required this.now,
    required this.text,
    super.key,
  });

  final List<ComplianceDocument> documents;
  final DateTime now;
  final PartnerText text;

  @override
  Widget build(BuildContext context) {
    final ComplianceDocument? document = soonestToLapse(documents, now);
    if (document == null) return const SizedBox.shrink();

    return ComplianceRow(document: document, now: now, text: text);
  }
}
