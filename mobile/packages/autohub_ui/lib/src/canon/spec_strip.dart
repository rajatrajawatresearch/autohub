import 'package:autohub_core/autohub_core.dart';
import 'package:flutter/widgets.dart';

import '../theme/ah_theme.dart';
import '../tokens/typography.dart';

enum SpecStripSize { sm, md }

/// A hairline-divided row of mono data — Year · Kms · Fuel · Owners.
///
/// The order and the content come from the registry (`specStrip` on a search result), never from
/// this widget: a category that exposes `payload_kg` and `axle_config` renders through exactly
/// this strip with no code change. That is the whole claim of the architecture, expressed as a
/// widget that cannot name a single vehicle attribute.
///
/// It wraps rather than scrolls. At 360dp — the width an inspector actually holds — a four-cell
/// strip does not fit on one line, and a horizontally-scrolling row hides data behind a gesture
/// nobody knows is available. Wrapping costs a line of height and hides nothing.
///
/// The leading divider is dropped on the first cell only, not on the first cell of each wrapped
/// line: the rule then reads as a continuous grid across the wrap instead of as a broken table.
class SpecStrip extends StatelessWidget {
  const SpecStrip({
    required this.items,
    this.size = SpecStripSize.md,
    this.labels = true,
    super.key,
  });

  /// [SpecItem] is `autohub_core`'s contract model, decoded straight off the search response.
  /// Its `label` and `value` are already resolved by the server — nothing here maps `kms_driven`
  /// to "Kms driven" or `1` to "1st owner", because that mapping is the taxonomy registry's job
  /// and doing it in a widget is how a product ends up unable to launch a truck category without
  /// an app store release.
  final List<SpecItem> items;

  final SpecStripSize size;

  /// Labels above values is the house style. Drop them only where the row is already captioned —
  /// a dense inventory cell, a PDF footer.
  final bool labels;

  @override
  Widget build(BuildContext context) {
    if (items.isEmpty) return const SizedBox.shrink();

    final surfaces = context.ahSurfaces;
    final compact = size == SpecStripSize.sm;
    final padX = compact ? AhSpace.sm : AhSpace.md;

    return Wrap(
      runSpacing: AhSpace.sm,
      children: <Widget>[
        for (var index = 0; index < items.length; index += 1)
          DecoratedBox(
            decoration: BoxDecoration(
              border: index == 0
                  ? null
                  : Border(left: BorderSide(color: surfaces.borderHairline)),
            ),
            child: Padding(
              padding: EdgeInsets.only(left: index == 0 ? 0 : padX, right: padX),
              child: Column(
                crossAxisAlignment: CrossAxisAlignment.start,
                mainAxisSize: MainAxisSize.min,
                children: <Widget>[
                  if (labels) ...<Widget>[
                    Text(
                      items[index].label.toUpperCase(),
                      style: AhText.label.copyWith(color: surfaces.textSecondary),
                    ),
                    SizedBox(height: compact ? 2 : AhSpace.xs),
                  ],
                  Text(
                    items[index].value,
                    style: AhText.data.copyWith(
                      fontSize: compact ? 13 : 14,
                      color: surfaces.textPrimary,
                    ),
                    maxLines: 1,
                    overflow: TextOverflow.ellipsis,
                  ),
                ],
              ),
            ),
          ),
      ],
    );
  }
}
