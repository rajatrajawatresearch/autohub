import 'package:autohub_core/autohub_core.dart';
import 'package:autohub_ui/autohub_ui.dart';
import 'package:flutter/material.dart';

import '../field_config.dart';
import '../form_scope.dart';
import '../types.dart';
import 'field_chrome.dart';
import 'media_picker.dart';
import 'media_upload.dart';
import 'shared.dart';

/// Documents: RC, insurance, PUC, PAN, the GST certificate, a bank statement for a loan.
///
/// A list rather than a grid, because a document is identified by its name and its size, not by
/// what it looks like at thumbnail scale. `config.multiple` decides whether choosing a second one
/// replaces the first — a shop-and-establishment certificate is singular and a set of bank
/// statements is not, and only the registry knows which this field is.
class FileControl extends StatefulWidget {
  const FileControl(this.props, {this.picker = defaultMediaPicker, super.key});

  final FieldProps props;
  final FormMediaPicker picker;

  @override
  State<FileControl> createState() => _FileControlState();
}

class _FileControlState extends State<FileControl> with WidgetsBindingObserver {
  MediaUploadController? _media;

  @override
  void initState() {
    super.initState();
    WidgetsBinding.instance.addObserver(this);
  }

  @override
  void didChangeAppLifecycleState(AppLifecycleState state) {
    if (state == AppLifecycleState.resumed) _media?.resumeInterrupted();
  }

  @override
  void dispose() {
    WidgetsBinding.instance.removeObserver(this);
    _media?.dispose();
    super.dispose();
  }

  MediaUploadController _controllerFor(BuildContext context) {
    final MediaUploadController? existing = _media;
    if (existing != null) return existing;
    final SchemaFormScope scope = SchemaFormScope.of(context);
    final MediaUploadController created = MediaUploadController(
      field: widget.props.field,
      labels: scope.labels,
      uploader: scope.uploader,
      onChanged: (List<MediaItem> items) =>
          widget.props.onChanged(items.isEmpty ? null : mediaItemsToValue(items)),
    );
    _media = created;
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
    final bool enabled = !props.disabled && !props.readOnly;

    return Column(
      crossAxisAlignment: CrossAxisAlignment.stretch,
      children: <Widget>[
        AhFieldBox(
          invalid: props.invalid,
          child: Row(
            children: <Widget>[
              Expanded(child: AhQuietText(props.field.helpText ?? t(FormLabelKeys.fileChoose))),
              const SizedBox(width: AhSpace.md),
              AhButton(
                label: t(FormLabelKeys.fileChoose),
                icon: Icons.attach_file,
                size: AhButtonSize.sm,
                variant: AhButtonVariant.outline,
                onPressed: enabled ? () => _choose(media) : null,
              ),
            ],
          ),
        ),
        for (final MediaItem item in items) ...<Widget>[
          const SizedBox(height: AhSpace.sm),
          _FileRow(item: item, media: media, enabled: enabled),
        ],
      ],
    );
  }

  Future<void> _choose(MediaUploadController media) async {
    final bool multiple = widget.props.field.multiple;
    final List<PickedMedia> picked =
        await widget.picker(MediaSource.gallery, multiple: multiple);
    if (!mounted || picked.isEmpty) return;

    // Replacing rather than appending for a singular field. Doing it after the pick, not before,
    // means a user who opened the picker and backed out still has the document they had.
    if (!multiple) {
      for (final MediaItem existing in List<MediaItem>.of(media.items)) {
        media.remove(existing.id);
      }
      media.add(<PickedMedia>[picked.first]);
    } else {
      media.add(picked);
    }
    widget.props.onBlur();
  }
}

class _FileRow extends StatelessWidget {
  const _FileRow({required this.item, required this.media, required this.enabled});

  final MediaItem item;
  final MediaUploadController media;
  final bool enabled;

  @override
  Widget build(BuildContext context) {
    final AhSurfaces surfaces = context.ahSurfaces;
    final FormLabels t = SchemaFormScope.of(context).labels;

    return Container(
      decoration: BoxDecoration(
        border: Border(bottom: BorderSide(color: surfaces.borderHairline)),
      ),
      padding: const EdgeInsets.symmetric(vertical: AhSpace.sm),
      child: Row(
        crossAxisAlignment: CrossAxisAlignment.center,
        children: <Widget>[
          Expanded(
            child: Column(
              crossAxisAlignment: CrossAxisAlignment.start,
              mainAxisSize: MainAxisSize.min,
              children: <Widget>[
                Text(
                  item.fileName,
                  maxLines: 1,
                  overflow: TextOverflow.ellipsis,
                  style: AhText.bodySm.copyWith(color: surfaces.textPrimary),
                ),
                AhLabelText(
                  '${formatIndianNumber((item.sizeBytes / 1024).round().clamp(1, 1 << 30))} KB',
                ),
                if (item.status == UploadStatus.uploading)
                  Padding(
                    padding: const EdgeInsets.only(top: AhSpace.xs),
                    child: LinearProgressIndicator(
                      value: item.progress / 100,
                      minHeight: 2,
                      backgroundColor: surfaces.sunken,
                      valueColor: AlwaysStoppedAnimation<Color>(surfaces.accentPrimary),
                    ),
                  ),
                if (item.status == UploadStatus.failed && item.errorMessage != null)
                  Text(
                    item.errorMessage!,
                    style: AhText.bodySm.copyWith(color: surfaces.textPrimary),
                  ),
              ],
            ),
          ),
          if (item.status == UploadStatus.failed)
            AhButton(
              label: t(FormLabelKeys.fileRetry),
              size: AhButtonSize.sm,
              variant: AhButtonVariant.outline,
              onPressed: enabled ? () => media.retry(item.id) : null,
            ),
          IconButton(
            icon: const Icon(Icons.close, size: 18),
            tooltip: t(FormLabelKeys.fileRemove),
            color: surfaces.textSecondary,
            constraints: const BoxConstraints(minWidth: 44, minHeight: 44),
            onPressed: enabled ? () => media.remove(item.id) : null,
          ),
        ],
      ),
    );
  }
}
