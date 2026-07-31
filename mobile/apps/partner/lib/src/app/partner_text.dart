import 'package:autohub_core/autohub_core.dart';
import 'package:flutter/foundation.dart';

/// Keys for this app's own chrome.
///
/// Everything a user reads that describes *their data* — a state name, a document label, a fee
/// line, a deny reason — arrives resolved from the server and is never in this file. What is here
/// is the furniture: tab names, section headings, the sentence that explains what a trial period
/// is. They are shaped like the localisation registry's keys
/// (`GET /platform/i18n/{locale}?namespace=web`) so a bundle overrides any of them without a
/// release.
///
/// `String` constants rather than an enum, for the same reason `FormLabelKeys` is: [PartnerText]
/// takes any key, so a bundle carrying a key this build has never seen still resolves, and a key
/// this build asks for that the bundle lacks falls through rather than crashing.
abstract final class PartnerKeys {
  static const String appTitle = 'partner.app.title';

  static const String sideEyebrow = 'partner.side.eyebrow';
  static const String sideTitle = 'partner.side.title';
  static const String sideBody = 'partner.side.body';
  static const String sideSupplyTitle = 'partner.side.supply.title';
  static const String sideSupplyBody = 'partner.side.supply.body';
  static const String sideSupplyCta = 'partner.side.supply.cta';
  static const String sideDemandTitle = 'partner.side.demand.title';
  static const String sideDemandBody = 'partner.side.demand.body';
  static const String sideDemandCta = 'partner.side.demand.cta';

  static const String navVehicles = 'partner.nav.vehicles';
  static const String navRequirements = 'partner.nav.requirements';
  static const String navMatches = 'partner.nav.matches';
  static const String navContracts = 'partner.nav.contracts';
  static const String navSettlement = 'partner.nav.settlement';
  static const String navSettings = 'partner.nav.settings';

  static const String vehiclesTitle = 'partner.vehicles.title';
  static const String vehiclesEmptyTitle = 'partner.vehicles.empty.title';
  static const String vehiclesEmptyBody = 'partner.vehicles.empty.body';
  static const String vehiclesAdd = 'partner.vehicles.add';
  static const String vehiclesAddTitle = 'partner.vehicles.add.title';

  static const String complianceTitle = 'partner.compliance.title';
  static const String complianceBody = 'partner.compliance.body';
  static const String complianceValidTill = 'partner.compliance.valid_till';
  static const String complianceNoExpiry = 'partner.compliance.no_expiry';

  static const String requirementsTitle = 'partner.requirements.title';
  static const String requirementsEmptyTitle = 'partner.requirements.empty.title';
  static const String requirementsEmptyBody = 'partner.requirements.empty.body';
  static const String requirementsAdd = 'partner.requirements.add';
  static const String requirementsAddTitle = 'partner.requirements.add.title';
  static const String requirementsFilled = 'partner.requirements.filled';

  static const String matchesTitle = 'partner.matches.title';
  static const String matchesEmptyTitle = 'partner.matches.empty.title';
  static const String matchesEmptyBody = 'partner.matches.empty.body';
  static const String matchesScore = 'partner.matches.score';
  static const String matchesWhy = 'partner.matches.why';
  static const String matchesAccept = 'partner.matches.accept';
  static const String matchesCommissionTitle = 'partner.matches.commission.title';
  static const String matchesCommissionBody = 'partner.matches.commission.body';
  static const String matchesCommissionMissing = 'partner.matches.commission.missing';
  static const String matchesIneligibleTitle = 'partner.matches.ineligible.title';
  static const String matchesIneligibleBody = 'partner.matches.ineligible.body';

  static const String contractsTitle = 'partner.contracts.title';
  static const String contractsEmptyTitle = 'partner.contracts.empty.title';
  static const String contractsEmptyBody = 'partner.contracts.empty.body';
  static const String contractsLifecycle = 'partner.contracts.lifecycle';
  static const String contractsHistoryEmpty = 'partner.contracts.history.empty';
  static const String contractsRate = 'partner.contracts.rate';
  static const String contractsCycle = 'partner.contracts.cycle';
  static const String contractsStarts = 'partner.contracts.starts';
  static const String contractsEnds = 'partner.contracts.ends';
  static const String contractsCounterparty = 'partner.contracts.counterparty';

