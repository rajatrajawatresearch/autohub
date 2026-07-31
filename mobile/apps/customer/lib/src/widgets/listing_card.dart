import 'package:autohub_core/autohub_core.dart';
import 'package:autohub_ui/autohub_ui.dart';
import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:go_router/go_router.dart';

import '../app/router.dart';
import '../data/app_providers.dart';
import '../i18n/customer_text.dart';
import 'remote_image.dart';

/// A search hit, a saved listing and a booking's vehicle, rendered by one card.
///
/// The card itself is `VehicleCard` from the canon, which contains no vehicle vocabulary at all —
/// no fuel type, no owner count, no price label. This wrapper supplies the three things only an app
/// can: the image widget, the word above the money, and where a tap goes.
class ListingCard extends ConsumerWidget {
  const ListingCard({required this.item, this.onTap, super.key});

  final SearchItem item;

  /// Defaults to the listing's own address, so the same card is tappable from search, from the home
  /// strip and from the garage without three call sites repeating the route.
  final VoidCallback? onTap;

  @override
  Widget build(BuildContext context, WidgetRef ref) {
    final AppStrings t = ref.watch(appStringsProvider);
    final String? cover = item.media.coverUrl;

    return VehicleCard(
      title: item.title,
      registration: item.registration.canonical,
      display: item.registration.display,
      // The plate colour is real information about the vehicle, not a theme decision. A class this
      // build has never heard of falls back to the private white rather than failing the card.
      plateClass: plateClassFromWire(item.registration.plateClass),
      onTap: onTap ?? () => context.push(Routes.listing(item.listingId)),
      priceMinor: item.priceMinor,
      priceLabel: priceLabelFor(item.intent, t),
      specs: item.specStrip,
      media: VehicleMedia(
        image: cover == null ? null : RemoteImage(url: cover),
        // Honoured whether the image has arrived or not. A card that sizes itself to whatever pixels
        // turn up moves the price on the connection where it takes longest to arrive.
        aspectRatio: item.media.displayAspectRatio,
        count: item.media.count,
      ),
      location: item.city.isEmpty ? null : item.city,
      badges: <Widget>[
        for (final String code in item.badges)
          AhBadge(label: badgeLabel(code, t), tone: AhBadgeTone.outline),
      ],
      footer: item.dealerName == null
          ? null
          : Text(
              item.dealerName!,
              style: AhText.bodySm.copyWith(color: context.ahSurfaces.textSecondary),
              maxLines: 1,
              overflow: TextOverflow.ellipsis,
            ),
    );
  }
}

/// What the money on a card means.
///
/// Exhaustive over [ListingIntent], which is one of the few closed sets this workspace compiles in:
/// an intent is a *route shape*, and a seventh one needs a screen. The compiler pointing at this
/// switch is the desired outcome. The *words*, though, are registry keys — "Asking price" becomes
/// "विक्री किंमत" without an app-store submission.
String priceLabelFor(ListingIntent intent, AppStrings t) => switch (intent) {
      ListingIntent.sale => t(CustomerText.priceSale),
      ListingIntent.auction => t(CustomerText.priceAuction),
      ListingIntent.rental => t(CustomerText.priceRental),
      ListingIntent.lease => t(CustomerText.priceLease),
      ListingIntent.attachment => t(CustomerText.priceAttachment),
      ListingIntent.exchange => t(CustomerText.priceExchange),
    };

/// A badge code's word.
///
/// `inspected` and `verified_rc` are codes the server sends; the words are translations. There is
/// no map of badge codes in this binary — an unknown code renders as its own code with the
/// underscores knocked out, which is ugly and immediately diagnosable, and is strictly better than
/// dropping a trust signal the server thought was worth sending.
String badgeLabel(String code, AppStrings t) {
  final String key = 'badge.$code';
  return t.has(key) ? t(key) : code.replaceAll('_', ' ');
}
