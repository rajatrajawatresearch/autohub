import 'package:autohub_core/src/format/indian_format.dart';
import 'package:flutter_test/flutter_test.dart';

/// The lakh/crore boundaries, against `packages/ui/src/lib/format.ts`.
///
/// The formatter itself lives in `src/format/` rather than in `src/values/` — it is the one
/// implementation in this package and this file is the only place its boundaries are pinned. A
/// second Dart formatter would be the exact defect the web comment warns about: two surfaces that
/// disagree about `₹8.4L`, discovered by a customer holding both.
///
/// Every expectation below is the literal string the web renders for the same `minorUnits`.
void main() {
  group('compact — how the figure is spoken', () {
    const cases = <int, String>{
      9450000: '₹94,500', // below a lakh: written in full, because that is how it is said
      54900000: '₹5.49L',
      84000000: '₹8.4L',
      1200000000: '₹1.2Cr',
      100000000: '₹10L',
      1000000000: '₹1Cr',
      0: '₹0',
    };

    cases.forEach((minorUnits, expected) {
      test('$minorUnits paise reads $expected', () {
        expect(formatIndianMoney(minorUnits, compact: true), expected);
      });
    });

    test('a figure that rounds across the lakh boundary is said as a lakh', () {
      // ₹99,999.60 rounds up to ₹1,00,000. Printing the grouped form there would be the only place
      // in the app where the compact renderer emits six digits.
      expect(formatIndianMoney(9999960, compact: true), '₹1L');
    });

    test('99.9995L is promoted to a crore rather than printed as 100L', () {
      // Nobody says "a hundred lakh".
      expect(formatIndianMoney(999995000, compact: true), '₹1Cr');
    });

    test('a negative amount keeps its sign outside the symbol', () {
      expect(formatIndianMoney(-84000000, compact: true), '-₹8.4L');
    });
  });

  group('full — how the figure is written', () {
    const cases = <int, String>{
      84735000: '₹8,47,350', // the last three digits, then pairs
      1000000000: '₹1,00,00,000',
      9450000: '₹94,500',
      100000: '₹1,000',
      99900: '₹999',
      0: '₹0',
    };

    cases.forEach((minorUnits, expected) {
      test('$minorUnits paise is written $expected', () {
        expect(formatIndianMoney(minorUnits), expected);
      });
    });

    test('paise appear only when they exist', () {
      // A vehicle price never carries them; a ledger line might, and dropping them there is a
      // reconciliation the dealer cannot perform.
      expect(formatIndianMoney(123450), '₹1,234.50');
      expect(formatIndianMoney(123405), '₹1,234.05');
      expect(formatIndianMoney(123400), '₹1,234');
    });

    test('an unmapped currency degrades to its ISO code rather than to a wrong symbol', () {
      expect(formatIndianMoney(100000, currency: 'AED'), 'AED 1,000');
    });
  });

  group('numbers and units', () {
    test('digit grouping is Indian, not western', () {
      expect(formatIndianNumber(847350), '8,47,350');
      expect(formatIndianNumber(38400), '38,400');
      expect(formatIndianNumber(999), '999');
      expect(formatIndianNumber(-1245), '-1,245');
    });

    test('a fraction keeps up to two places with trailing zeros trimmed', () {
      expect(formatIndianNumber(1245.5), '1,245.5');
    });

    test('the unit travels with the odometer reading', () {
      expect(formatKms(38400), '38,400 km');
    });

    test('a genuinely absent number is a dash, never a silent zero', () {
      expect(formatIndianNumber(double.nan), ahAbsent);
      expect(formatKms(double.infinity), ahAbsent);
    });
  });
}
