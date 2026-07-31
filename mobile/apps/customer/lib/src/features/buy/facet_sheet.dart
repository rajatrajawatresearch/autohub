import 'package:autohub_core/autohub_core.dart';
import 'package:autohub_ui/autohub_ui.dart';
import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';

import '../../data/app_providers.dart';
import '../../data/search.dart';
import '../../i18n/customer_text.dart';

/// The filter sheet, built entirely from what the last search response returned.
///
/// **There is no list of filterable attributes in this file, and there cannot be one.** Facets
/// arrive inside the search response, generated from the taxonomy registry: their codes, their
/// labels, their option lists, their counts and — critically — the *style* they render as. Marking
/// `battery_health_pct` searchable is a database row, and a range slider for it appears here on the
/// next request with nothing rebuilt.
///
/// The style switch is exhaustive over the sealed [SearchFacet] union, so a fifth style added to the
/// contract stops this file compiling rather than silently rendering nothing. A style this build has
/// not shipped decodes to [SearchFacetUnsupported] and is skipped — one missing filter chip is a far
/// better outcome than the failed parse of an entire search response, which on the web once rendered
/// an empty marketplace.
class FacetSheet extends ConsumerWidget {
  const FacetSheet({required this.intent, super.key});

  final ListingIntent intent;

  /// Opens the sheet. Modal rather than a route: a filter is a state of the results behind it, and
  /// pushing a page would put the results the user is adjusting off screen.
  static Future<void> show(BuildContext context, ListingIntent intent) {
    return showModalBottomSheet<void>(
      context: context,
      isScrollControlled: true,
      showDragHandle: true,
      backgroundColor: context.ahSurfaces.card,
      builder: (BuildContext context) => FacetSheet(intent: intent),
    );
  }

  @override
  Widget build(BuildContext context, WidgetRef ref) {
    final AhSurfaces surfaces = context.ahSurfaces;
    final AppStrings t = ref.watch(appStringsProvider);
    final SearchState state = ref.watch(searchProvider(intent));
    final VehicleSearchController controller = ref.read(searchProvider(intent).notifier);

    final List<SearchFacet> facets = state.facets;

    return SafeArea(
      child: ConstrainedBox(
        constraints: BoxConstraints(
          maxHeight: MediaQuery.sizeOf(context).height * 0.85,
        ),
        child: Column(
          mainAxisSize: MainAxisSize.min,
          children: <Widget>[
            Padding(
              padding: const EdgeInsets.fromLTRB(AhSpace.lg, 0, AhSpace.lg, AhSpace.md),
              child: SectionHeading(
                title: t(CustomerText.buyFilters),
                rule: true,
                action: state.activeFilterCount == 0
                    ? null
                    : AhButton(
                        label: t(
                          CustomerText.buyFiltersClear,
                          <String, Object?>{'count': state.activeFilterCount},
                        ),
                        variant: AhButtonVariant.ghost,
                        size: AhButtonSize.sm,
                        onPressed: controller.clearFilters,
                      ),
              ),
            ),
            Flexible(
              child: facets.isEmpty
                  ? Padding(
                      padding: const EdgeInsets.all(AhSpace.lg),
                      child: Text(
                        // No facets is not an error: a market with nothing in it has nothing to
                        // filter by, and the honest sentence is the empty-results one.
                        t(CustomerText.buyEmptyUnfilteredBody),
                        style: AhText.bodySm.copyWith(color: surfaces.textSecondary),
                      ),
                    )
                  : ListView.separated(
                      padding: const EdgeInsets.symmetric(horizontal: AhSpace.lg),
                      itemCount: facets.length,
                      separatorBuilder: (BuildContext context, int index) =>
                          Divider(height: 1, color: surfaces.borderHairline),
                      itemBuilder: (BuildContext context, int index) => _Facet(
                        facet: facets[index],
                        params: state.params,
                        controller: controller,
                      ),
                    ),
            ),
            Padding(
              padding: const EdgeInsets.all(AhSpace.lg),
              child: AhButton(
                label: t(CustomerText.buyFiltersApply),
                size: AhButtonSize.lg,
                block: true,
                onPressed: () => Navigator.of(context).pop(),
              ),
            ),
          ],
        ),
      ),
    );
  }
}

class _Facet extends StatelessWidget {
  const _Facet({required this.facet, required this.params, required this.controller});

  final SearchFacet facet;
  final SearchParams params;
  final VehicleSearchController controller;

