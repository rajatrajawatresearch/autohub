/// Indian number formatting — the edge of the money pipeline.
///
/// Money travels through this product as integer minor units (paise) and is never a `double` and
/// never formatted by the server. It becomes a string exactly here, at the moment of render.
///
/// Ported line-for-line from `packages/ui/src/lib/format.ts`. The port is deliberate rather than
/// re-derived: a phone that groups `847350` differently from the laptop the same user opened the
/// same listing on is two products, and the disagreement would only ever be found by a customer.
///
/// The grouping is not cosmetic. `₹8,47,350` is how India *writes* money and `₹8.4L` is how India
/// *says* it; western `₹847,350` or `₹847.4K` marks the product as foreign on the first screen.
///
/// Everything below is deterministic and ICU-free on purpose. `intl`'s `NumberFormat` depends on
/// loaded locale data, and a formatter that silently falls back to `en-US` when that data is
/// missing is a bug that only appears on the devices where the data failed to load.
library;

/// Rendered wherever a number is genuinely absent. A missing figure is never a silent zero.
const String ahAbsent = '—';

const int _paisePerRupee = 100;
const int _rupeesPerLakh = 100000;
const int _rupeesPerCrore = 10000000;

/// Currency symbols are a locale invariant, not a business rule — there is no registry for what
/// the rupee sign looks like. Anything unmapped falls back to its ISO code, so it is never wrong,
/// only verbose.
const Map<String, String> _currencySymbol = <String, String>{'INR': '₹'};

final RegExp _indianGroups = RegExp(r'\B(?=(\d{2})+(?!\d))');
final RegExp _trailingZeros = RegExp(r'\.?0+$');

/// Indian digit grouping: the last three digits, then pairs.
///
/// `847350` → `8,47,350`; `10000000` → `1,00,00,000`.
String _groupIndian(String digits) {
  if (digits.length <= 3) return digits;
  final last3 = digits.substring(digits.length - 3);
  final head = digits.substring(0, digits.length - 3);
  return '${head.replaceAllMapped(_indianGroups, (_) => ',')},$last3';
}

/// `8.40` → `8.4`, `1.00` → `1`, `5.49` → `5.49`.
///
/// Two decimals is the ceiling for a spoken figure — nobody says "eight point four three seven
/// lakh", so precision past that is noise on the one number the user is comparing.
String _trimScaled(double value) =>
    value.toStringAsFixed(2).replaceFirst(_trailingZeros, '');

/// `1,24,500`.
///
/// Integers group cleanly; a fractional value keeps up to two decimals with trailing zeros
/// trimmed, so `1245.5` reads `1,245.5` rather than `1,245.50`.
String formatIndianNumber(num value) {
  if (!value.isFinite) return ahAbsent;

  final negative = value < 0;
  final abs = value.abs();
  final whole = abs.floor();
  final fraction = abs - whole;

  var out = _groupIndian('$whole');
  if (fraction > 0) {
    out += fraction.toStringAsFixed(2).substring(1).replaceFirst(_trailingZeros, '');
  }

  return negative ? '-$out' : out;
}

/// `38,400 km`.
///
/// The unit travels with the number. An odometer reading without `km` is a rumour, and the same
/// attribute on a commercial category may be declared in a different unit tomorrow — which is why
/// the unit is passed in from the taxonomy everywhere except here, where it is the function's name.
String formatKms(num kms) {
  if (!kms.isFinite) return ahAbsent;
  return '${formatIndianNumber(kms.round())} km';
}

/// The single most-seen number in AutoHub.
///
/// [minorUnits] is paise, as an `int`. Never rupees, never a `double` off the wire — binary
/// floating point cannot represent `0.1` and a marketplace that accumulates commission in
/// `double` reconciles short by the end of the month.
///
/// [compact] gives `₹8.4L` / `₹1.2Cr` / `₹94,500` — how the figure is spoken, for cards and
/// tables. Leave it off on a contract, an invoice or anything a user will be held to: `₹8.4L`
/// is not a number you can sign.
String formatIndianMoney(int minorUnits, {bool compact = false, String currency = 'INR'}) {
  final symbol = _currencySymbol[currency] ?? '$currency ';
  final abs = minorUnits.abs();
  final sign = minorUnits < 0 ? '-' : '';

  if (compact) {
    final rupees = abs / _paisePerRupee;

    if (rupees >= _rupeesPerCrore) {
      return '$sign$symbol${_trimScaled(rupees / _rupeesPerCrore)}Cr';
    }

    if (rupees >= _rupeesPerLakh) {
      final lakhs = (rupees / _rupeesPerLakh * 100).round() / 100;
      // 99.9995L rounds to 100L, which nobody says. Promote it.
      return lakhs >= 100
          ? '$sign$symbol${_trimScaled(lakhs / 100)}Cr'
          : '$sign$symbol${_trimScaled(lakhs)}L';
    }

    final wholeRupees = rupees.round();
    // ₹99,999.60 rounds up across the lakh boundary; say it as a lakh, don't print ₹1,00,000.
    return wholeRupees >= _rupeesPerLakh
        ? '$sign$symbol${_trimScaled(wholeRupees / _rupeesPerLakh)}L'
        : '$sign$symbol${_groupIndian('$wholeRupees')}';
  }

  final rupees = abs ~/ _paisePerRupee;
  final remainder = abs % _paisePerRupee;
  final grouped = _groupIndian('$rupees');

  // Paise appear only when they exist. A vehicle price never carries them; a ledger line might.
  return remainder == 0
      ? '$sign$symbol$grouped'
      : '$sign$symbol$grouped.${'$remainder'.padLeft(2, '0')}';
}
