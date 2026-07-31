import 'package:autohub_core/src/values/parse_result.dart';

/// An ISO 4217 currency and its minor-unit scale.
///
/// AutoHub is INR-only today. The type exists anyway because the roadmap has an export/import
/// marketplace on it, and retrofitting a currency column into a ledger that assumed one is a
/// migration nobody survives cheerfully. Carrying the code costs a field now.
final class Currency {
  const Currency._(this.code, this.decimals);

  /// ISO 4217 alphabetic code, uppercase.
  final String code;

  /// Digits after the decimal separator. 2 for INR — a rupee is a hundred paise.
  final int decimals;

  static const Currency inr = Currency._('INR', 2);
  static const Currency usd = Currency._('USD', 2);
  static const Currency aed = Currency._('AED', 2);

  static const List<Currency> known = <Currency>[inr, usd, aed];

  /// 100 for INR. Derived rather than stored so [decimals] stays the single source of truth.
  int get minorUnitsPerMajor {
    var scale = 1;
    for (var i = 0; i < decimals; i++) {
      scale *= 10;
    }
    return scale;
  }

  /// `null` for a code this build does not know.
  ///
  /// Null rather than a throw because this runs at the decode seam on a phone: a response carrying
  /// a currency added after the app shipped must degrade to "cannot do arithmetic on this", not
  /// take down the screen. The caller decides — a display surface falls back to the ISO code, a
  /// payment surface refuses.
  static Currency? tryFromCode(String code) {
    final upper = code.toUpperCase();
    for (final candidate in known) {
      if (candidate.code == upper) return candidate;
    }
    return null;
  }

  /// Throws when the code is unknown. Use at composition points where a wrong currency is worse
  /// than a crash — anything that moves money.
  static Currency fromCode(String code) =>
      tryFromCode(code) ??
      (throw ArgumentError.value(code, 'code', 'Unsupported currency.'));

  @override
  bool operator ==(Object other) => other is Currency && other.code == code;

  @override
  int get hashCode => code.hashCode;

  @override
  String toString() => code;
}

/// How a division that does not come out exactly resolves.
///
/// Named and explicit at every call site, because the alternative is that the rounding decision is
/// an accident of whichever operator someone reached for, and nobody can re-derive a fee six months
/// later.
enum Rounding {
  /// Half rounds to the nearest even minor unit. The default, and the only one safe for fees.
  ///
  /// Always-away-from-zero quietly transfers real money to the platform over a million
  /// transactions. Auditors notice; regulators do not enjoy it.
  halfEven,

  /// Half rounds away from zero — `MidpointRounding.AwayFromZero`. Correct where a statute or a
  /// counterparty's system says so, and nowhere else.
  halfAwayFromZero,

  /// Toward negative infinity.
  floor,

  /// Toward positive infinity.
  ceiling,
}

/// An exact rate applied to money — a 2% auction commission, 18% GST — held as an integer fraction.
///
/// The wire sends `0.02`, which arrives in Dart as a binary `double` that is not exactly two
/// hundredths. Multiplying paise by that double and rounding is how two systems end up a paisa
/// apart on the same commission. So the double is converted to an exact fraction *once*, at the
/// boundary, from its shortest decimal representation — and every subsequent operation is integer
/// arithmetic. There is no floating point anywhere in the money path.
final class Rate {
  const Rate._(this.numerator, this.denominator);

  /// Signed.
  final int numerator;

  /// Always positive.
  final int denominator;

  static const Rate zero = Rate._(0, 1);
  static const Rate one = Rate._(1, 1);

  /// Largest denominator accepted. `1e15` keeps `minorUnits * numerator` inside a 64-bit int for
  /// any amount this marketplace will ever see, and a rate needing more precision than fifteen
  /// decimal places is an authoring mistake, not a business rule.
  static const int _maxDenominator = 1000000000000000;

  static final RegExp _decimalPattern =
      RegExp(r'^([+-]?)(\d*)(?:\.(\d*))?(?:[eE]([+-]?\d+))?$');

