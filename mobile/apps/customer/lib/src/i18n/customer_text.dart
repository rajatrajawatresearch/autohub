/// The customer app's own chrome strings, and the terminal fallback for them.
///
/// Every word a user reads in this binary resolves through here, and the resolution order is the
/// contract's (`docs/contracts/registry-api.md` §6): the requested locale, then `en-IN`, then the
/// authored default below, then the key itself. **Never an empty string** — a blank button is
/// unrecoverable for a user, who can neither guess what it does nor describe it to support, while
/// `home.hero.cta` on the button is ugly and diagnosable in one screenshot.
///
/// The map at the bottom is the one place in this app where a user-facing literal is correct, for
/// the same reason `defaultFormLabels` is in `autohub_forms`: a cold first launch in a basement has
/// no i18n bundle on disk, and a home screen whose only control is unlabelled is worse than one in
/// English. Every one of these is overridden the moment `GET /platform/i18n/{locale}` answers, and
/// a content person rewording "Get valuation" ships without an app-store submission.
///
/// What is deliberately *not* here: anything a registry owns. No fuel types, no listing statuses,
/// no badge words, no facet labels, no validation sentences, no fee copy. Those arrive as data on
/// the objects that carry them.
library;

import 'package:autohub_core/autohub_core.dart';
import 'package:autohub_forms/autohub_forms.dart' show FormLabels, interpolate;
import 'package:flutter/foundation.dart';

/// The keys this binary asks for.
///
/// Constants rather than an enum, so a bundle carrying a key this build has never heard of is
/// still a valid bundle and a key this build asks for that the bundle lacks still resolves. An
/// enum here would make the app and the registry have to ship together, which is the coupling the
/// whole architecture exists to remove.
abstract final class CustomerText {
  /* ── Shell ─────────────────────────────────────────────────────────────── */
  static const String navHome = 'nav.home';
  static const String navBuy = 'nav.buy';
  static const String navSell = 'nav.sell';
  static const String navGarage = 'nav.garage';
  static const String commonBack = 'common.back';
  static const String commonCancel = 'common.cancel';

  /* ── Home ──────────────────────────────────────────────────────────────── */
  static const String heroEyebrow = 'home.hero.eyebrow';
  static const String heroTitle = 'home.hero.title';
  static const String heroPlateLabel = 'home.hero.plate_label';
  static const String heroKmsLabel = 'home.hero.kms_label';
  static const String heroKmsPlaceholder = 'home.hero.kms_placeholder';
  static const String heroCta = 'home.hero.cta';
  static const String heroWorking = 'home.hero.working';
  static const String heroReassurance = 'home.hero.reassurance';
  static const String heroReset = 'home.hero.reset';

  static const String intentBuyTitle = 'home.intent.buy.title';
  static const String intentBuyLine = 'home.intent.buy.line';
  static const String intentSellTitle = 'home.intent.sell.title';
  static const String intentSellLine = 'home.intent.sell.line';
  static const String intentRentTitle = 'home.intent.rent.title';
  static const String intentRentLine = 'home.intent.rent.line';
  static const String intentAttachTitle = 'home.intent.attach.title';
  static const String intentAttachLine = 'home.intent.attach.line';

  static const String stockEyebrow = 'home.stock.eyebrow';
  static const String stockTitle = 'home.stock.title';
  static const String stockAll = 'home.stock.all';
  static const String stockEmpty = 'home.stock.empty';

  static const String trustOwnershipLabel = 'home.trust.ownership.label';
  static const String trustOwnershipValue = 'home.trust.ownership.value';
  static const String trustOwnershipHint = 'home.trust.ownership.hint';
  static const String trustConditionLabel = 'home.trust.condition.label';
  static const String trustConditionValue = 'home.trust.condition.value';
  static const String trustConditionHint = 'home.trust.condition.hint';
  static const String trustPaymentLabel = 'home.trust.payment.label';
  static const String trustPaymentValue = 'home.trust.payment.value';
  static const String trustPaymentHint = 'home.trust.payment.hint';

  /* ── Valuation ─────────────────────────────────────────────────────────── */
  static const String valuationHeadlineFallback = 'valuation.headline_fallback';
  static const String valuationRange = 'valuation.range';
  static const String valuationConfidence = 'valuation.confidence';
  static const String valuationAsOf = 'valuation.as_of';
  static const String valuationModel = 'valuation.model';
  static const String valuationSellCta = 'valuation.sell_cta';
  static const String valuationBrowseCta = 'valuation.browse_cta';
  static const String valuationDisclaimer = 'valuation.disclaimer';
  static const String valuationSource = 'valuation.source';
  static const String valuationTitle = 'valuation.title';
  static const String valuationMissingTitle = 'valuation.missing.title';
  static const String valuationMissingBody = 'valuation.missing.body';

