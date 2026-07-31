import 'dart:io' show File;

import 'package:autohub_core/autohub_core.dart';
import 'package:autohub_ui/autohub_ui.dart';
import 'package:flutter/material.dart';

import '../field_config.dart';
import '../form_scope.dart';
import '../types.dart';
import 'field_chrome.dart';
import 'media_picker.dart';
import 'media_upload.dart';
import 'option_picker.dart';
import 'shared.dart';

/// The sell flow's core, and the reason this product is an app.
///
/// A seller with a phone in a parking lot does not know what a listing needs, so the control tells
/// them: one slot per prompt from the registry — "Front 3/4", "Odometer", "Tyres" — each a labelled
/// empty frame that is obviously waiting for something. That is the difference between six usable
/// photographs and eleven pictures of the same bumper.
///
/// Order is information: the first item is the cover, so reordering is a real operation. It is
/// available by button and not by drag, which is a deliberate divergence from the web — a seller
/// outdoors, one-handed, in sunlight, cannot drag anything, and a long-press-drag grid on a
/// scrolling form fights the scroll.
///
/// The prompt list, the minimum, the maximum and the accepted types all come from `config`. This
/// file has no opinion about what a car should be photographed from, and the moment it did, adding
/// "Roof" to the commercial category would need an app store release.
class PhotoSetControl extends StatefulWidget {
  const PhotoSetControl(this.props, {this.picker = defaultMediaPicker, super.key});

  final FieldProps props;

  /// Injected so a widget test can drive the whole lifecycle without a plugin registrant.
  final FormMediaPicker picker;

  @override
  State<PhotoSetControl> createState() => _PhotoSetControlState();
}

class _PhotoSetControlState extends State<PhotoSetControl> with WidgetsBindingObserver {
  MediaUploadController? _media;

  @override
  void initState() {
    super.initState();
    WidgetsBinding.instance.addObserver(this);
  }

  @override
  void didChangeAppLifecycleState(AppLifecycleState state) {
    // Coming back from the camera, from a phone call, or from being evicted entirely. Anything left
    // `uploading` has no live future behind it any more; without this it sits at 60% forever,
    // looking busy, until the seller gives up on a listing they had already photographed.
    if (state == AppLifecycleState.resumed) _media?.resumeInterrupted();
  }

  @override
  void dispose() {
    WidgetsBinding.instance.removeObserver(this);
    _media?.dispose();
    super.dispose();
  }

  MediaUploadController _controllerFor(BuildContext context) {
    final SchemaFormScope scope = SchemaFormScope.of(context);
    final MediaUploadController? existing = _media;
    if (existing != null) return existing;

    final MediaUploadController created = MediaUploadController(
      field: widget.props.field,
      labels: scope.labels,
      uploader: scope.uploader,
      onChanged: (List<MediaItem> items) {
        // Written back as plain JSON so the value survives the draft store and the wire unchanged.
        widget.props.onChanged(items.isEmpty ? null : mediaItemsToValue(items));
      },
    );
    _media = created;
    // A draft restored on relaunch arrives here with items already in it.
    created
      ..adopt(asMediaItems(widget.props.value))
      ..resumeInterrupted();
    return created;
  }

