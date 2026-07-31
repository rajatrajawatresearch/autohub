import 'package:autohub_dealer/src/data/models/deal.dart';
import 'package:autohub_dealer/src/features/wallet/wallet_screen.dart';
import 'package:flutter_test/flutter_test.dart';

import 'fixtures.dart';

DealSummary _deal({required String category, required int amountMinor, bool isTerminal = false}) =>
    DealSummary.fromJson(
      testDeal(category: category, amountMinor: amountMinor, isTerminal: isTerminal).json,
    );

/// A wallet that overstates what a dealer is owed produces a complaint at settlement; one that
/// understates produces a call. Both are the same defect, so the split is tested against the
/// workflow *category* rather than against state codes this binary could never enumerate.
void main() {
  group('WalletBalances', () {
    test('splits settled from in flight by category, in integer paise', () {
      final balances = WalletBalances.of(<DealSummary>[
        _deal(category: 'succeeded', amountMinor: 84500000, isTerminal: true),
        _deal(category: 'succeeded', amountMinor: 15500000, isTerminal: true),
        _deal(category: 'in_progress', amountMinor: 42000000),
        _deal(category: 'awaiting_action', amountMinor: 8000000),
        _deal(category: 'draft', amountMinor: 1000000),
      ]);

      expect(balances.settledMinor, 100000000);
      expect(balances.settledCount, 2);
      expect(balances.inFlightMinor, 51000000);
      expect(balances.inFlightCount, 3);
    });

    test('a failed or cancelled deal counts as neither', () {
      final balances = WalletBalances.of(<DealSummary>[
        _deal(category: 'succeeded', amountMinor: 50000000, isTerminal: true),
        _deal(category: 'failed', amountMinor: 99900000, isTerminal: true),
        _deal(category: 'cancelled', amountMinor: 88800000, isTerminal: true),
      ]);

      // Neither settled nor coming. Folding a withdrawn deal into "in flight" is how a dealer plans
      // against money that is never arriving.
      expect(balances.settledMinor, 50000000);
      expect(balances.inFlightMinor, 0);
      expect(balances.inFlightCount, 0);
    });

    test('an unknown category degrades to draft and lands in flight, not in settled', () {
      // A category this build has never heard of decodes to `draft` — the one category that
      // promises nothing. It must never be counted as money already received.
      final balances = WalletBalances.of(<DealSummary>[
        _deal(category: 'quantum_superposition', amountMinor: 12345600),
      ]);

      expect(balances.settledMinor, 0);
      expect(balances.inFlightMinor, 12345600);
    });

    test('an empty ledger is zero, not null', () {
      final balances = WalletBalances.of(const <DealSummary>[]);
      expect(balances.settledMinor, 0);
      expect(balances.inFlightMinor, 0);
    });
  });
}
