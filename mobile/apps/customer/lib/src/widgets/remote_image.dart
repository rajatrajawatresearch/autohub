import 'package:autohub_ui/autohub_ui.dart';
import 'package:cached_network_image/cached_network_image.dart';
import 'package:flutter/widgets.dart';

/// A photograph from the media pipeline, in a slot whose size is already decided.
///
/// `autohub_ui` takes a `Widget` for its media slots rather than a URL, because a widget library
/// that fetched images would be a widget library that knows a network exists. This is the app
/// filling that in, and it is the only place in the binary that turns a URL into pixels.
///
/// Three things it does that a bare `Image.network` does not:
///
/// * **It caches to disk.** Browsing is the same forty cars scrolled past three times in an
///   evening, and re-downloading each of them on a metered connection is a real cost to a real
///   person.
/// * **It never reflows.** The caller has already reserved the box; the placeholder and the error
///   state fill exactly that box, so a slow photograph cannot move the price the user was reaching
///   for. This is the same discipline `VehicleMedia.aspectRatio` exists for.
/// * **It fails quietly.** A missing photograph is a normal state — a dealer's bulk upload lands
///   the rows before the images — and it must look like an empty slot, not like an outage.
class RemoteImage extends StatelessWidget {
  const RemoteImage({
    required this.url,
    this.fit = BoxFit.cover,
    super.key,
  });

  final String url;
  final BoxFit fit;

  @override
  Widget build(BuildContext context) {
    final AhSurfaces surfaces = context.ahSurfaces;

    if (url.isEmpty) return _Blank(color: surfaces.sunken);

    return CachedNetworkImage(
      imageUrl: url,
      fit: fit,
      // No fade. The one animation in this product is the odometer roll on a valuation reveal, and
      // a hundred cross-fading thumbnails in a scrolling list is exactly the ambient motion that
      // would cost that moment its weight.
      fadeInDuration: Duration.zero,
      fadeOutDuration: Duration.zero,
      placeholder: (BuildContext context, String _) => _Blank(color: surfaces.sunken),
      errorWidget: (BuildContext context, String _, Object __) => _Blank(color: surfaces.sunken),
    );
  }
}

class _Blank extends StatelessWidget {
  const _Blank({required this.color});

  final Color color;

  @override
  Widget build(BuildContext context) => ColoredBox(color: color);
}
