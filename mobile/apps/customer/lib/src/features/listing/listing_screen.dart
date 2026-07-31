import 'dart:async';

import 'package:autohub_core/autohub_core.dart' hide WorkflowHistoryEntry;
import 'package:autohub_core/autohub_core.dart' as core show WorkflowHistoryEntry;
import 'package:autohub_ui/autohub_ui.dart';
import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:go_router/go_router.dart';

import '../../app/error_surface.dart';
import '../../app/fault.dart';
import '../../app/router.dart';
import '../../data/app_providers.dart';
import '../../data/listing.dart';
import '../../i18n/customer_text.dart';
import '../../widgets/listing_card.dart';
import 'gallery.dart';
import 'offer_sheet.dart';

/// One vehicle, in full.
///
/// The page is an RC card scrolled: the plate at the top at [PlateSize.lg], a gallery with fixed
/// slots, a `SpecStrip` of what the registry says matters, a `FieldBlock` grid per spec group, the
/// seller, and the listing's own state history. Everything on it — which specs, which groups, which
/// badges, which actions — arrives as data.
class ListingScreen extends ConsumerWidget {
  const ListingScreen({required this.listingId, super.key});

  final String listingId;

  @override
  Widget build(BuildContext context, WidgetRef ref) {
    final AppStrings t = ref.watch(appStringsProvider);
    final AsyncValue<ListingDetail> listing = ref.watch(listingProvider(listingId));

    return Scaffold(
      appBar: AppBar(
        backgroundColor: context.ahSurfaces.page,
        surfaceTintColor: Colors.transparent,
        elevation: 0,
        leading: BackButton(onPressed: () => _leave(context)),
      ),
      body: listing.when(
        loading: () => const _DetailSkeleton(),
        error: (Object error, StackTrace _) => Padding(
          padding: const EdgeInsets.all(AhSpace.lg),
          child: AppErrorView(
            fault: AppFault.of(error, t),
            onRetry: () async => ref.invalidate(listingProvider(listingId)),
            secondary: AhButton(
              label: t(CustomerText.navBuy),
              variant: AhButtonVariant.outline,
              onPressed: () => context.go(Routes.buy),
            ),
          ),
        ),
        data: (ListingDetail detail) => _Detail(detail: detail),
      ),
      bottomNavigationBar: listing.valueOrNull == null
          ? null
          : _ActionBar(detail: listing.value!),
    );
  }

  /// Back, or home if this screen was the deep link that opened the app.
  ///
  /// A push notification can land a user here with nothing beneath them, and a back arrow that does
  /// nothing is the fastest way to make an app feel broken.
  static void _leave(BuildContext context) {
    if (context.canPop()) {
      context.pop();
    } else {
      context.go(Routes.buy);
    }
  }
}

class _Detail extends ConsumerWidget {
  const _Detail({required this.detail});

  final ListingDetail detail;