  /// Reduced to lowest terms so equal rates compare equal and the multiplication stays small.
  factory Rate.ratio(int numerator, int denominator) {
    if (denominator == 0) {
      throw ArgumentError.value(denominator, 'denominator', 'A rate cannot divide by zero.');
    }
    var top = numerator;
    var bottom = denominator;
    if (bottom < 0) {
      top = -top;
      bottom = -bottom;
    }
    final divisor = _gcd(top.abs(), bottom);
    return Rate._(top ~/ divisor, bottom ~/ divisor);
  }

  /// `Rate.percent(18)` is 18%, exactly 9/50 — not `0.18`.
  factory Rate.percent(int percent) => Rate.ratio(percent, 100);

  /// Basis points. `Rate.basisPoints(250)` is 2.5%.
  factory Rate.basisPoints(int basisPoints) => Rate.ratio(basisPoints, 10000);

  /// Converts a JSON number from a pricing quote into an exact fraction.
  ///
  /// Goes via `toString()` deliberately: Dart prints the shortest decimal that round-trips, so
  /// `0.02` prints `"0.02"` and becomes exactly 1/50. Reading the double's binary expansion instead
  /// would yield 720575940379279/36028797018963968, which is the same number to a physicist and a
  /// different one to an accountant.
  factory Rate.fromWire(num value) {
    if (value is int) return Rate.ratio(value, 1);
    if (!value.isFinite) {
      throw ArgumentError.value(value, 'value', 'A rate must be a finite number.');
    }
    return Rate.parse(value.toString());
  }

  factory Rate.parse(String decimal) {
    final text = decimal.trim();
    final match = _decimalPattern.firstMatch(text);
    final whole = match?.group(2) ?? '';
    final fraction = match?.group(3) ?? '';
    if (match == null || (whole.isEmpty && fraction.isEmpty)) {
      throw FormatException('Not a decimal rate.', decimal);
    }

    final sign = match.group(1) == '-' ? -1 : 1;
    final exponent = int.parse(match.group(4) ?? '0');
    final digits = int.parse('${whole.isEmpty ? '0' : whole}$fraction');

    // `scale` is the power of ten the digits must be divided by: the fractional digit count, less
    // whatever the exponent already shifted.
    final scale = fraction.length - exponent;
    if (scale.abs() > 15) {
      throw ArgumentError.value(decimal, 'decimal', 'Rate needs more precision than money has.');
    }

    if (scale <= 0) return Rate.ratio(sign * digits * _pow10(-scale), 1);

    final denominator = _pow10(scale);
    if (denominator > _maxDenominator) {
      throw ArgumentError.value(decimal, 'decimal', 'Rate needs more precision than money has.');
    }
    return Rate.ratio(sign * digits, denominator);
  }

  /// Lossy, and for display only — never feed this back into money arithmetic.
  double get asFraction => numerator / denominator;

  static int _pow10(int exponent) {
    var value = 1;
    for (var i = 0; i < exponent; i++) {
      value *= 10;
    }
    return value;
  }

  static int _gcd(int a, int b) {
    var left = a;
    var right = b;
    while (right != 0) {
      final next = left % right;
      left = right;
      right = next;
    }
    return left == 0 ? 1 : left;
  }

  @override
  bool operator ==(Object other) =>
      other is Rate && other.numerator == numerator && other.denominator == denominator;

  @override
  int get hashCode => Object.hash(numerator, denominator);

  @override
  String toString() => '$numerator/$denominator';
}

/// An exact amount of money, held in minor units (paise for INR).
///
/// Never a `double`, never rupees-as-a-fraction. A 2% commission on ₹8,47,350 is ₹16,947.00
/// exactly; the moment a fractional paisa is representable, two systems disagree about it and the
/// month-end reconciliation stops balancing.
///
/// Ported from `backend/src/AutoHub.SharedKernel/Money/Money.cs`, including the allocation
/// remainder rule, because the phone and the server must agree on the split of a payment down to
/// the paisa or the dealer's statement will not tie out to the app's.
///
/// This type deliberately has no formatting. Rendering `₹8.47L` depends on the viewer's locale and
/// surface and lives in `format/indian_format.dart`; the domain neither knows nor cares.
final class Money implements Comparable<Money> {
  const Money.fromMinorUnits(this.minorUnits, [this.currency = Currency.inr]);