  @override
  Widget build(BuildContext context) {
    final FormLabels t = SchemaFormScope.of(context).labels;
    final FieldProps props = widget.props;
    final MediaUploadController media = _controllerFor(context)
      ..adopt(asMediaItems(props.value));

    final List<MediaItem> items = media.items;
    final List<String> prompts = props.field.prompts;
    final num? min = props.field.configMin;
    final num? max = props.field.configMax;
    final bool full = max != null && items.length >= max;
    final bool enabled = !props.disabled && !props.readOnly;

    final Set<String> assigned = <String>{
      for (final MediaItem item in items)
        if (item.prompt != null) item.prompt!,
    };
    final List<String> openPrompts = <String>[
      for (final String prompt in prompts)
        if (!assigned.contains(prompt)) prompt,
    ];
    final List<MediaItem> extras = <MediaItem>[
      for (final MediaItem item in items)
        if (item.prompt == null || !prompts.contains(item.prompt)) item,
    ];

    return Column(
      crossAxisAlignment: CrossAxisAlignment.stretch,
      children: <Widget>[
        Row(
          children: <Widget>[
            Expanded(
              child: AhLabelText(
                min == null
                    ? t(FormLabelKeys.multiselectCount, <String, Object?>{'count': items.length})
                    : t(FormLabelKeys.photoCount, <String, Object?>{
                        'count': items.length,
                        'min': min,
                      }),
                tone: props.invalid ? AhColors.flag : null,
              ),
            ),
            AhButton(
              label: t(FormLabelKeys.photoAdd),
              icon: Icons.add_a_photo_outlined,
              size: AhButtonSize.sm,
              variant: AhButtonVariant.outline,
              onPressed: enabled && !full ? () => _addTo(context, media, null) : null,
            ),
          ],
        ),
        if (prompts.isNotEmpty) ...<Widget>[
          const SizedBox(height: AhSpace.md),
          AhLabelText(t(FormLabelKeys.photoRequiredShots)),
          const SizedBox(height: AhSpace.sm),
          _Grid(
            children: <Widget>[
              for (final String prompt in prompts)
                _slotFor(context, media, items, prompt, enabled: enabled, full: full),
            ],
          ),
        ],
        if (extras.isNotEmpty || prompts.isEmpty) ...<Widget>[
          const SizedBox(height: AhSpace.md),
          if (prompts.isNotEmpty) ...<Widget>[
            AhLabelText(t(FormLabelKeys.photoMoreShots)),
            const SizedBox(height: AhSpace.sm),
          ],
          _Grid(
            children: <Widget>[
              for (final MediaItem item in extras)
                _PhotoTile(
                  item: item,
                  index: items.indexOf(item),
                  total: items.length,
                  openPrompts: openPrompts,
                  media: media,
                  enabled: enabled,
                ),
              if (!full)
                _EmptySlot(
                  prompt: null,
                  enabled: enabled,
                  onTap: () => _addTo(context, media, null),
                ),
            ],
          ),
        ],
      ],
    );
  }

  Widget _slotFor(
    BuildContext context,
    MediaUploadController media,
    List<MediaItem> items,
    String prompt, {
    required bool enabled,
    required bool full,
  }) {
    for (final MediaItem item in items) {
      if (item.prompt != prompt) continue;
      return _PhotoTile(
        item: item,
        index: items.indexOf(item),
        total: items.length,
        openPrompts: const <String>[],
        media: media,
        enabled: enabled,
      );
    }
    return _EmptySlot(
      prompt: prompt,
      enabled: enabled && !full,
      onTap: () => _addTo(context, media, prompt),
    );
  }

  /// Camera or gallery, asked once, at the moment of choosing.
  ///
  /// A prompted slot takes exactly one shot; the loose pile takes as many as the remaining headroom
  /// allows. Slicing to the headroom rather than refusing the batch means a seller who picks twelve
  /// with eight slots left gets eight, not an error.
  Future<void> _addTo(
    BuildContext context,
    MediaUploadController media,
    String? prompt,
  ) async {
    final FormLabels t = SchemaFormScope.of(context).labels;
    final Option? source = await showOptionPicker(
      context,
      title: t(FormLabelKeys.photoAdd),
      options: <Option>[
        Option(value: 'camera', label: t(FormLabelKeys.photoCamera)),
        Option(value: 'gallery', label: t(FormLabelKeys.photoGallery)),
      ],
    );
    if (source == null) return;

    final List<PickedMedia> picked = await widget.picker(
      source.value == 'camera' ? MediaSource.camera : MediaSource.gallery,
      multiple: prompt == null,
    );
    if (!mounted || picked.isEmpty) return;

    final num? max = widget.props.field.configMax;
    final int room = max == null ? picked.length : (max.toInt() - media.items.length);
    if (room <= 0) return;

    final List<PickedMedia> accepted =
        picked.length > room ? picked.sublist(0, room) : picked;

    // A pick into a named slot claims that slot; anything else in the batch lands loose.
    media.add(<PickedMedia>[accepted.first], prompt: prompt);
    if (accepted.length > 1) media.add(accepted.sublist(1));
    widget.props.onBlur();
  }
}

class _Grid extends StatelessWidget {
  const _Grid({required this.children});

