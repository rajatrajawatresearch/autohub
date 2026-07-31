import 'package:autohub_core/autohub_core.dart';
import 'package:flutter/widgets.dart';

import '../theme/ah_theme.dart';
import '../tokens/colors.dart';
import '../tokens/typography.dart';

/// Read from the distance the figure is read at, not from a modular ratio.
enum MoneySize { xs, sm, md, lg, xl }

/// Money never gets a decorative colour. These five are the only meanings a figure can carry.
enum MoneyTone { normal, secondary, accent, positive, negative }

/// Money, the way India writes and says it.
///
/// Amounts arrive as integer paise and are formatted at the edge — here — by
/// [formatIndianMoney] in `autohub_core`. This widget deliberately owns **no** formatting logic:
/// the lakh/crore thresholds, the digit grouping and the rounding promotions all live in one
/// function that the web client's TypeScript twin was ported from, because a phone that renders
/// `₹8.4L` where the laptop renders `₹8,40,000` is two products, and the disagreement is only
/// ever discovered by a customer comparing two screens.
///
/// The rupee sign sits at 0.85em so the digits — which are what the user is actually comparing —
/// hold the optical line. Tabular figures and a slashed zero come from [AhText.data]: a column of
/// prices must align on the decimal, and a `0` a dealer might transcribe must not be an `O`.
class MoneyValue extends StatelessWidget {
  const MoneyValue({
    required this.minorUnits,
    this.compact = false,
    this.size = MoneySize.md,
    this.tone = MoneyTone.normal,
    this.currency = 'INR',
    super.key,
  });

  /// Integer paise. Never rupees, never a `double`.
  final int minorUnits;

  /// `₹8.4L` instead of `₹8,40,000`.
  ///
  /// Right for cards, lists and anything scanned. Wrong for a contract, an invoice, a bid
  /// confirmation or a settlement line — `₹8.4L` is not a figure anyone can be held to.
  final bool compact;

  final MoneySize size;
  final MoneyTone tone;

  /// ISO 4217. Present because the attachment-partner settlement ledger is the one place a
  /// non-INR figure is plausible; everything else leaves it alone.
  final String currency;

  @override
  Widget build(BuildContext context) {
    final formatted = formatIndianMoney(minorUnits, compact: compact, currency: currency);
    final split = _split(formatted);
    final metrics = _metrics(size);
    final color = _color(context, tone);

    final style = AhText.data.copyWith(
      fontSize: metrics.fontSize,
      height: metrics.height,
      fontWeight: metrics.weight,
      color: color,
    );

    return Semantics(
      label: formatted,
      // The visual split into two spans is a typographic device, not content. Excluding the
      // children keeps a screen reader from announcing "rupee" and "8.4L" as two separate nodes.
      excludeSemantics: true,
      child: Text.rich(
        TextSpan(
          children: <InlineSpan>[
            TextSpan(text: split.prefix, style: style.copyWith(fontSize: metrics.fontSize * 0.85)),
            TextSpan(text: split.digits, style: style),
          ],
        ),
        maxLines: 1,
        softWrap: false,
        overflow: TextOverflow.visible,
      ),
    );
  }

  /// Splits `-₹8.4L` into the leading sign-and-symbol run and the rest, so only the symbol is
  /// scaled down. Works for the `INR ` fallback prefix too, which is why it is a run of
  /// non-digits rather than a hardcoded `₹`.
  static _Amount _split(String formatted) {
    var index = 0;
    while (index < formatted.length) {
      final code = formatted.codeUnitAt(index);
      final isDigit = code >= 0x30 && code <= 0x39;
      if (isDigit) break;
      index += 1;
    }
    return _Amount(formatted.substring(0, index), formatted.substring(index));
  }

  static Color _color(BuildContext context, MoneyTone tone) {
    final surfaces = context.ahSurfaces;
    return switch (tone) {
      MoneyTone.normal => surfaces.textPrimary,
      MoneyTone.secondary => surfaces.textSecondary,
      MoneyTone.accent => surfaces.accentPrimary,
      MoneyTone.positive => AhColors.clear,
      MoneyTone.negative => AhColors.flag,
    };
  }

  static _MoneyMetrics _metrics(MoneySize size) => switch (size) {
        MoneySize.xs => const _MoneyMetrics(13, 1.35, FontWeight.w600),
        MoneySize.sm => const _MoneyMetrics(14, 1.4, FontWeight.w600),
        MoneySize.md => const _MoneyMetrics(17, 1.35, FontWeight.w600),
        MoneySize.lg => const _MoneyMetrics(24, 1.2, FontWeight.w700),
        // The valuation reveal and the auction lot header. Capped rather than fluid because a
        // 360dp handset is the narrow case and a figure that reflows is a figure that shifts.
        MoneySize.xl => const _MoneyMetrics(32, 1.05, FontWeight.w700),
      };
}

@immutable
class _Amount {
  const _Amount(this.prefix, this.digits);

  final String prefix;
  final String digits;
}

@immutable
class _MoneyMetrics {
  const _MoneyMetrics(this.fontSize, this.height, this.weight);

  final double fontSize;
  final double height;
  final FontWeight weight;
}