  /// Paise for INR. Signed — a refund and a credit note are negative.
  final int minorUnits;

  final Currency currency;

  static const Money zeroInr = Money.fromMinorUnits(0);

  static Money zero(Currency currency) => Money.fromMinorUnits(0, currency);

  /// Whole rupees. Exact by construction, which is why the parameter is an `int`: there is no
  /// representable-or-not question to answer and therefore no rounding decision hidden in a
  /// constructor.
  factory Money.fromRupees(int rupees) => Money.fromMinorUnits(rupees * 100, Currency.inr);

  factory Money.fromMajorUnits(int majorUnits, Currency currency) =>
      Money.fromMinorUnits(majorUnits * currency.minorUnitsPerMajor, currency);

  /// Parses a major-unit string as a user typed it: `847350`, `8,47,350`, `100.50`.
  ///
  /// Fails rather than rounds when the input carries more precision than the currency has.
  /// `100.005` is not a rupee amount, and silently making it `₹100.01` is how a half-paisa reaches
  /// the ledger and only surfaces at close. The C# constructor throws here; on a phone the same
  /// decision arrives as a field error, because the caller is a form control and the user can fix
  /// it.
  static ParseResult<Money> tryParseMajor(String? input, [Currency currency = Currency.inr]) {
    // Indian grouping commas and any flavour of space are how the number reaches a text field.
    final text = (input ?? '').replaceAll(RegExp(r'[,\s]'), '');
    if (text.isEmpty) {
      return const ParseInvalid<Money>('money.required', 'Enter an amount.');
    }

    final match = RegExp(r'^([+-]?)(\d*)(?:\.(\d*))?$').firstMatch(text);
    final whole = match?.group(2) ?? '';
    final fraction = match?.group(3) ?? '';
    if (match == null || (whole.isEmpty && fraction.isEmpty)) {
      return const ParseInvalid<Money>('money.malformed', 'Enter an amount in numbers.');
    }

    if (fraction.length > currency.decimals) {
      return ParseInvalid<Money>(
        'money.not_representable',
        '${currency.code} amounts carry at most ${currency.decimals} decimal places.',
      );
    }

    final padded = fraction.padRight(currency.decimals, '0');
    final digits = int.tryParse('${whole.isEmpty ? '0' : whole}$padded');
    if (digits == null) {
      return const ParseInvalid<Money>('money.malformed', 'That amount is too large.');
    }

    final sign = match.group(1) == '-' ? -1 : 1;
    return ParseOk<Money>(Money.fromMinorUnits(sign * digits, currency));
  }

  bool get isZero => minorUnits == 0;

  bool get isPositive => minorUnits > 0;

  bool get isNegative => minorUnits < 0;

  Money operator +(Money other) {
    _ensureSameCurrency(this, other);
    return Money.fromMinorUnits(minorUnits + other.minorUnits, currency);
  }

  Money operator -(Money other) {
    _ensureSameCurrency(this, other);
    return Money.fromMinorUnits(minorUnits - other.minorUnits, currency);
  }

  Money operator -() => Money.fromMinorUnits(-minorUnits, currency);

  Money operator *(int factor) => Money.fromMinorUnits(minorUnits * factor, currency);

  /// Applies a rate with an explicit rounding mode.
  ///
  /// Banker's rounding by default because it does not systematically favour one party.
  Money multiplyByRate(Rate rate, {Rounding rounding = Rounding.halfEven}) => Money.fromMinorUnits(
        _divideRounded(minorUnits * rate.numerator, rate.denominator, rounding),
        currency,
      );