  /* ── Buy ───────────────────────────────────────────────────────────────── */
  static const String buyTitle = 'buy.title';
  static const String buySearchPlaceholder = 'buy.search_placeholder';
  static const String buyCount = 'buy.count';
  static const String buyFilters = 'buy.filters';
  static const String buyFiltersClear = 'buy.filters.clear';
  static const String buyFiltersApply = 'buy.filters.apply';
  static const String buySort = 'buy.sort';
  static const String buySortRelevance = 'buy.sort.relevance';
  static const String buySortNewest = 'buy.sort.newest';
  static const String buySortPriceAsc = 'buy.sort.price_asc';
  static const String buySortPriceDesc = 'buy.sort.price_desc';
  static const String buySortKmsAsc = 'buy.sort.kms_asc';
  static const String buyEmptyTitle = 'buy.empty.title';
  static const String buyEmptyBody = 'buy.empty.body';
  static const String buyEmptyUnfilteredBody = 'buy.empty.unfiltered_body';
  static const String buyMore = 'buy.more';
  static const String buyLoadingMore = 'buy.loading_more';

  /* ── What the money on a card means, per intent ────────────────────────── */
  static const String priceSale = 'price.sale';
  static const String priceAuction = 'price.auction';
  static const String priceRental = 'price.rental';
  static const String priceLease = 'price.lease';
  static const String priceAttachment = 'price.attachment';
  static const String priceExchange = 'price.exchange';

  /* ── Listing detail ────────────────────────────────────────────────────── */
  static const String listingAbout = 'listing.about';
  static const String listingHistory = 'listing.history';
  static const String listingHistoryEmpty = 'listing.history.empty';
  static const String listingPhotosEmpty = 'listing.photos.empty';
  static const String listingSeller = 'listing.seller';
  static const String listingSellerDealer = 'listing.seller.dealer';
  static const String listingSellerIndividual = 'listing.seller.individual';
  static const String listingSellerListings = 'listing.seller.listings';
  static const String listingSellerReplies = 'listing.seller.replies';
  static const String listingPriceOnRequest = 'listing.price_on_request';
  static const String listingEscrow = 'listing.escrow';
  static const String listingOfferCta = 'listing.offer.cta';
  static const String listingOfferTitle = 'listing.offer.title';
  static const String listingOfferAmount = 'listing.offer.amount';
  static const String listingOfferMessage = 'listing.offer.message';
  static const String listingOfferSubmit = 'listing.offer.submit';
  static const String listingOfferSent = 'listing.offer.sent';
  static const String listingOfferQueued = 'listing.offer.queued';
  static const String listingActionDone = 'listing.action.done';
  static const String listingNotFoundTitle = 'listing.not_found.title';
  static const String listingNotFoundBody = 'listing.not_found.body';

  /* ── Sell ──────────────────────────────────────────────────────────────── */
  static const String sellTitle = 'sell.title';
  static const String sellEyebrow = 'sell.eyebrow';
  static const String sellUnavailableTitle = 'sell.unavailable.title';
  static const String sellUnavailableBody = 'sell.unavailable.body';
  static const String sellTransportError = 'sell.transport_error';
  static const String sellDoneEyebrow = 'sell.done.eyebrow';
  static const String sellDoneTitle = 'sell.done.title';
  static const String sellDoneBody = 'sell.done.body';
  static const String sellDoneView = 'sell.done.view';
  static const String sellDoneAnother = 'sell.done.another';
  static const String sellQueuedEyebrow = 'sell.queued.eyebrow';
  static const String sellQueuedTitle = 'sell.queued.title';
  static const String sellQueuedBody = 'sell.queued.body';

  /* ── Login ─────────────────────────────────────────────────────────────── */
  static const String loginStepPhone = 'login.step.phone';
  static const String loginStepCode = 'login.step.code';
  static const String loginTitlePhone = 'login.title.phone';
  static const String loginTitleCode = 'login.title.code';
  static const String loginDescPhone = 'login.desc.phone';
  static const String loginDescCode = 'login.desc.code';
  static const String loginPhoneLabel = 'login.phone_label';
  static const String loginPhonePlaceholder = 'login.phone_placeholder';
  static const String loginSend = 'login.send';
  static const String loginSending = 'login.sending';
  static const String loginCodeLabel = 'login.code_label';
  static const String loginCodePlaceholder = 'login.code_placeholder';
  static const String loginVerify = 'login.verify';
  static const String loginVerifying = 'login.verifying';
  static const String loginChangeNumber = 'login.change_number';
  static const String loginResend = 'login.resend';
  static const String loginResendIn = 'login.resend_in';