  static const String settlementTitle = 'partner.settlement.title';
  static const String settlementEmptyTitle = 'partner.settlement.empty.title';
  static const String settlementEmptyBody = 'partner.settlement.empty.body';
  static const String settlementGross = 'partner.settlement.gross';
  static const String settlementNet = 'partner.settlement.net';
  static const String settlementPayoutOn = 'partner.settlement.payout_on';
  static const String settlementPayoutPending = 'partner.settlement.payout_pending';
  static const String settlementLines = 'partner.settlement.lines';
  static const String settlementInvoice = 'partner.settlement.invoice';
  static const String settlementUnreconciled = 'partner.settlement.unreconciled';
  static const String settlementPeriod = 'partner.settlement.period';

  static const String settingsTitle = 'partner.settings.title';
  static const String settingsSide = 'partner.settings.side';
  static const String settingsSideSwitch = 'partner.settings.side.switch';
  static const String settingsTheme = 'partner.settings.theme';
  static const String settingsQueue = 'partner.settings.queue';
  static const String settingsQueueBody = 'partner.settings.queue.body';

  static const String actionRetry = 'partner.action.retry';
  static const String actionDismiss = 'partner.action.dismiss';
  static const String actionSubmit = 'partner.action.submit';

  static const String errorTitle = 'partner.error.title';
  static const String errorOffline = 'partner.error.offline';
  static const String errorSupportRef = 'partner.error.support_ref';
  static const String queuedTitle = 'partner.queued.title';
  static const String queuedBody = 'partner.queued.body';
}

