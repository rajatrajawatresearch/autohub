import 'dart:io';

import 'package:drift/drift.dart';
import 'package:drift/native.dart';
import 'package:path/path.dart' as p;
import 'package:path_provider/path_provider.dart';
import 'package:sqlite3_flutter_libs/sqlite3_flutter_libs.dart';

import 'ah_database.dart';

/// The on-device database file.
///
/// [LazyDatabase] defers the whole thing — `path_provider`, the old-Android workaround, opening the
/// file — until the first query. That is what lets the Riverpod provider be synchronous, which in
/// turn is what stops the app shell from having to await a database before it can paint. A splash
/// screen blocked on SQLite is a splash screen that is visible on a cheap handset.
QueryExecutor ahDatabaseFile({String fileName = 'autohub.sqlite'}) {
  return LazyDatabase(() async {
    // Android below 6 ships an ancient system sqlite3 that drift's generated SQL will not run
    // against. This relinks to the bundled library. Not hypothetical in this market: those handsets
    // are still in daily use and are exactly the ones this app has to work on.
    if (Platform.isAndroid) {
      await applyWorkaroundToOpenSqlite3OnOldAndroidVersions();
    }

    // Application support, not the documents directory. Documents is user-visible on iOS when the
    // app declares file sharing, and it is included in iCloud backup by default — neither is right
    // for a cache and a write queue. Application support is backed up but private, which is what a
    // queue of the user's unsent listings should be.
    final directory = await getApplicationSupportDirectory();
    final file = File(p.join(directory.path, fileName));
    return NativeDatabase.createInBackground(file);
  });
}

/// The database, ready to use. One call at app start.
AhDatabase openAhDatabase({String fileName = 'autohub.sqlite'}) =>
    AhDatabase(ahDatabaseFile(fileName: fileName));

/// A throwaway database in RAM.
///
/// For tests, and for nothing else. Everything this package exists to do — surviving a cold launch
/// on a dead network, replaying a write after a tunnel — is about data that outlives the process,
/// so an in-memory database in production would remove the feature rather than back it.
AhDatabase openInMemoryAhDatabase() => AhDatabase(NativeDatabase.memory());