  /* ── Garage ────────────────────────────────────────────────────────────── */
  static const String garageTitle = 'garage.title';
  static const String garageSignedOutTitle = 'garage.signed_out.title';
  static const String garageSignedOutBody = 'garage.signed_out.body';
  static const String garageSignIn = 'garage.sign_in';
  static const String garageSignOut = 'garage.sign_out';
  static const String garageVehicles = 'garage.vehicles';
  static const String garageVehiclesEmpty = 'garage.vehicles.empty';
  static const String garageListings = 'garage.listings';
  static const String garageListingsEmpty = 'garage.listings.empty';
  static const String garageBookings = 'garage.bookings';
  static const String garageBookingsEmpty = 'garage.bookings.empty';
  static const String garageQueued = 'garage.queued';

  /* ── The error surface ─────────────────────────────────────────────────── */
  static const String errorTitle = 'error.title';
  static const String errorBody = 'error.body';
  static const String errorOfflineTitle = 'error.offline.title';
  static const String errorOfflineBody = 'error.offline.body';
  static const String errorRetry = 'error.retry';
  static const String errorCopy = 'error.copy';
  static const String errorCopied = 'error.copied';
  static const String errorReference = 'error.reference';
}

/// The authored fallback bundle, in the product's voice: a knowledgeable dealer, not a salesperson.
///
/// Errors state the problem and the fix and never apologise. Empty states are instructions rather
/// than moods. Actions keep their name through the whole flow — `Get valuation` stays `Get
/// valuation`, and the working state is the same verb in the present tense.
const Map<String, String> defaultCustomerStrings = <String, String>{
  CustomerText.navHome: 'Home',
  CustomerText.navBuy: 'Buy',
  CustomerText.navSell: 'Sell',
  CustomerText.navGarage: 'Garage',
  CustomerText.commonBack: 'Back',
  CustomerText.commonCancel: 'Cancel',

  CustomerText.heroEyebrow: 'Free valuation',
  CustomerText.heroTitle: 'What is your car worth today?',
  CustomerText.heroPlateLabel: 'Registration number',
  CustomerText.heroKmsLabel: 'Kms driven — optional',
  CustomerText.heroKmsPlaceholder: '38400',
  CustomerText.heroCta: 'Get valuation',
  CustomerText.heroWorking: 'Checking RTO records',
  CustomerText.heroReassurance: 'Free · 30 seconds · no phone number needed',
  CustomerText.heroReset: 'Check another vehicle',

  CustomerText.intentBuyTitle: 'Buy',
  CustomerText.intentBuyLine: 'Inspected stock from verified dealers and owners.',
  CustomerText.intentSellTitle: 'Sell',
  CustomerText.intentSellLine: 'One form, then a dealer auction or an instant offer.',
  CustomerText.intentRentTitle: 'Rent',
  CustomerText.intentRentLine: 'Self-drive or with a driver, by the day or the month.',
  CustomerText.intentAttachTitle: 'Attach',
  CustomerText.intentAttachLine: 'Put your taxi on a fleet contract and get paid monthly.',

  CustomerText.stockEyebrow: 'Live stock',
  CustomerText.stockTitle: 'Inspected and ready to view',
  CustomerText.stockAll: 'See all',
  CustomerText.stockEmpty:
      'Stock for your city has not loaded. Pull down to retry, or change city.',

  CustomerText.trustOwnershipLabel: 'Ownership',
  CustomerText.trustOwnershipValue: 'RC verified against VAHAN',
  CustomerText.trustOwnershipHint:
      'Hypothecation, challans and blacklist status checked before a listing goes live.',
  CustomerText.trustConditionLabel: 'Condition',
  CustomerText.trustConditionValue: '200-point inspection',
  CustomerText.trustConditionHint:
      'Engine, body, tyres, electricals and an OBD scan, photographed and graded.',
  CustomerText.trustPaymentLabel: 'Payment',
  CustomerText.trustPaymentValue: 'Escrow protected',
  CustomerText.trustPaymentHint:
      'Money is released to the seller only after the transfer is filed.',

  CustomerText.valuationHeadlineFallback: 'Market value',
  CustomerText.valuationRange: 'Range',
  CustomerText.valuationConfidence: 'Confidence',
  CustomerText.valuationAsOf: 'As of',
  CustomerText.valuationModel: 'Model {version}',
  CustomerText.valuationSellCta: 'Sell at this price',
  CustomerText.valuationBrowseCta: 'Browse similar',
  CustomerText.valuationDisclaimer:
      'A valuation is an estimate from comparable sales, not an offer. '
          'The price a dealer pays depends on the inspection.',
  CustomerText.valuationSource: 'Source',
  CustomerText.valuationTitle: 'Valuation',
  CustomerText.valuationMissingTitle: 'That quote is not on this device',
  CustomerText.valuationMissingBody:
      'Quotes are kept on the phone that asked for them. Enter the registration number to get a '
          'fresh valuation — it takes thirty seconds.',

  CustomerText.buyTitle: 'Buy',
  CustomerText.buySearchPlaceholder: 'Search make, model or variant',
  CustomerText.buyCount: '{count} vehicles',
  CustomerText.buyFilters: 'Filters',
  CustomerText.buyFiltersClear: 'Clear {count}',
  CustomerText.buyFiltersApply: 'Show results',
  CustomerText.buySort: 'Sort',
  CustomerText.buySortRelevance: 'Most relevant',
  CustomerText.buySortNewest: 'Newest first',
  CustomerText.buySortPriceAsc: 'Price: low to high',
  CustomerText.buySortPriceDesc: 'Price: high to low',
  CustomerText.buySortKmsAsc: 'Kms: low to high',
  CustomerText.buyEmptyTitle: 'No vehicles match these filters',
  CustomerText.buyEmptyBody: 'Widen the price range or clear a filter to see more.',
  CustomerText.buyEmptyUnfilteredBody:
      'Nothing is listed in this city yet. Sell yours, or check another city.',
  CustomerText.buyMore: 'Show more',
  CustomerText.buyLoadingMore: 'Loading',

  CustomerText.priceSale: 'Asking price',
  CustomerText.priceAuction: 'Current bid',
  CustomerText.priceRental: 'Per day',
  CustomerText.priceLease: 'Per month',
  CustomerText.priceAttachment: 'Per month',
  CustomerText.priceExchange: 'Exchange value',

  CustomerText.listingAbout: 'About this vehicle',
  CustomerText.listingHistory: 'Listing history',
  CustomerText.listingHistoryEmpty: 'Nothing has happened to this listing yet.',
  CustomerText.listingPhotosEmpty: 'No photos yet',
  CustomerText.listingSeller: 'Seller',
  CustomerText.listingSellerDealer: 'Verified dealer',
  CustomerText.listingSellerIndividual: 'Individual owner',
  CustomerText.listingSellerListings: 'Listings',
  CustomerText.listingSellerReplies: 'Replies in',
  CustomerText.listingPriceOnRequest: 'Price on request',
  CustomerText.listingEscrow:
      'Payment is held in escrow and released to the seller only after the RC transfer is filed.',
  CustomerText.listingOfferCta: 'Make an offer',
  CustomerText.listingOfferTitle: 'Your offer',
  CustomerText.listingOfferAmount: 'Offer amount',
  CustomerText.listingOfferMessage: 'Message to the seller — optional',
  CustomerText.listingOfferSubmit: 'Send offer',
  CustomerText.listingOfferSent: 'Offer sent.',
  CustomerText.listingOfferQueued: 'Offer saved. It goes out when you are back on a network.',
  CustomerText.listingActionDone: 'Done. The seller has been told.',
  CustomerText.listingNotFoundTitle: 'This listing is not available',
  CustomerText.listingNotFoundBody:
      'It may have sold or been withdrawn. Browse what is live in your city instead.',

  CustomerText.sellTitle: 'Sell your vehicle',
  CustomerText.sellEyebrow: 'Sell',
  CustomerText.sellUnavailableTitle: 'The sell form has not reached this phone',
  CustomerText.sellUnavailableBody:
      'The form is fetched once and then works offline. Get on a network for a moment and open '
          'this again — nothing you entered has been lost.',
  CustomerText.sellTransportError:
      'That did not save. Check your connection and submit again — your answers are kept.',
  CustomerText.sellDoneEyebrow: 'Submitted',
  CustomerText.sellDoneTitle: 'Your vehicle is with our team',
  CustomerText.sellDoneBody:
      'We check the RC against VAHAN and review the photos. You will hear from us within four hours.',
  CustomerText.sellDoneView: 'See your listing',
  CustomerText.sellDoneAnother: 'Value another vehicle',
  CustomerText.sellQueuedEyebrow: 'Saved',
  CustomerText.sellQueuedTitle: 'Held until you are back on a network',
  CustomerText.sellQueuedBody:
      'Your answers and photos are on this phone. They go up by themselves the moment there is '
          'signal — you can close the app.',

  CustomerText.loginStepPhone: 'Step 1 of 2',
  CustomerText.loginStepCode: 'Step 2 of 2',
  CustomerText.loginTitlePhone: 'Sign in',
  CustomerText.loginTitleCode: 'Enter the code',
  CustomerText.loginDescPhone: 'We will text you a code. No password to remember.',
  CustomerText.loginDescCode: 'We sent a 6-digit code to {phone}.',
  CustomerText.loginPhoneLabel: 'Mobile number',
  CustomerText.loginPhonePlaceholder: '98765 43210',
  CustomerText.loginSend: 'Send code',
  CustomerText.loginSending: 'Sending',
  CustomerText.loginCodeLabel: '6-digit code',
  CustomerText.loginCodePlaceholder: '482915',
  CustomerText.loginVerify: 'Sign in',
  CustomerText.loginVerifying: 'Checking',
  CustomerText.loginChangeNumber: 'Change number',
  CustomerText.loginResend: 'Resend code',
  CustomerText.loginResendIn: 'Resend in {seconds}s',

  CustomerText.garageTitle: 'Garage',
  CustomerText.garageSignedOutTitle: 'Sign in to see your garage',
  CustomerText.garageSignedOutBody:
      'Your valuations, listings and bookings live behind your mobile number. No password.',
  CustomerText.garageSignIn: 'Sign in',
  CustomerText.garageSignOut: 'Sign out',
  CustomerText.garageVehicles: 'Your vehicles',
  CustomerText.garageVehiclesEmpty:
      'No vehicles yet. Type a registration number on the home screen to add one.',
  CustomerText.garageListings: 'Your listings',
  CustomerText.garageListingsEmpty: 'Nothing listed yet. Selling starts with the sell form.',
  CustomerText.garageBookings: 'Your bookings',
  CustomerText.garageBookingsEmpty:
      'No bookings yet. Book a test drive from any listing you are looking at.',
  CustomerText.garageQueued: '{count} waiting to go up',

  CustomerText.errorTitle: 'That did not go through',
  CustomerText.errorBody: 'Try again. If it keeps happening, send support the reference below.',
  CustomerText.errorOfflineTitle: 'No connection',
  CustomerText.errorOfflineBody:
      'AutoHub cannot be reached. What is already on this phone still works.',
  CustomerText.errorRetry: 'Try again',
  CustomerText.errorCopy: 'Copy reference',
  CustomerText.errorCopied: 'Reference copied',
  CustomerText.errorReference: 'Reference',
};