/// The terminal fallback bundle, in the product's voice.
///
/// This is the one place in the app where a user-facing literal is correct, and only because the
/// contract demands a terminal fallback: a first cold launch in a basement has no i18n bundle on
/// disk, and a settlement screen whose headings are blank is worse than one whose headings are in
/// English. `AhStrings` resolves locale → `en-IN` → the key; this sits underneath as a fourth step
/// so that the key itself is what a user sees only when this build genuinely has nothing to say.
///
/// Every one of these is overridden the moment `GET /platform/i18n/{locale}` answers. None of them
/// names a fuel type, a state, a document, a fee or a percentage.
const Map<String, String> defaultPartnerLabels = <String, String>{
  PartnerKeys.appTitle: 'AutoHub Partner',
  PartnerKeys.sideEyebrow: 'Taxi Attachment Centre',
  PartnerKeys.sideTitle: 'Which side are you on?',
  PartnerKeys.sideBody: 'You can switch later in Settings.',
  PartnerKeys.sideSupplyTitle: 'I have a vehicle',
  PartnerKeys.sideSupplyBody:
      'List your taxi with its permit, driver and routes. Get matched with operators who need it.',
  PartnerKeys.sideSupplyCta: 'List my vehicle',
  PartnerKeys.sideDemandTitle: 'I need vehicles',
  PartnerKeys.sideDemandBody:
      'Post the count, duration and rate you need. See vehicles whose permits and driver documents '
          'are already checked.',
  PartnerKeys.sideDemandCta: 'Post a requirement',
  PartnerKeys.navVehicles: 'Vehicles',
  PartnerKeys.navRequirements: 'Requirements',
  PartnerKeys.navMatches: 'Matches',
  PartnerKeys.navContracts: 'Contracts',
  PartnerKeys.navSettlement: 'Settlement',
  PartnerKeys.navSettings: 'Settings',
  PartnerKeys.vehiclesTitle: 'My vehicles',
  PartnerKeys.vehiclesEmptyTitle: 'No vehicles listed yet',
  PartnerKeys.vehiclesEmptyBody:
      'List a vehicle with its permit, fitness and driver documents to start matching.',
  PartnerKeys.vehiclesAdd: 'Add vehicle',
  PartnerKeys.vehiclesAddTitle: 'Add a vehicle',
  PartnerKeys.complianceTitle: 'Compliance',
  PartnerKeys.complianceBody:
      'An expired document pauses the contract and blocks the payout, not the earning. Renew before '
          'the date shown.',
  PartnerKeys.complianceValidTill: 'Valid till',
  PartnerKeys.complianceNoExpiry: 'No expiry recorded',
  PartnerKeys.requirementsTitle: 'Requirements',
  PartnerKeys.requirementsEmptyTitle: 'No requirements posted',
  PartnerKeys.requirementsEmptyBody:
      'Post what you need — count, route, duty pattern and rate — and matching starts immediately.',
  PartnerKeys.requirementsAdd: 'Post requirement',
  PartnerKeys.requirementsAddTitle: 'Post a requirement',
  PartnerKeys.requirementsFilled: 'Filled',
  PartnerKeys.matchesTitle: 'Matches',
  PartnerKeys.matchesEmptyTitle: 'No matches yet',
  PartnerKeys.matchesEmptyBody: 'Matching re-runs whenever either side changes its terms.',
  PartnerKeys.matchesScore: 'Match',
  PartnerKeys.matchesWhy: 'Why this matched',
  PartnerKeys.matchesAccept: 'Accept match',
  PartnerKeys.matchesCommissionTitle: 'Commission on this match',
  PartnerKeys.matchesCommissionBody: 'Charged every cycle, for as long as the contract runs.',
  PartnerKeys.matchesCommissionMissing:
      'The commission for this match has not been quoted yet. Accepting is disabled until it is.',
  PartnerKeys.matchesIneligibleTitle: 'Not matched, and why',
  PartnerKeys.matchesIneligibleBody:
      'Every requirement that filtered you out, with the reason. Fixing the reason makes you '
          'eligible on the next sweep.',
  PartnerKeys.contractsTitle: 'Contracts',
  PartnerKeys.contractsEmptyTitle: 'No contracts yet',
  PartnerKeys.contractsEmptyBody: 'Accept a match and the contract opens here.',
  PartnerKeys.contractsLifecycle: 'Lifecycle',
  PartnerKeys.contractsHistoryEmpty: 'Nothing has happened to this contract yet.',
  PartnerKeys.contractsRate: 'Rate',
  PartnerKeys.contractsCycle: 'Cycle',
  PartnerKeys.contractsStarts: 'Starts',
  PartnerKeys.contractsEnds: 'Ends',
  PartnerKeys.contractsCounterparty: 'Counterparty',
  PartnerKeys.settlementTitle: 'Settlement',
  PartnerKeys.settlementEmptyTitle: 'No cycles closed yet',
  PartnerKeys.settlementEmptyBody:
      'The first statement appears once the opening cycle closes and reconciles.',
  PartnerKeys.settlementGross: 'Agreed gross',
  PartnerKeys.settlementNet: 'Net payable',
  PartnerKeys.settlementPayoutOn: 'Payout on',
  PartnerKeys.settlementPayoutPending: 'Payout date is set when the operator funds the cycle.',
  PartnerKeys.settlementLines: 'Statement',
  PartnerKeys.settlementInvoice: 'Invoice',
  PartnerKeys.settlementPeriod: 'Period',
  PartnerKeys.settlementUnreconciled:
      'This statement does not add up to the payable figure. Do not act on it — send the reference '
          'below to support.',
  PartnerKeys.settingsTitle: 'Settings',
  PartnerKeys.settingsSide: 'Your side of the market',
  PartnerKeys.settingsSideSwitch: 'Switch side',
  PartnerKeys.settingsTheme: 'Theme',
  PartnerKeys.settingsQueue: 'Waiting to send',
  PartnerKeys.settingsQueueBody:
      'Anything you submitted without signal is held here and sent when the network returns.',
  PartnerKeys.actionRetry: 'Retry',
  PartnerKeys.actionDismiss: 'Dismiss',
  PartnerKeys.actionSubmit: 'Submit',
  PartnerKeys.errorTitle: 'That did not go through',
  PartnerKeys.errorOffline: 'No connection. Nothing was sent.',
  PartnerKeys.errorSupportRef: 'Quote this to support',
  PartnerKeys.queuedTitle: 'Held until you are back online',
  PartnerKeys.queuedBody: 'It will be sent once, with the same reference, as soon as there is signal.',
};

/// Resolves a chrome string: the i18n registry, then `en-IN`, then [defaultPartnerLabels], then the
/// key itself.
///
/// Callable so call sites read `t(PartnerKeys.navMatches)`, matching the shape of the registry's
/// own lookup and of `FormLabels` in `autohub_forms`.
///
/// There is deliberately no placeholder interpolation. The contract defines no substitution syntax,
/// and a client that invents one is a client whose `{count}` renders literally the day a translator
/// writes `{गिनती}`. Values that vary are composed at the call site, where their type is known —
/// which is why a countdown in this app is a mono `22d` beside a registry-named status rather than
/// a sentence this file assembled.
@immutable
final class PartnerText {
  const PartnerText(this._strings);

  /// `null` before the localisation registry has answered on this device. The defaults below carry
  /// the screen until it does; they are not a substitute for it.
  final AhStrings? _strings;

  String call(String key) {
    final AhStrings? strings = _strings;
    if (strings != null && strings.has(key)) return strings(key);
    return defaultPartnerLabels[key] ?? key;
  }

  /// The resolved locale, for the diagnostics line on the settings screen. `null` until the bundle
  /// lands, which is itself the useful signal.
  String? get locale => _strings?.locale;
}
