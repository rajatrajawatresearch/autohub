import 'package:autohub_core/autohub_core.dart';
import 'package:flutter/material.dart';

import '../theme/ah_theme.dart';
import '../tokens/typography.dart';
import 'money_value.dart';
import 'plate_block.dart';
import 'spec_strip.dart';
import 'status_pill.dart';

/// The media slot of a [VehicleCard].
///
/// [image] is a [Widget] rather than a URL on purpose: `autohub_ui` must not know that a network
/// exists, so the app passes a `CachedNetworkImage` (or a local file, or a placeholder) and this
/// package only reserves the box it goes in.
@immutable
class VehicleMedia {
  const VehicleMedia({this.image, this.aspectRatio = 1.5, this.count});

  /// Already-decoded, already-cached. `null` renders the reserved box empty.
  final Widget? image;

  /// **Fixed, and honoured whether the image has arrived or not.**
  ///
  /// This is the entire reason the slot exists. A card that sizes itself to whatever pixels turn
  /// up moves the price the user was reaching for, on the connection where it takes longest to
  /// arrive — a 3G train journey, which is where a large share of this product's browsing
  /// actually happens.
  final double aspectRatio;

  /// Photo count. Shown as a corner stamp; a listing with 14 photos is a different proposition
  /// from one with 2, and buyers filter on it.
  final int? count;
}

/// The canonical vehicle object.
///
/// The same card renders a search result, an auction lot, a dealer's inventory row and an admin
/// moderation queue item — because in the model those are one thing: a vehicle wearing a different
/// listing intent. Anything intent-specific arrives as a slot ([badges], [footer], [priceSlot]),
/// so adding a rental intent needs no new card and no `if (intent == ...)` anywhere in here.
///
/// Note what this widget does not contain: no fuel type, no owner count, no status string, no
/// price label. Specs arrive as a [SpecStrip] built from the registry, the status arrives as a
/// category, and even the word above the money is passed in — because "Asking price" on a sale is
/// "Current bid" on an auction and "Monthly rent" on a rental, and the server already knows which.
class VehicleCard extends StatelessWidget {
  const VehicleCard({
    required this.title,
    required this.registration,
    this.display,
    this.plateClass = PlateClass.private,
    this.onTap,
    this.priceMinor,
    this.priceLabel,
    this.priceCompact = true,
    this.priceSlot,
    this.specs = const <SpecItem>[],
    this.media,
    this.badges = const <Widget>[],
    this.statusCategory,
    this.statusLabel,
    this.location,
    this.footer,
    super.key,
  });

  final String title;

  /// Canonical registration from the API — `MH12AB1234`.
  final String registration;

  /// The server's pre-grouped form, when there is one.
  final String? display;

  final PlateClass plateClass;
  final VoidCallback? onTap;

  /// Integer paise. Omit while a lot has no bids and let [priceSlot] say so.
  final int? priceMinor;

  /// What the money means here. Comes from the caller, resolved from the localisation registry.
  final String? priceLabel;

  final bool priceCompact;

  /// Replaces the money entirely — an on-request line, a countdown, a fee note.
  final Widget? priceSlot;

  final List<SpecItem> specs;
  final VehicleMedia? media;

  /// Registry-labelled badges, built by the caller so that no code in this package maps a badge
  /// code to a word.
  final List<Widget> badges;

  /// Workflow state, for the surfaces where a vehicle has one — inventory, moderation.
  final AhStateCategory? statusCategory;

  /// The state's `name` from the registry. Both this and [statusCategory] must be present for the
  /// chip to render; a category with no label would force this widget to invent the word.
  final String? statusLabel;

  final String? location;
  final Widget? footer;