  /// Splits an amount by ratio, handing the remainder out one minor unit at a time so the parts
  /// always sum back to the whole.
  ///
  /// This is the function that stops ₹100.00 split three ways from becoming ₹99.99. It matters
  /// wherever one payment is divided between seller, platform commission and GST — and it matters
  /// that the phone splits it exactly as the server does, or a dealer's payout screen disagrees
  /// with their statement by a paisa and they stop trusting both.
  ///
  /// Distribution is sequential rather than largest-ratio-first: largest-first biases the winner,
  /// and sequential is the convention an accountant can re-derive from the inputs.
  List<Money> allocate(List<int> ratios) {
    if (ratios.isEmpty) {
      throw ArgumentError.value(ratios, 'ratios', 'Allocation needs at least one ratio.');
    }

    var total = 0;
    for (final ratio in ratios) {
      if (ratio < 0) {
        throw ArgumentError.value(ratios, 'ratios', 'A ratio cannot be negative.');
      }
      total += ratio;
    }
    if (total == 0) {
      throw ArgumentError.value(ratios, 'ratios', 'Ratios must not sum to zero.');
    }

    final shares = List<int>.filled(ratios.length, 0);
    var remainder = minorUnits;
    for (var i = 0; i < ratios.length; i++) {
      final share = minorUnits * ratios[i] ~/ total;
      shares[i] = share;
      remainder -= share;
    }

    for (var i = 0; remainder != 0; i = (i + 1) % ratios.length) {
      final step = remainder > 0 ? 1 : -1;
      shares[i] += step;
      remainder -= step;
    }

    return List<Money>.unmodifiable(
      shares.map((share) => Money.fromMinorUnits(share, currency)),
    );
  }

  Money clamp({Money? floor, Money? cap}) {
    var value = this;
    if (floor != null) {
      _ensureSameCurrency(value, floor);
      if (value.minorUnits < floor.minorUnits) value = floor;
    }
    if (cap != null) {
      _ensureSameCurrency(value, cap);
      if (value.minorUnits > cap.minorUnits) value = cap;
    }
    return value;
  }

  @override
  int compareTo(Money other) {
    _ensureSameCurrency(this, other);
    return minorUnits.compareTo(other.minorUnits);
  }

  bool operator <(Money other) => compareTo(other) < 0;

  bool operator >(Money other) => compareTo(other) > 0;

  bool operator <=(Money other) => compareTo(other) <= 0;

  bool operator >=(Money other) => compareTo(other) >= 0;

  @override
  bool operator ==(Object other) =>
      other is Money && other.minorUnits == minorUnits && other.currency == currency;

  @override
  int get hashCode => Object.hash(minorUnits, currency);

  @override
  String toString() => '${currency.code} $minorUnits';

  static void _ensureSameCurrency(Money left, Money right) {
    if (left.currency != right.currency) {
      throw StateError('Cannot combine ${left.currency.code} with ${right.currency.code}.');
    }
  }
}

/// Integer division with the rounding decision made explicitly.
///
/// Written out rather than reached for via `double` division and `.round()`: `(5 * 0.5).round()` is
/// 3 in Dart because `round` goes away from zero at the half, and half-away-from-zero applied to
/// every commission line is a systematic transfer to whoever wrote the code.
int _divideRounded(int numerator, int denominator, Rounding rounding) {
  assert(denominator > 0, 'A rate denominator is normalised positive by Rate.ratio.');

  final quotient = numerator ~/ denominator;
  final remainder = numerator.remainder(denominator);
  if (remainder == 0) return quotient;

  // `~/` truncates toward zero, so the correction always moves away from zero.
  final step = numerator < 0 ? -1 : 1;
  final twiceRemainder = 2 * remainder.abs();

  return switch (rounding) {
    Rounding.floor => numerator < 0 ? quotient - 1 : quotient,
    Rounding.ceiling => numerator < 0 ? quotient : quotient + 1,
    Rounding.halfAwayFromZero =>
      twiceRemainder >= denominator ? quotient + step : quotient,
    Rounding.halfEven => switch (twiceRemainder.compareTo(denominator)) {
        > 0 => quotient + step,
        < 0 => quotient,
        _ => quotient.isEven ? quotient : quotient + step,
      },
  };
}
