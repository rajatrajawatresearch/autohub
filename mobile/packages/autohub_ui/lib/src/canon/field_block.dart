import 'package:autohub_core/autohub_core.dart';
import 'package:flutter/widgets.dart';

import '../theme/ah_theme.dart';
import '../tokens/typography.dart';

/// The RC-card atom: a small uppercase letter-spaced label sitting above its value.
///
/// Nearly everything in this product is made of these. The repetition is deliberate and it is the
/// single largest coherence lever in the design system — it is why a valuation panel, an auction
/// lot header and a settlement drawer read as pages of one document rather than three screens by
/// three people. When building a surface that does not exist yet, reach for this first.
///
/// A genuinely absent value renders an em dash, never an empty row. An empty row looks like a
/// rendering bug; `—` says "we asked and there is no answer", which on a listing detail page is
/// itself information the buyer needs.
class FieldBlock extends StatelessWidget {
  const FieldBlock({
    required this.label,
    this.value,
    this.valueWidget,
    this.mono = false,
    this.align = CrossAxisAlignment.start,
    this.hint,
    super.key,
  });

  final String label;

  /// The text value. Ignored when [valueWidget] is supplied.
  final String? value;

  /// For values that are already a widget — a [MoneyValue], a plate, a status chip.
  final Widget? valueWidget;

  /// Identifiers, money, kms, dates, VINs — anything a user reads, compares or transcribes.
  final bool mono;

  /// [CrossAxisAlignment.end] for the right-hand column of a two-column card, where figures
  /// should agree on their right edge.
  final CrossAxisAlignment align;

  /// A qualifier under the value: a source, a unit, an "as of" date.
  ///
  /// A valuation without "as of today" is a rumour, and this is where that sentence goes.
  final String? hint;

  @override
  Widget build(BuildContext context) {
    final surfaces = context.ahSurfaces;
    final isEnd = align == CrossAxisAlignment.end;
    final textAlign = isEnd ? TextAlign.right : TextAlign.left;
    final empty = valueWidget == null && (value == null || value!.isEmpty);

    final valueStyle = (mono ? AhText.data : AhText.body).copyWith(
      color: empty ? surfaces.textSecondary : surfaces.textPrimary,
    );

    return Column(
      crossAxisAlignment: align,
      mainAxisSize: MainAxisSize.min,
      children: <Widget>[
        Text(
          label.toUpperCase(),
          style: AhText.label.copyWith(color: surfaces.textSecondary),
          textAlign: textAlign,
        ),
        const SizedBox(height: AhSpace.xs),
        if (valueWidget != null)
          valueWidget!
        else
          Text(empty ? ahAbsent : value!, style: valueStyle, textAlign: textAlign),
        if (hint != null) ...<Widget>[
          const SizedBox(height: AhSpace.xs),
          Text(
            hint!,
            style: AhText.bodySm.copyWith(color: surfaces.textSecondary),
            textAlign: textAlign,
          ),
        ],
      ],
    );
  }
}
