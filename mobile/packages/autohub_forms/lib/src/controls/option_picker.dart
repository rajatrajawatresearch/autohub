import 'package:autohub_core/autohub_core.dart';
import 'package:autohub_ui/autohub_ui.dart';
import 'package:flutter/material.dart';

import 'field_chrome.dart';

/// Choosing one value from a registry list, on a phone.
///
/// A sheet rather than a dropdown menu, and this is a place mobile genuinely diverges from the web.
/// A model list can run to two hundred entries; a native menu renders them at menu density with
/// menu-sized targets, anchored to a control that may well be under the keyboard. A sheet gets full
/// width, 48dp rows and a thumb that does not have to travel.
///
/// It returns the chosen option rather than mutating anything, so the same sheet serves `select`,
/// each level of `vehicle_picker`, each select-shaped part of `address`, and the camera/gallery
/// choice in `photo_set`.
Future<Option?> showOptionPicker(
  BuildContext context, {
  required String title,
  required List<Option> options,
  String? selectedValue,
}) {
  final AhSurfaces surfaces = context.ahSurfaces;

  return showModalBottomSheet<Option>(
    context: context,
    backgroundColor: surfaces.card,
    isScrollControlled: true,
    shape: const RoundedRectangleBorder(
      borderRadius: BorderRadius.vertical(top: AhRadius.card),
    ),
    builder: (BuildContext sheetContext) => SafeArea(
      child: ConstrainedBox(
        // Capped so the sheet never covers the field it belongs to. A user who cannot see what they
        // are answering scrolls back up, and on a fifteen-field form that is a real cost.
        constraints: BoxConstraints(maxHeight: MediaQuery.sizeOf(sheetContext).height * 0.7),
        child: Column(
          mainAxisSize: MainAxisSize.min,
          crossAxisAlignment: CrossAxisAlignment.stretch,
          children: <Widget>[
            Padding(
              padding: const EdgeInsets.fromLTRB(AhSpace.lg, AhSpace.lg, AhSpace.lg, AhSpace.sm),
              child: AhLabelText(title),
            ),
            Flexible(
              child: ListView.separated(
                shrinkWrap: true,
                padding: EdgeInsets.zero,
                itemCount: options.length,
                separatorBuilder: (BuildContext _, int __) =>
                    Divider(height: 1, thickness: 1, color: surfaces.borderHairline),
                itemBuilder: (BuildContext rowContext, int index) {
                  final Option option = options[index];
                  final bool selected = option.value == selectedValue;
                  return ListTile(
                                        // Above the 44dp floor, because this list is scrolled fast and tapped once.
                    minTileHeight: 48,
                    title: Text(
                      option.label,
                      style: AhText.body.copyWith(
                        color: surfaces.textPrimary,
                        fontWeight: selected ? FontWeight.w600 : FontWeight.w400,
                      ),
                    ),
                    subtitle: option.count == null
                        ? null
                        : Text(
                            // Facet counts only. A taxonomy option has none, and inventing a
                            // subtitle for it would put an empty second line under every choice.
                            '${option.count}',
                            style: AhText.bodySm.copyWith(color: surfaces.textSecondary),
                          ),
                    trailing:
                        selected ? Icon(Icons.check, size: 18, color: surfaces.accentPrimary) : null,
                    onTap: () => Navigator.of(rowContext).pop(option),
                  );
                },
              ),
            ),
          ],
        ),
      ),
    ),
  );
}

/// The closed state of a picker: the chosen label, or the placeholder, in the input's own chrome.
///
/// Shared by `select`, `vehicle_picker`, `address` and `date` so that a locked cascade level, an
/// empty taxonomy, a failed fetch and a plain unanswered question all read as the same kind of
/// thing — a field waiting for an answer, with a sentence saying which of those it is.
class AhPickerField extends StatelessWidget {
  const AhPickerField({
    required this.label,
    required this.placeholder,
    required this.onTap,
    this.invalid = false,
    this.trailing = Icons.expand_more,
    this.mono = false,
    super.key,
  });

  /// `null` when nothing is chosen — the placeholder shows in the secondary tone instead.
  final String? label;
  final String placeholder;

  /// `null` disables the field: options are loading, the list is empty, or a parent level is
  /// unanswered.
  final VoidCallback? onTap;
  final bool invalid;
  final IconData trailing;
  final bool mono;

  @override
  Widget build(BuildContext context) {
    final AhSurfaces surfaces = context.ahSurfaces;
    final bool enabled = onTap != null;
    final bool empty = label == null;

    return Material(
      color: enabled ? surfaces.card : surfaces.sunken,
      borderRadius: const BorderRadius.all(AhRadius.control),
      child: InkWell(
        onTap: onTap,
        borderRadius: const BorderRadius.all(AhRadius.control),
        child: Container(
          constraints: BoxConstraints(
            minHeight: context.ahTapTarget(context.ahDensity.controlHeight),
          ),
          padding: EdgeInsets.symmetric(horizontal: context.ahDensity.padX),
          decoration: BoxDecoration(
            borderRadius: const BorderRadius.all(AhRadius.control),
            border: Border.all(color: invalid ? AhColors.flag : surfaces.borderStrong),
          ),
          child: Row(
            children: <Widget>[
              Expanded(
                child: Text(
                  label ?? placeholder,
                  maxLines: 1,
                  overflow: TextOverflow.ellipsis,
                  style: (mono ? AhText.data : AhText.body).copyWith(
                    color: empty ? surfaces.textSecondary : surfaces.textPrimary,
                  ),
                ),
              ),
              Icon(trailing, size: 18, color: surfaces.textSecondary),
            ],
          ),
        ),
      ),
    );
  }
}