  final List<Widget> children;

  @override
  Widget build(BuildContext context) => LayoutBuilder(
        builder: (BuildContext context, BoxConstraints constraints) {
          // Two columns on a handset, three when there is genuinely room. Fixed rather than
          // responsive-by-pixel because a photo tile has a caption, a caption has buttons, and
          // below about 150dp the buttons stop being hittable.
          final int columns = constraints.maxWidth > 520 ? 3 : 2;
          final double width =
              (constraints.maxWidth - (columns - 1) * AhSpace.sm) / columns;
          return Wrap(
            spacing: AhSpace.sm,
            runSpacing: AhSpace.sm,
            children: <Widget>[
              for (final Widget child in children) SizedBox(width: width, child: child),
            ],
          );
        },
      );
}

class _EmptySlot extends StatelessWidget {
  const _EmptySlot({required this.prompt, required this.enabled, required this.onTap});

  final String? prompt;
  final bool enabled;
  final VoidCallback onTap;

  @override
  Widget build(BuildContext context) {
    final AhSurfaces surfaces = context.ahSurfaces;
    final FormLabels t = SchemaFormScope.of(context).labels;

    return Opacity(
      opacity: enabled ? 1 : 0.45,
      child: InkWell(
        onTap: enabled ? onTap : null,
        child: Container(
          height: 132,
          padding: const EdgeInsets.all(AhSpace.sm),
          decoration: BoxDecoration(
            color: surfaces.sunken,
            borderRadius: const BorderRadius.all(AhRadius.card),
            border: Border.all(color: surfaces.borderHairline),
          ),
          child: Column(
            crossAxisAlignment: CrossAxisAlignment.start,
            mainAxisAlignment: MainAxisAlignment.spaceBetween,
            children: <Widget>[
              // The prompt is the whole point of the slot, so it leads.
              AhLabelText(prompt ?? t(FormLabelKeys.photoAdd)),
              Row(
                children: <Widget>[
                  Icon(Icons.photo_camera_outlined, size: 16, color: surfaces.textSecondary),
                  const SizedBox(width: AhSpace.xs),
                  Expanded(child: AhQuietText(t(FormLabelKeys.photoEmptySlot))),
                ],
              ),
            ],
          ),
        ),
      ),
    );
  }
}

class _PhotoTile extends StatelessWidget {
  const _PhotoTile({
    required this.item,
    required this.index,
    required this.total,
    required this.openPrompts,
    required this.media,
    required this.enabled,
  });

  final MediaItem item;
  final int index;
  final int total;
  final List<String> openPrompts;
  final MediaUploadController media;
  final bool enabled;