  @override
  Widget build(BuildContext context) {
    return Padding(
      padding: const EdgeInsets.symmetric(vertical: AhSpace.lg),
      child: switch (facet) {
        SearchFacetChips(:final String code, :final String label, :final List<Option> options) =>
          _Chips(
            code: code,
            label: label,
            options: options,
            selected: _selectedOf(code),
            controller: controller,
          ),
        // `list` and `toggle` are the same interaction on a phone. The web renders `list` as
        // checkboxes down a rail and `toggle` as a switch; at 360dp both are a tapped row, and
        // inventing a third affordance would be inventing a difference the user cannot see.
        SearchFacetList(:final String code, :final String label, :final List<Option> options) ||
        SearchFacetToggle(:final String code, :final String label, :final List<Option> options) =>
          _Rows(
            code: code,
            label: label,
            options: options,
            selected: _selectedOf(code),
            controller: controller,
          ),
        SearchFacetRange(
          :final String code,
          :final String label,
          :final double min,
          :final double max,
          :final double? step,
          :final String? unit,
        ) =>
          _Range(
            code: code,
            label: label,
            min: min,
            max: max,
            step: step,
            unit: unit,
            current: _rangeOf(code),
            controller: controller,
          ),
        // A style added to the contract after this build shipped. Skipped, not crashed.
        SearchFacetUnsupported() => const SizedBox.shrink(),
      },
    );
  }

  List<String> _selectedOf(String code) {
    final AttributeFilter? filter = params.attributes[code];
    return filter is AttributeAnyOf ? filter.values : const <String>[];
  }

  RangeFilter? _rangeOf(String code) {
    final AttributeFilter? filter = params.attributes[code];
    return filter is AttributeRangeFilter ? filter.range : null;
  }
}

class _FacetLabel extends StatelessWidget {
  const _FacetLabel(this.label);

  final String label;

  @override
  Widget build(BuildContext context) => Text(
        label.toUpperCase(),
        style: AhText.label.copyWith(color: context.ahSurfaces.textSecondary),
      );
}

class _Chips extends StatelessWidget {
  const _Chips({
    required this.code,
    required this.label,
    required this.options,
    required this.selected,
    required this.controller,
  });

  final String code;
  final String label;
  final List<Option> options;
  final List<String> selected;
  final VehicleSearchController controller;

  @override
  Widget build(BuildContext context) {
    final AhSurfaces surfaces = context.ahSurfaces;

    return Column(
      crossAxisAlignment: CrossAxisAlignment.start,
      children: <Widget>[
        _FacetLabel(label),
        const SizedBox(height: AhSpace.md),
        Wrap(
          spacing: AhSpace.sm,
          runSpacing: AhSpace.sm,
          children: <Widget>[
            for (final Option option in options)
              _Chip(
                option: option,
                active: selected.contains(option.value),
                surfaces: surfaces,
                onTap: () => controller.toggleAttributeValue(code, option.value),
              ),
          ],
        ),
      ],
    );
  }
}

class _Chip extends StatelessWidget {
  const _Chip({
    required this.option,
    required this.active,
    required this.surfaces,
    required this.onTap,
  });

  final Option option;
  final bool active;
  final AhSurfaces surfaces;
  final VoidCallback onTap;

  @override
  Widget build(BuildContext context) {
    return Semantics(
      button: true,
      selected: active,
      label: option.label,
      excludeSemantics: true,
      child: Material(
        type: MaterialType.transparency,
        child: InkWell(
          onTap: onTap,
          splashFactory: NoSplash.splashFactory,
          hoverColor: Colors.transparent,
          focusColor: Colors.transparent,
          highlightColor: Colors.transparent,
          child: ConstrainedBox(
            // The floor, even though the painted chip is shorter. A filter nobody can hit is a
            // filter nobody uses.
            constraints: const BoxConstraints(minHeight: ahMinTouchTarget),
            child: Center(
              widthFactor: 1,
              child: DecoratedBox(
                decoration: BoxDecoration(
                  color: active ? surfaces.accentPrimary : Colors.transparent,
                  borderRadius: const BorderRadius.all(AhRadius.control),
                  border: Border.all(
                    color: active ? surfaces.accentPrimary : surfaces.borderStrong,
                  ),
                ),
                child: Padding(
                  padding: const EdgeInsets.symmetric(
                    horizontal: AhSpace.md,
                    vertical: AhSpace.sm,
                  ),
                  child: Row(
                    mainAxisSize: MainAxisSize.min,
                    children: <Widget>[
                      Text(
                        option.label,
                        style: AhText.bodySm.copyWith(
                          color: active ? surfaces.textOnPrimary : surfaces.textPrimary,
                        ),
                      ),
                      if (option.count != null) ...<Widget>[
                        const SizedBox(width: AhSpace.xs),
                        Text(
                          // The server counted; the client formats. Indian grouping, always.
                          formatIndianNumber(option.count!),
                          style: AhText.data.copyWith(
                            fontSize: 11,
                            color: active ? surfaces.textOnPrimary : surfaces.textSecondary,
                          ),
                        ),
                      ],
                    ],
                  ),
                ),
              ),
            ),
          ),
        ),
      ),
    );
  }
}

