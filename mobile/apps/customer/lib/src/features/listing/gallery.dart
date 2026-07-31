import 'package:autohub_core/autohub_core.dart';
import 'package:autohub_ui/autohub_ui.dart';
import 'package:flutter/material.dart';

import '../../widgets/remote_image.dart';

/// The vehicle's photographs, in slots whose size is decided before a byte arrives.
///
/// Photography is the heaviest thing on this page and a listing whose images reflow as they load is
/// a listing people bounce off. Every slot here — the main frame and every thumbnail — has a fixed
/// aspect ratio taken from the asset's own `aspectRatio`, so the price and the action bar stay
/// exactly where the user reached for them on a 3G train journey.
class Gallery extends StatefulWidget {
  const Gallery({
    required this.assets,
    required this.emptyLabel,
    this.cover,
    this.aspectRatio = 1.5,
    super.key,
  });

  final List<MediaAsset> assets;

  /// The cover from the card projection, used when the detail response has no gallery yet.
  final String? cover;

  final double aspectRatio;

  /// Authored text for a listing with no photographs — a real state, not an error: a dealer's bulk
  /// upload lands the rows before the images.
  final String emptyLabel;

  @override
  State<Gallery> createState() => _GalleryState();
}

class _GalleryState extends State<Gallery> {
  int _index = 0;

  List<MediaAsset> get _images {
    if (widget.assets.isNotEmpty) return widget.assets;
    final String? cover = widget.cover;
    if (cover == null || cover.isEmpty) return const <MediaAsset>[];
    return <MediaAsset>[MediaAsset(id: 'cover', url: cover, aspectRatio: widget.aspectRatio)];
  }

  @override
  Widget build(BuildContext context) {
    final AhSurfaces surfaces = context.ahSurfaces;
    final List<MediaAsset> images = _images;

    if (images.isEmpty) {
      return AspectRatio(
        aspectRatio: widget.aspectRatio,
        child: DecoratedBox(
          decoration: BoxDecoration(
            color: surfaces.sunken,
            border: Border.all(color: surfaces.borderHairline),
          ),
          child: Center(
            child: Text(
              widget.emptyLabel,
              style: AhText.bodySm.copyWith(color: surfaces.textSecondary),
            ),
          ),
        ),
      );
    }

    final int active = _index.clamp(0, images.length - 1);

    return Column(
      crossAxisAlignment: CrossAxisAlignment.start,
      children: <Widget>[
        AspectRatio(
          aspectRatio: images[active].aspectRatio,
          child: DecoratedBox(
            decoration: BoxDecoration(
              color: surfaces.sunken,
              border: Border.all(color: surfaces.borderHairline),
            ),
            child: Stack(
              fit: StackFit.expand,
              children: <Widget>[
                RemoteImage(url: images[active].url),
                if (images.length > 1)
                  Positioned(
                    right: AhSpace.sm,
                    bottom: AhSpace.sm,
                    child: _Counter(index: active + 1, total: images.length),
                  ),
              ],
            ),
          ),
        ),
        if (images.length > 1) ...<Widget>[
          const SizedBox(height: AhSpace.sm),
          SizedBox(
            height: 64,
            child: ListView.separated(
              scrollDirection: Axis.horizontal,
              itemCount: images.length,
              separatorBuilder: (BuildContext context, int index) =>
                  const SizedBox(width: AhSpace.sm),
              itemBuilder: (BuildContext context, int index) => Semantics(
                button: true,
                selected: index == active,
                label: images[index].caption ?? '${index + 1}',
                excludeSemantics: true,
                child: GestureDetector(
                  onTap: () => setState(() => _index = index),
                  child: DecoratedBox(
                    decoration: BoxDecoration(
                      border: Border.all(
                        color: index == active
                            ? surfaces.accentPrimary
                            : surfaces.borderHairline,
                        width: index == active ? 2 : 1,
                      ),
                    ),
                    child: SizedBox(
                      width: 96,
                      child: RemoteImage(url: images[index].url),
                    ),
                  ),
                ),
              ),
            ),
          ),
        ],
      ],
    );
  }
}

/// Fixed colours, not theme colours. It sits on a photograph, and a photograph has no theme.
class _Counter extends StatelessWidget {
  const _Counter({required this.index, required this.total});

  final int index;
  final int total;

  @override
  Widget build(BuildContext context) => DecoratedBox(
        decoration: const BoxDecoration(
          color: Color(0xD90F1512),
          borderRadius: BorderRadius.all(AhRadius.control),
        ),
        child: Padding(
          padding: const EdgeInsets.symmetric(horizontal: AhSpace.sm, vertical: 2),
          child: Text(
            '$index / $total',
            style: AhText.data.copyWith(fontSize: 11, color: const Color(0xFFF6F5F1)),
          ),
        ),
      );
}