  @override
  Widget build(BuildContext context, WidgetRef ref) {
    final AhSurfaces surfaces = context.ahSurfaces;
    final AppStrings t = ref.watch(appStringsProvider);
    final WorkflowSnapshot? workflow = detail.workflow;

    return ListView(
      padding: const EdgeInsets.fromLTRB(AhSpace.lg, 0, AhSpace.lg, AhSpace.xxl),
      children: <Widget>[
        Wrap(
          spacing: AhSpace.sm,
          runSpacing: AhSpace.sm,
          crossAxisAlignment: WrapCrossAlignment.center,
          children: <Widget>[
            // The plate at its largest. On a phone this earns its size: the user is frequently
            // standing next to the car, checking one against the other.
            PlateBlock(
              registration: detail.registration.canonical,
              display: detail.registration.display,
              plateClass: plateClassFromWire(detail.registration.plateClass),
              size: PlateSize.lg,
            ),
            if (workflow != null)
              // Tone from the registry's `category`, never from the state code. Inserting "Legal
              // hold" into the listing workflow renders correctly on a handset that shipped months
              // before it existed.
              StatusPill(
                category: workflow.currentState.category,
                label: workflow.currentState.name,
              ),
            for (final String code in detail.badges)
              AhBadge(label: badgeLabel(code, t), tone: AhBadgeTone.outline),
          ],
        ),
        const SizedBox(height: AhSpace.lg),
        Text(detail.title, style: AhText.displayLg.copyWith(color: surfaces.textPrimary)),
        const SizedBox(height: AhSpace.xs),
        Text(
          <String>[
            if ((detail.locality ?? '').isNotEmpty) detail.locality!,
            if (detail.city.isNotEmpty) detail.city,
          ].join(' · '),
          style: AhText.bodySm.copyWith(color: surfaces.textSecondary),
        ),

        const SizedBox(height: AhSpace.lg),
        Gallery(
          assets: detail.gallery,
          cover: detail.media.coverUrl,
          aspectRatio: detail.media.displayAspectRatio,
          emptyLabel: t(CustomerText.listingPhotosEmpty),
        ),

        if (detail.specStrip.isNotEmpty) ...<Widget>[
          const SizedBox(height: AhSpace.xl),
          Container(
            decoration: BoxDecoration(
              border: Border.symmetric(
                horizontal: BorderSide(color: surfaces.borderHairline),
              ),
            ),
            padding: const EdgeInsets.symmetric(vertical: AhSpace.lg),
            child: SpecStrip(items: detail.specStrip),
          ),
        ],

        if ((detail.description ?? '').isNotEmpty) ...<Widget>[
          const SizedBox(height: AhSpace.xl),
          SectionHeading(title: t(CustomerText.listingAbout), size: SectionHeadingSize.sm),
          const SizedBox(height: AhSpace.sm),
          Text(detail.description!, style: AhText.body.copyWith(color: surfaces.textPrimary)),
        ],

        // The RC particulars. Groups, their order and their contents all come from the taxonomy
        // registry's `displayGroup`; there is no list of vehicle attributes in this file.
        for (final SpecGroup group in detail.specGroups) ...<Widget>[
          const SizedBox(height: AhSpace.xl),
          SectionHeading(title: group.label, size: SectionHeadingSize.sm, rule: true),
          const SizedBox(height: AhSpace.lg),
          Wrap(
            spacing: AhSpace.xl,
            runSpacing: AhSpace.lg,
            children: <Widget>[
              for (final SpecItem item in group.items)
                SizedBox(
                  width: 150,
                  child: FieldBlock(label: item.label, value: item.value, mono: true),
                ),
            ],
          ),
        ],

        if (detail.seller != null) ...<Widget>[
          const SizedBox(height: AhSpace.xl),
          _SellerCard(seller: detail.seller!),
        ],

        if (workflow != null) ...<Widget>[
          const SizedBox(height: AhSpace.xl),
          SectionHeading(
            title: t(CustomerText.listingHistory),
            size: SectionHeadingSize.sm,
            rule: true,
          ),
          const SizedBox(height: AhSpace.lg),
          _History(workflow: workflow),
        ],

        const SizedBox(height: AhSpace.xl),
        Text(
          t(CustomerText.listingEscrow),
          style: AhText.bodySm.copyWith(color: surfaces.textSecondary),
        ),
      ],
    );
  }
}

/* ── Seller ────────────────────────────────────────────────────────────────── */

class _SellerCard extends ConsumerWidget {
  const _SellerCard({required this.seller});

  final Seller seller;

  @override
  Widget build(BuildContext context, WidgetRef ref) {
    final AhSurfaces surfaces = context.ahSurfaces;
    final AppStrings t = ref.watch(appStringsProvider);

    return DecoratedBox(
      decoration: BoxDecoration(
        color: surfaces.card,
        borderRadius: const BorderRadius.all(AhRadius.card),
        border: Border.all(color: surfaces.borderHairline),
      ),
      child: Padding(
        padding: const EdgeInsets.all(AhSpace.lg),
        child: Column(
          crossAxisAlignment: CrossAxisAlignment.start,
          children: <Widget>[
            Text(
              t(CustomerText.listingSeller).toUpperCase(),
              style: AhText.label.copyWith(color: surfaces.textSecondary),
            ),
            const SizedBox(height: AhSpace.sm),
            Row(
              children: <Widget>[
                Expanded(
                  child: Text(
                    seller.displayName,
                    style: AhText.subheading.copyWith(color: surfaces.textPrimary),
                  ),
                ),
                if (seller.isVerified)
                  AhBadge(
                    label: t(CustomerText.listingSellerDealer),
                    tone: AhBadgeTone.clear,
                    size: AhBadgeSize.sm,
                  ),
              ],
            ),
            Text(
              <String>[
                // `type` is a data value from the server; the two words for it are registry keys.
                seller.type == 'dealer'
                    ? t(CustomerText.listingSellerDealer)
                    : t(CustomerText.listingSellerIndividual),
                if ((seller.city ?? '').isNotEmpty) seller.city!,
              ].join(' · '),
              style: AhText.bodySm.copyWith(color: surfaces.textSecondary),
            ),
            if (seller.listingsCount != null || seller.responseTimeMinutes != null) ...<Widget>[
              const SizedBox(height: AhSpace.lg),
              Wrap(
                spacing: AhSpace.xxl,
                runSpacing: AhSpace.md,
                children: <Widget>[
                  if (seller.listingsCount != null)
                    FieldBlock(
                      label: t(CustomerText.listingSellerListings),
                      value: formatIndianNumber(seller.listingsCount!),
                      mono: true,
                    ),
                  if (seller.responseTimeMinutes != null)
                    FieldBlock(
                      label: t(CustomerText.listingSellerReplies),
                      value: '${seller.responseTimeMinutes} min',
                      mono: true,
                    ),
                ],
              ),
            ],
          ],
        ),
      ),
    );
  }
}