class _Rows extends StatelessWidget {
  const _Rows({
    required this.code,
    required this.label,
    required this.options,
    required this.selected,
    required this.controller,
  });

  final String code;
  final String label;
  final List<Option> options;
  final List<String> selected;
  final VehicleSearchController controller;

  @override
  Widget build(BuildContext context) {
    final AhSurfaces surfaces = context.ahSurfaces;

    return Column(
      crossAxisAlignment: CrossAxisAlignment.start,
      children: <Widget>[
        _FacetLabel(label),
        const SizedBox(height: AhSpace.sm),
        for (final Option option in options)
          Semantics(
            button: true,
            selected: selected.contains(option.value),
            label: option.label,
            excludeSemantics: true,
            child: Material(
              type: MaterialType.transparency,
              child: InkWell(
                onTap: () => controller.toggleAttributeValue(code, option.value),
                splashFactory: NoSplash.splashFactory,
                hoverColor: Colors.transparent,
                focusColor: Colors.transparent,
                highlightColor: Colors.transparent,
                child: ConstrainedBox(
                  constraints: const BoxConstraints(minHeight: ahMinTouchTarget),
                  child: Row(
                    children: <Widget>[
                      Icon(
                        selected.contains(option.value)
                            ? Icons.check_box_outlined
                            : Icons.check_box_outline_blank,
                        size: 20,
                        color: selected.contains(option.value)
                            ? surfaces.accentPrimary
                            : surfaces.borderStrong,
                      ),
                      const SizedBox(width: AhSpace.md),
                      Expanded(
                        child: Text(
                          option.label,
                          style: AhText.body.copyWith(color: surfaces.textPrimary),
                        ),
                      ),
                      if (option.count != null)
                        Text(
                          formatIndianNumber(option.count!),
                          style: AhText.data.copyWith(
                            fontSize: 12,
                            color: surfaces.textSecondary,
                          ),
                        ),
                    ],
                  ),
                ),
              ),
            ),
          ),
      ],
    );
  }
}

class _Range extends StatefulWidget {
  const _Range({
    required this.code,
    required this.label,
    required this.min,
    required this.max,
    required this.step,
    required this.unit,
    required this.current,
    required this.controller,
  });

  final String code;
  final String label;
  final double min;
  final double max;
  final double? step;
  final String? unit;
  final RangeFilter? current;
  final VehicleSearchController controller;

  @override
  State<_Range> createState() => _RangeState();
}

class _RangeState extends State<_Range> {
  late RangeValues _values = _initial;

  RangeValues get _initial => RangeValues(
        (widget.current?.min ?? widget.min).toDouble().clamp(widget.min, widget.max),
        (widget.current?.max ?? widget.max).toDouble().clamp(widget.min, widget.max),
      );

  @override
  void didUpdateWidget(_Range oldWidget) {
    super.didUpdateWidget(oldWidget);
    // Only adopt an externally-driven change — a cleared filter — rather than every rebuild, which
    // would fight the thumb the user is currently dragging.
    if (oldWidget.current != widget.current) _values = _initial;
  }

  @override
  Widget build(BuildContext context) {
    final AhSurfaces surfaces = context.ahSurfaces;
    // A hundred steps across the range when the registry did not name one: fine enough to feel
    // continuous, coarse enough that a drag does not fire a request per pixel.
    final double step =
        widget.step ?? ((widget.max - widget.min) / 100).clamp(1, double.maxFinite);
    final int divisions = ((widget.max - widget.min) / step).round().clamp(1, 1000);

    return Column(
      crossAxisAlignment: CrossAxisAlignment.start,
      children: <Widget>[
        _FacetLabel(widget.label),
        RangeSlider(
          min: widget.min,
          max: widget.max,
          divisions: divisions,
          values: _values,
          activeColor: surfaces.accentPrimary,
          inactiveColor: surfaces.borderStrong,
          labels: RangeLabels(
            formatIndianNumber(_values.start.round()),
            formatIndianNumber(_values.end.round()),
          ),
          onChanged: (RangeValues next) => setState(() => _values = next),
          // The request fires on release, not on every frame of the drag. A search per pixel is a
          // hundred requests and a hot phone.
          onChangeEnd: (RangeValues next) => widget.controller.setAttributeRange(
            widget.code,
            next.start <= widget.min && next.end >= widget.max
                // Back at both ends is not a filter. Sending `0..500000` would narrow nothing and
                // still count against "Clear 3".
                ? null
                : RangeFilter(min: next.start.round(), max: next.end.round()),
          ),
        ),
        Text(
          '${formatIndianNumber(_values.start.round())} – '
          '${formatIndianNumber(_values.end.round())}'
          '${widget.unit == null ? '' : ' ${widget.unit}'}',
          style: AhText.data.copyWith(fontSize: 12, color: surfaces.textSecondary),
        ),
      ],
    );
  }
}
