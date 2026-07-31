/// Every location in the dealer console, named once.
///
/// Paths rather than a generated typed-route API, because the alternative needs `go_router_builder`
/// and this binary carries no code generator. What matters is that no screen types a URL: a route
/// spelled `/inventory/$id` in one place and `/inventory/${id}` in another is a deep link that
/// works from the app and 404s from a push notification.
///
/// The paths are the product's deep-link surface. `/auction/{lotId}` is what an outbid notification
/// opens, and it must land on that lot rather than on the home screen — which is the reason
/// `docs/10-mobile-architecture.md` §5 chose a declarative router at all.
abstract final class DealerRoutes {
  static const String dashboard = '/';

  static const String inventory = '/inventory';

  /// A vehicle in stock. Deep-linked from a moderation notification.
  static String stock(String listingId) => '$inventory/${Uri.encodeComponent(listingId)}';

  static const String auction = '/auction';

  /// One lot. The destination of an outbid push, and the only screen that places bids.
  static String lot(String lotId) => '$auction/${Uri.encodeComponent(lotId)}';

  static const String leads = '/leads';

  static const String wallet = '/wallet';

  /// The tab order, and the order the bottom bar renders in.
  static const List<String> tabs = <String>[dashboard, inventory, auction, leads, wallet];
}