/* ── History ───────────────────────────────────────────────────────────────── */

/// The listing's state history, named by the workflow registry.
///
/// The snapshot carries state *codes*; the names live in the workflow definition. Fetching the
/// definition alongside is what keeps this page from hardcoding "Under review" next to
/// `pending_review` — and the timeline degrades to the raw code rather than crashing when a
/// migrated history references a state the current version no longer declares.
class _History extends ConsumerWidget {
  const _History({required this.workflow});

  final WorkflowSnapshot workflow;

  @override
  Widget build(BuildContext context, WidgetRef ref) {
    final AppStrings t = ref.watch(appStringsProvider);
    final AsyncValue<WorkflowDefinition> definition =
        ref.watch(workflowDefinitionProvider(workflow.workflowKey));

    return WorkflowTimeline(
      states: <WorkflowStateDefinition>[
        for (final WorkflowState state in definition.valueOrNull?.states ?? const <WorkflowState>[])
          WorkflowStateDefinition(
            code: state.code,
            name: state.name,
            category: state.category,
          ),
      ],
      entries: <WorkflowHistoryEntry>[
        for (final core.WorkflowHistoryEntry entry in workflow.history)
          WorkflowHistoryEntry(
            to: entry.to,
            from: entry.from,
            actionCode: entry.actionCode,
            // The server sends ISO-8601 UTC. An unparseable stamp would be a contract violation;
            // falling back to the entry's own position rather than throwing keeps an audit trail
            // readable, and an audit trail that crashes is worse than one with an odd date in it.
            occurredAt: DateTime.tryParse(entry.occurredAt)?.toUtc() ?? DateTime.now().toUtc(),
            actor: entry.actor,
            timeInPreviousState: entry.timeInPreviousState,
          ),
      ],
      empty: Text(
        t(CustomerText.listingHistoryEmpty),
        style: AhText.bodySm.copyWith(color: context.ahSurfaces.textSecondary),
      ),
    );
  }
}

/* ── The sticky action bar ─────────────────────────────────────────────────── */

/// What a buyer can do, decided by the server.
///
/// The primary control is **whatever `workflow.availableActions` marked `primary`** — labelled by
/// the server, permitted by the server, and wearing the one rationed yellow on the screen. On a
/// live listing that action is *Book test drive*; the client does not know that and must not. A
/// client that decided "live listings get a Book test drive button" is a client that disagrees with
/// the server the first time operations adds a step, on handsets that cannot be corrected without
/// an app-store release.
///
/// *Make an offer* sits beside it because it is not a workflow transition at all — it creates a
/// deal — and it is available on any priced listing.
class _ActionBar extends ConsumerWidget {
  const _ActionBar({required this.detail});

  final ListingDetail detail;