/// Resolves a chrome string. Callable, so a call site reads `t(CustomerText.heroCta)`.
///
/// Deliberately the same shape as `FormLabels` in `autohub_forms` and as the i18n registry's own
/// lookup, so there is one way to ask for a word in this workspace rather than three.
@immutable
final class AppStrings {
  const AppStrings([this.registry]);

  /// The bundles from `GET /platform/i18n/{locale}`, or `null` before they have arrived.
  ///
  /// Null is a working state and not a failure: the app renders in the authored fallback until the
  /// registry answers, and swaps to the user's language on the next frame after it does.
  final AhStrings? registry;

  /// The locale actually in force. `en-IN` until a bundle has landed.
  String get locale => registry?.locale ?? AhStrings.fallbackLocale;

  String call(String key, [Map<String, Object?>? vars]) {
    final AhStrings? bundle = registry;
    final String template = (bundle != null && bundle.has(key))
        ? bundle(key)
        : (defaultCustomerStrings[key] ?? key);
    return interpolate(template, vars);
  }

  /// True when [key] resolves to authored text rather than to itself.
  ///
  /// The error surface needs this and nothing else does: a failure is rendered from
  /// `error.{machine code}` **when the registry has authored one**, and from the server's own
  /// `detail` when it has not. Without this check the fallback chain would render the literal
  /// string `error.catalog.vahan.record_not_found` at a user, which is the one failure mode the
  /// contract's "never an empty string" rule is trying to avoid the mirror image of.
  bool has(String key) => registry?.has(key) ?? defaultCustomerStrings.containsKey(key);

  /// The renderer chrome for `SchemaForm`, from the same bundle.
  ///
  /// Composed here rather than in the sell screen because the fallback ordering has to be identical
  /// for the app's words and the form's — a sell flow whose buttons are Marathi and whose "Back" is
  /// English is one screen speaking two languages.
  FormLabels get formLabels {
    final AhStrings? bundle = registry;
    if (bundle == null) return const FormLabels();
    return FormLabels(<String, String>{
      ...?bundle.fallback?.resources,
      ...bundle.primary.resources,
    });
  }
}
