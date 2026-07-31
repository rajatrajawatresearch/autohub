import 'package:autohub_core/autohub_core.dart';
import 'package:flutter/widgets.dart';

import '../tokens/colors.dart';
import '../tokens/typography.dart';

/// How large the chip reads. [sm] is for a card overlay or a timeline rail; [md] is a header.
enum StatusPillSize { sm, md }

/// Status, driven entirely by the workflow registry.
///
/// The server sends a state's `name` and its `category` — **never a colour**, and never a string
/// this widget is expected to recognise. Inserting a "Legal hold" state into the deal workflow
/// therefore renders correctly on the day operations configures it, on handsets that shipped
/// months earlier, with no app store submission and no mapping table to update here.
///
/// This is the widget where the prime directive is easiest to break and most expensive to have
/// broken: a `switch (status.code)` here compiles, passes review, ships, and then silently
/// renders every new state as grey on a million phones. So the parameter is an
/// [AhStateCategory] — a closed contract enum — and there is nowhere to put a state code.
///
/// The tones do not change between light and dark. A status chip is a sticker on a file: the
/// colours are picked for AA against their own fill, not against the page, so the same amber
/// means the same thing whichever mode the dealer is in.
class StatusPill extends StatelessWidget {
  const StatusPill({
    required this.category,
    required this.label,
    this.size = StatusPillSize.md,
    super.key,
  });

  /// The registry's `category` for the current state.
  final AhStateCategory category;

  /// The state's `name` from the registry, already localised by the caller.
  ///
  /// Not derived from [category] — six words for six categories would be exactly the hardcoded
  /// vocabulary the registry exists to replace, and "Under review" and "Awaiting documents" are
  /// both `awaiting_action`.
  final String label;

  final StatusPillSize size;

  @override
  Widget build(BuildContext context) {
    final tone = toneFor(category);
    final metrics = _metrics(size);

    return Semantics(
      label: label,
      // The uppercasing is a typographic treatment, not content. Without this the node's label
      // merges to "Sold\nSOLD" and a screen reader reads the state twice, the second time as
      // four spelled-out letters.
      excludeSemantics: true,
      child: DecoratedBox(
        decoration: BoxDecoration(
          color: tone.fill,
          borderRadius: const BorderRadius.all(AhRadius.control),
          border: Border.all(color: tone.border),
        ),
        child: Padding(
          padding: EdgeInsets.symmetric(horizontal: metrics.padX, vertical: metrics.padY),
          child: Text(
            label.toUpperCase(),
            style: AhText.label.copyWith(fontSize: metrics.fontSize, color: tone.ink),
            maxLines: 1,
            overflow: TextOverflow.ellipsis,
          ),
        ),
      ),
    );
  }

  /// The single category → colour mapping in the product.
  ///
  /// Exposed so that anything else that has to agree with a chip — a timeline dot, a row accent —
  /// reads it rather than re-deciding that "rejected is red".
  static StatusTone toneFor(AhStateCategory category) => switch (category) {
        // Neutral on purpose. A draft has made no claim yet, and colouring it would imply one.
        AhStateCategory.draft =>
          const StatusTone(AhColors.n100, AhColors.n600, AhColors.n300),
        AhStateCategory.awaitingAction =>
          const StatusTone(AhColors.cautionSoft, AhColors.caution, AhColors.caution),
        AhStateCategory.inProgress =>
          const StatusTone(AhColors.infoSoft, AhColors.info, AhColors.info),
        AhStateCategory.succeeded =>
          const StatusTone(AhColors.clearSoft, AhColors.clear, AhColors.clear),
        AhStateCategory.failed =>
          const StatusTone(AhColors.flagSoft, AhColors.flag, AhColors.flag),
        // Cancelled is not a failure — nobody rejected it, the owner withdrew it. Red would read
        // as blame, so it shares draft's neutral fill with a firmer border to mark it terminal.
        AhStateCategory.cancelled =>
          const StatusTone(AhColors.n100, AhColors.n600, AhColors.n400),
      };

  static _PillMetrics _metrics(StatusPillSize size) => switch (size) {
        StatusPillSize.sm => const _PillMetrics(10, 6, 2),
        StatusPillSize.md => const _PillMetrics(11, 8, 3),
      };
}

/// A category's three colours. Public because [StatusPill.toneFor] returns it.
@immutable
class StatusTone {
  const StatusTone(this.fill, this.ink, this.border);

  final Color fill;
  final Color ink;
  final Color border;
}

@immutable
class _PillMetrics {
  const _PillMetrics(this.fontSize, this.padX, this.padY);

  final double fontSize;
  final double padX;
  final double padY;
}