  @override
  Widget build(BuildContext context, WidgetRef ref) {
    final AhSurfaces surfaces = context.ahSurfaces;
    final AppStrings t = ref.watch(appStringsProvider);
    final ListingActionState action = ref.watch(listingActionProvider(detail.listingId));
    final ListingActionController controller =
        ref.read(listingActionProvider(detail.listingId).notifier);

    final List<AvailableAction> available =
        detail.workflow?.availableActions ?? const <AvailableAction>[];
    final AvailableAction? primary = _primaryOf(available);
    final bool busy = action is ListingActionBusy;

    return DecoratedBox(
      decoration: BoxDecoration(
        color: surfaces.card,
        border: Border(top: BorderSide(color: surfaces.borderHairline)),
      ),
      child: SafeArea(
        top: false,
        child: Padding(
          padding: const EdgeInsets.all(AhSpace.lg),
          child: Column(
            mainAxisSize: MainAxisSize.min,
            crossAxisAlignment: CrossAxisAlignment.stretch,
            children: <Widget>[
              switch (action) {
                ListingActionDone(:final String message) ||
                ListingActionQueued(:final String message) =>
                  _Outcome(message: message, onDismiss: controller.reset),
                ListingActionRefused(:final AppFault fault) =>
                  AppFaultBanner(fault: fault, onRetry: null),
                ListingActionIdle() || ListingActionBusy() => const SizedBox.shrink(),
              },
              if (action is! ListingActionDone && action is! ListingActionQueued) ...<Widget>[
                if (action is ListingActionRefused) const SizedBox(height: AhSpace.md),
                // The price above the controls rather than beside them. At 360dp a full figure and
                // two buttons do not share a line, and the figure is the thing the buyer is reading
                // when they reach for the button — squeezing it is the wrong trade.
                _Price(detail: detail),
                const SizedBox(height: AhSpace.md),
                Row(
                  children: <Widget>[
                    Expanded(
                      child: AhButton(
                        label: t(CustomerText.listingOfferCta),
                        variant: AhButtonVariant.outline,
                        block: true,
                        onPressed: busy
                            ? null
                            : () => unawaited(
                                  OfferSheet.show(
                                    context,
                                    listingId: detail.listingId,
                                    askingPriceMinor: detail.priceMinor,
                                  ),
                                ),
                      ),
                    ),
                    if (primary != null) ...<Widget>[
                      const SizedBox(width: AhSpace.sm),
                      Expanded(
                        child: AhButton(
                          // The server's own label, already localised. This screen never invents
                          // the word on its most consequential control.
                          label: primary.label,
                          variant: AhButtonVariant.action,
                          block: true,
                          onPressed: busy ? null : () => unawaited(controller.fire(primary)),
                        ),
                      ),
                    ],
                  ],
                ),
              ],
            ],
          ),
        ),
      ),
    );
  }

  /// The action the registry marked `primary`, or the first one offered.
  ///
  /// Falling back to the first is deliberate: a registry row with no emphasis set is an authoring
  /// omission, and hiding the only action a user has because of it would be the client punishing
  /// them for it.
  static AvailableAction? _primaryOf(List<AvailableAction> actions) {
    if (actions.isEmpty) return null;
    for (final AvailableAction action in actions) {
      if (action.emphasis == ActionEmphasis.primary) return action;
    }
    return actions.first;
  }
}

class _Price extends ConsumerWidget {
  const _Price({required this.detail});

  final ListingDetail detail;

  @override
  Widget build(BuildContext context, WidgetRef ref) {
    final AhSurfaces surfaces = context.ahSurfaces;
    final AppStrings t = ref.watch(appStringsProvider);
    final int? price = detail.priceMinor;

    return Column(
      crossAxisAlignment: CrossAxisAlignment.start,
      mainAxisSize: MainAxisSize.min,
      children: <Widget>[
        Text(
          priceLabelFor(detail.intent, t).toUpperCase(),
          style: AhText.label.copyWith(color: surfaces.textSecondary),
        ),
        const SizedBox(height: 2),
        if (price == null)
          Text(
            t(CustomerText.listingPriceOnRequest),
            style: AhText.subheading.copyWith(color: surfaces.textPrimary),
          )
        else
          // Not compact. `₹8.4L` is how India says a price and the wrong thing to show beside the
          // button that commits to one — a figure a buyer is about to act on is written in full.
          MoneyValue(minorUnits: price, size: MoneySize.lg, currency: detail.currency),
      ],
    );
  }
}

class _Outcome extends StatelessWidget {
  const _Outcome({required this.message, required this.onDismiss});

  final String message;
  final VoidCallback onDismiss;

  @override
  Widget build(BuildContext context) {
    final AhSurfaces surfaces = context.ahSurfaces;

    return Row(
      children: <Widget>[
        Expanded(
          child: Semantics(
            liveRegion: true,
            child: Text(
              message,
              style: AhText.body.copyWith(color: surfaces.textPrimary),
            ),
          ),
        ),
        AhButton(
          label: MaterialLocalizations.of(context).closeButtonTooltip,
          variant: AhButtonVariant.ghost,
          size: AhButtonSize.sm,
          onPressed: onDismiss,
        ),
      ],
    );
  }
}

class _DetailSkeleton extends StatelessWidget {
  const _DetailSkeleton();

  @override
  Widget build(BuildContext context) => ListView(
        padding: const EdgeInsets.all(AhSpace.lg),
        children: const <Widget>[
          AhSkeleton(width: 180, height: 40),
          SizedBox(height: AhSpace.lg),
          AhSkeleton.block(height: 220),
          SizedBox(height: AhSpace.lg),
          AhSkeleton.line(width: 260),
          SizedBox(height: AhSpace.sm),
          AhSkeleton.line(width: 180),
        ],
      );
}
