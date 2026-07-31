import 'package:autohub_core/autohub_core.dart';
import 'package:autohub_partner/src/app/partner_side.dart';
import 'package:autohub_partner/src/data/partner_providers.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:flutter_test/flutter_test.dart';

/// Which side of the market a device is on is the first thing this app asks and the last thing it
/// should get wrong.
///
/// The failure to guard against is not "the setting is lost" — that costs one tap. It is the app
/// *guessing*: an owner dropped into the operator's product sees a requirement board with none of
/// their vehicles on it and concludes their listings were deleted. So an unrecognised stored value
/// returns `null` and sends the partner back through the chooser, and the value written to disk is
/// spelled out rather than derived from a Dart identifier that somebody could rename.
void main() {
  group('PartnerSide parsing', () {
    test('round-trips through its stored form', () {
      for (final PartnerSide side in PartnerSide.values) {
        expect(PartnerSide.tryParse(side.storageValue), side);
      }
    });

    test('stored values are the wire spellings, not the enum names', () {
      // Pinned deliberately: renaming the Dart identifier must not orphan every device that has
      // already chosen, and a test is the only thing that makes that a compile-time conversation.
      expect(PartnerSide.supply.storageValue, 'supply');
      expect(PartnerSide.demand.storageValue, 'demand');
    });

    test('an unknown or missing value picks no side at all', () {
      expect(PartnerSide.tryParse(null), isNull);
      expect(PartnerSide.tryParse(''), isNull);
      // Written by a newer build and read after a downgrade.
      expect(PartnerSide.tryParse('broker'), isNull);
    });
  });

  group('PartnerSideController', () {
    late AhDatabase database;
    late ProviderContainer container;

    setUp(() {
      database = openInMemoryAhDatabase();
      container = ProviderContainer(
        overrides: <Override>[ahDatabaseProvider.overrideWithValue(database)],
      );
    });

    tearDown(() async {
      container.dispose();
      await database.close();
    });

    test('starts from what bootstrap read off disk', () {
      final ProviderContainer withStoredSide = ProviderContainer(
        overrides: <Override>[
          ahDatabaseProvider.overrideWithValue(database),
          initialPartnerSideProvider.overrideWithValue(PartnerSide.demand),
        ],
      );
      addTearDown(withStoredSide.dispose);

      expect(withStoredSide.read(partnerSideProvider), PartnerSide.demand);
    });

    test('is null until a side is chosen, and never defaults to one', () {
      expect(container.read(partnerSideProvider), isNull);
    });

    test('choosing updates state before it touches the disk, then persists', () async {
      final Future<void> pending =
          container.read(partnerSideProvider.notifier).choose(PartnerSide.supply);

      // The repaint does not wait on SQLite. On a cheap handset a chooser that did feels broken,
      // and the failure mode of a lost write is that the partner picks their side once more.
      expect(container.read(partnerSideProvider), PartnerSide.supply);

      await pending;

      final DevicePreferences preferences = container.read(devicePreferencesProvider);
      expect(await preferences.read(PartnerSide.storageKey), 'supply');
    });

    test('switching sides rewrites the stored value', () async {
      final PartnerSideController controller = container.read(partnerSideProvider.notifier);
      await controller.choose(PartnerSide.supply);
      await controller.choose(PartnerSide.demand);

      final DevicePreferences preferences = container.read(devicePreferencesProvider);
      expect(await preferences.read(PartnerSide.storageKey), 'demand');
      expect(container.read(partnerSideProvider), PartnerSide.demand);
    });
  });
}