  @override
  Widget build(BuildContext context) {
    final AhSurfaces surfaces = context.ahSurfaces;
    final FormLabels t = SchemaFormScope.of(context).labels;

    return Container(
      decoration: BoxDecoration(
        color: surfaces.card,
        borderRadius: const BorderRadius.all(AhRadius.card),
        border: Border.all(
          color: item.status == UploadStatus.failed ? AhColors.flag : surfaces.borderHairline,
        ),
      ),
      padding: const EdgeInsets.all(AhSpace.xs),
      child: Column(
        crossAxisAlignment: CrossAxisAlignment.stretch,
        mainAxisSize: MainAxisSize.min,
        children: <Widget>[
          AspectRatio(
            aspectRatio: 4 / 3,
            child: Stack(
              fit: StackFit.expand,
              children: <Widget>[
                ColoredBox(color: surfaces.sunken, child: _Preview(item: item)),
                if (index == 0)
                  Align(
                    alignment: Alignment.topLeft,
                    child: Container(
                      color: surfaces.inverse,
                      padding: const EdgeInsets.symmetric(horizontal: 4, vertical: 2),
                      // A stamp, not a highlight. The plate yellow is spent on the step's one
                      // action, and a badge competing with it is the drift the rationing exists
                      // to stop.
                      child: Text(
                        t(FormLabelKeys.photoCover).toUpperCase(),
                        style: AhText.label.copyWith(color: surfaces.page),
                      ),
                    ),
                  ),
                if (item.status == UploadStatus.uploading)
                  Align(
                    alignment: Alignment.bottomCenter,
                    child: LinearProgressIndicator(
                      value: item.progress / 100,
                      minHeight: 2,
                      backgroundColor: surfaces.sunken,
                      valueColor: AlwaysStoppedAnimation<Color>(surfaces.accentPrimary),
                    ),
                  ),
              ],
            ),
          ),
          const SizedBox(height: AhSpace.xs),
          Row(
            children: <Widget>[
              Expanded(child: AhLabelText(item.prompt ?? t(FormLabelKeys.photoUnassigned))),
              Text(
                (index + 1).toString().padLeft(2, '0'),
                style: AhText.label.copyWith(color: surfaces.textSecondary),
              ),
            ],
          ),
          if (item.status == UploadStatus.failed && item.errorMessage != null)
            Padding(
              padding: const EdgeInsets.only(top: AhSpace.xs),
              child: Text(
                item.errorMessage!,
                style: AhText.bodySm.copyWith(color: surfaces.textPrimary),
              ),
            ),
          Row(
            children: <Widget>[
              _TileAction(
                icon: Icons.chevron_left,
                tooltip: t(FormLabelKeys.photoMoveEarlier),
                onPressed: enabled && index > 0 ? () => media.move(item.id, -1) : null,
              ),
              _TileAction(
                icon: Icons.chevron_right,
                tooltip: t(FormLabelKeys.photoMoveLater),
                onPressed: enabled && index < total - 1 ? () => media.move(item.id, 1) : null,
              ),
              if (openPrompts.isNotEmpty && item.prompt == null)
                _TileAction(
                  icon: Icons.label_outline,
                  tooltip: t(FormLabelKeys.photoAssign),
                  onPressed: enabled ? () => _assign(context) : null,
                ),
              if (item.status == UploadStatus.failed)
                _TileAction(
                  icon: Icons.refresh,
                  tooltip: t(FormLabelKeys.fileRetry),
                  onPressed: enabled ? () => media.retry(item.id) : null,
                ),
              const Spacer(),
              _TileAction(
                icon: Icons.close,
                tooltip: t(FormLabelKeys.fileRemove),
                onPressed: enabled ? () => media.remove(item.id) : null,
              ),
            ],
          ),
        ],
      ),
    );
  }

  Future<void> _assign(BuildContext context) async {
    final FormLabels t = SchemaFormScope.of(context).labels;
    final Option? chosen = await showOptionPicker(
      context,
      title: t(FormLabelKeys.photoAssign),
      options: <Option>[
        for (final String prompt in openPrompts) Option(value: prompt, label: prompt),
      ],
    );
    if (chosen == null) return;
    media.assign(item.id, chosen.value);
  }
}

/// The image itself: the uploaded copy once there is one, the file on disk until then.
///
/// Preferring `localPath` while an upload is in flight is what keeps a thumbnail on screen in a
/// basement — the remote URL exists but nothing can fetch it.
class _Preview extends StatelessWidget {
  const _Preview({required this.item});

  final MediaItem item;

  @override
  Widget build(BuildContext context) {
    final String? path = item.localPath;
    if (path != null) {
      return Image.file(
        File(path),
        fit: BoxFit.cover,
        errorBuilder: (BuildContext _, Object __, StackTrace? ___) => const SizedBox.shrink(),
      );
    }
    final String? url = item.url;
    if (url != null) {
      return Image.network(
        url,
        fit: BoxFit.cover,
        errorBuilder: (BuildContext _, Object __, StackTrace? ___) => const SizedBox.shrink(),
      );
    }
    return const SizedBox.shrink();
  }
}

class _TileAction extends StatelessWidget {
  const _TileAction({required this.icon, required this.tooltip, required this.onPressed});

  final IconData icon;
  final String tooltip;
  final VoidCallback? onPressed;

  @override
  Widget build(BuildContext context) => IconButton(
        icon: Icon(icon, size: 18),
        tooltip: tooltip,
        onPressed: onPressed,
        color: context.ahSurfaces.textSecondary,
        // 44dp of hit area around an 18dp glyph. The tile is small; the target is not.
        constraints: const BoxConstraints(minWidth: 44, minHeight: 44),
        padding: EdgeInsets.zero,
        visualDensity: VisualDensity.compact,
      );
}
