import 'package:autohub_core/autohub_core.dart';
import 'package:autohub_dealer/src/app/strings.dart';
import 'package:flutter_test/flutter_test.dart';

AhStrings _bundle(Map<String, String> resources, {String locale = 'mr-IN'}) => AhStrings(
      primary: I18nBundle(locale: locale, version: 1, resources: resources),
      fallback: const I18nBundle(
        locale: 'en-IN',
        version: 1,
        resources: <String, String>{DealerText.cancel: 'Cancel'},
      ),
    );

/// The i18n claim, made testable: a translator publishes `dealer.*` and every handset picks it up
/// on the next launch with nothing rebuilt. If the shipped map ever wins over the registry, that
/// claim is false and only a Marathi-speaking dealer would find out.
void main() {
  group('DealerStrings', () {
    test('the registry wins over the shipped en-IN copy', () {
      final t = DealerStrings(_bundle(<String, String>{DealerText.placeBid: 'बोली लगाएँ'}));

      expect(t(DealerText.placeBid), 'बोली लगाएँ');
      expect(kDealerEnIn[DealerText.placeBid], 'Place bid');
    });

    test('a key the registry has not translated falls back to the shipped copy', () {
      final t = DealerStrings(_bundle(<String, String>{DealerText.placeBid: 'बोली लगाएँ'}));

      // Not the raw key. `AhStrings` alone would render `auction.bid.confirm` on the button of the
      // most consequential sheet in the product.
      expect(t(DealerText.confirmBid), 'Confirm bid');
    });

    test('with no registry at all the app still reads as English', () {
      // First launch, in a basement, before any bundle has ever been fetched.
      const t = DealerStrings(null);

      expect(t(DealerText.placeBid), 'Place bid');
      expect(t(DealerText.errBidUnsent), contains('not placed'));
    });

    test('an unknown key returns itself rather than an empty string', () {
      const t = DealerStrings(null);

      // Ugly and immediately diagnosable, which is strictly better than a blank control: a user
      // cannot guess what a blank button does, cannot describe it to support, and cannot work
      // around it.
      expect(t('dealer.nothing.here'), 'dealer.nothing.here');
      expect(t('dealer.nothing.here'), isNotEmpty);
    });

    test('or() supplies a readable fallback for keys this build cannot enumerate', () {
      const t = DealerStrings(null);

      // Badge codes are registry rows. The console cannot ship a word for every one of them, so a
      // humanised code beats `listing.badge.verified_rc` on a card.
      expect(t.or('listing.badge.verified_rc', 'verified rc'), 'verified rc');
    });

    test('every shipped default is non-empty', () {
      // A blank entry here would defeat the whole fallback chain silently.
      for (final entry in kDealerEnIn.entries) {
        expect(entry.value, isNotEmpty, reason: 'empty copy for ${entry.key}');
      }
    });
  });
}