  @override
  Widget build(BuildContext context) {
    final surfaces = context.ahSurfaces;
    final slot = media ?? const VehicleMedia();

    final card = DecoratedBox(
      decoration: BoxDecoration(
        color: surfaces.card,
        borderRadius: const BorderRadius.all(AhRadius.card),
        border: Border.all(color: surfaces.borderHairline),
      ),
      child: ClipRRect(
        borderRadius: const BorderRadius.all(AhRadius.card),
        child: Column(
          crossAxisAlignment: CrossAxisAlignment.start,
          mainAxisSize: MainAxisSize.min,
          children: <Widget>[
            _Media(slot: slot, statusCategory: statusCategory, statusLabel: statusLabel),
            Padding(
              padding: const EdgeInsets.all(AhSpace.lg),
              child: Column(
                crossAxisAlignment: CrossAxisAlignment.start,
                mainAxisSize: MainAxisSize.min,
                children: <Widget>[
                  Row(
                    crossAxisAlignment: CrossAxisAlignment.start,
                    children: <Widget>[
                      PlateBlock(
                        registration: registration,
                        display: display,
                        plateClass: plateClass,
                        size: PlateSize.xs,
                      ),
                      if (location != null) ...<Widget>[
                        const SizedBox(width: AhSpace.sm),
                        Expanded(
                          child: Text(
                            location!,
                            style: AhText.data.copyWith(
                              fontSize: 13,
                              color: surfaces.textSecondary,
                            ),
                            textAlign: TextAlign.right,
                            maxLines: 1,
                            overflow: TextOverflow.ellipsis,
                          ),
                        ),
                      ],
                    ],
                  ),
                  const SizedBox(height: AhSpace.md),
                  Text(
                    title,
                    style: AhText.subheading.copyWith(color: surfaces.textPrimary),
                    maxLines: 2,
                    overflow: TextOverflow.ellipsis,
                  ),
                  if (priceLabel != null || priceSlot != null || priceMinor != null) ...<Widget>[
                    const SizedBox(height: AhSpace.md),
                    if (priceLabel != null) ...<Widget>[
                      Text(
                        priceLabel!.toUpperCase(),
                        style: AhText.label.copyWith(color: surfaces.textSecondary),
                      ),
                      const SizedBox(height: AhSpace.xs),
                    ],
                    if (priceSlot != null)
                      priceSlot!
                    else if (priceMinor != null)
                      MoneyValue(
                        minorUnits: priceMinor!,
                        compact: priceCompact,
                        size: MoneySize.lg,
                      ),
                  ],
                  if (specs.isNotEmpty) ...<Widget>[
                    const SizedBox(height: AhSpace.md),
                    _HairlineTop(
                      color: surfaces.borderHairline,
                      child: SpecStrip(items: specs, size: SpecStripSize.sm),
                    ),
                  ],
                  if (badges.isNotEmpty) ...<Widget>[
                    const SizedBox(height: AhSpace.md),
                    Wrap(spacing: AhSpace.xs + 2, runSpacing: AhSpace.xs + 2, children: badges),
                  ],
                  if (footer != null) ...<Widget>[
                    const SizedBox(height: AhSpace.md),
                    _HairlineTop(color: surfaces.borderHairline, child: footer!),
                  ],
                ],
              ),
            ),
          ],
        ),
      ),
    );

    if (onTap == null) return card;

    return Semantics(
      button: true,
      label: title,
      child: Material(
        type: MaterialType.transparency,
        child: InkWell(
          onTap: onTap,
          splashFactory: NoSplash.splashFactory,
          hoverColor: Colors.transparent,
          focusColor: Colors.transparent,
          highlightColor: Colors.transparent,
          borderRadius: const BorderRadius.all(AhRadius.card),
          child: card,
        ),
      ),
    );
  }
}

class _Media extends StatelessWidget {
  const _Media({required this.slot, required this.statusCategory, required this.statusLabel});

  final VehicleMedia slot;
  final AhStateCategory? statusCategory;
  final String? statusLabel;

  @override
  Widget build(BuildContext context) {
    final surfaces = context.ahSurfaces;
    final category = statusCategory;
    final label = statusLabel;

    return DecoratedBox(
      decoration: BoxDecoration(
        color: surfaces.sunken,
        border: Border(bottom: BorderSide(color: surfaces.borderHairline)),
      ),
      child: AspectRatio(
        aspectRatio: slot.aspectRatio,
        child: Stack(
          fit: StackFit.expand,
          children: <Widget>[
            if (slot.image != null) slot.image!,
            if (category != null && label != null)
              Positioned(
                left: AhSpace.sm,
                top: AhSpace.sm,
                child: StatusPill(
                  category: category,
                  label: label,
                  size: StatusPillSize.sm,
                ),
              ),
            if (slot.count != null && slot.count! > 0)
              Positioned(
                right: AhSpace.sm,
                bottom: AhSpace.sm,
                child: _PhotoCount(count: slot.count!),
              ),
          ],
        ),
      ),
    );
  }
}

/// Fixed colours, not theme colours. It sits on a photograph, and a photograph has no theme.
class _PhotoCount extends StatelessWidget {
  const _PhotoCount({required this.count});

  final int count;

  @override
  Widget build(BuildContext context) {
    return DecoratedBox(
      decoration: const BoxDecoration(
        color: Color(0xD90F1512),
        borderRadius: BorderRadius.all(AhRadius.control),
      ),
      child: Padding(
        padding: const EdgeInsets.symmetric(horizontal: AhSpace.xs + 2, vertical: 2),
        child: Text(
          '$count',
          style: AhText.data.copyWith(fontSize: 11, color: const Color(0xFFF6F5F1)),
        ),
      ),
    );
  }
}

class _HairlineTop extends StatelessWidget {
  const _HairlineTop({required this.color, required this.child});

  final Color color;
  final Widget child;

  @override
  Widget build(BuildContext context) {
    return DecoratedBox(
      decoration: BoxDecoration(border: Border(top: BorderSide(color: color))),
      child: Padding(
        padding: const EdgeInsets.only(top: AhSpace.md),
        child: child,
      ),
    );
  }
}
