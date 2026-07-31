import 'dart:async';

import 'package:autohub_core/autohub_core.dart';
import 'package:autohub_forms/autohub_forms.dart';
import 'package:autohub_ui/autohub_ui.dart';
import 'package:flutter/material.dart' hide FormField;
import 'package:flutter_riverpod/flutter_riverpod.dart';

import '../../app/error_surface.dart';
import '../../app/providers.dart';
import '../../app/strings.dart';
import '../../data/workflow_actions.dart';
import '../../widgets/async_section.dart';
import '../../widgets/console.dart';

final AutoDisposeFutureProviderFamily<ListingDetail, String> stockDetailProvider =
    FutureProvider.autoDispose.family<ListingDetail, String>(
  (ref, listingId) => ref.watch(dealerApiProvider).listing(listingId),
);

/// One vehicle in stock: what it is, what has happened to it, and what may happen next.
///
/// **The action list is the server's, verbatim.** `workflow.availableActions` arrives with guards
/// and permissions already evaluated for this dealer, so this screen renders buttons from it and
/// re-derives nothing. There is no `if (state == 'draft')` here and there is nowhere to put one —
/// a client that decides what a user may do is a client an attacker can convince, and on a handset
/// they can take their time about it.
class StockDetailScreen extends ConsumerWidget {
  const StockDetailScreen({required this.listingId, super.key});

  final String listingId;

  @override
  Widget build(BuildContext context, WidgetRef ref) {
    final t = ref.watch(stringsProvider);
    final listing = ref.watch(stockDetailProvider(listingId));

    return Scaffold(
      appBar: AppBar(
        title: Text(
          listing.valueOrNull?.title ?? t(DealerText.inventoryTitle),
          style: AhText.subheading.copyWith(color: context.ahSurfaces.textPrimary),
          maxLines: 1,
          overflow: TextOverflow.ellipsis,
        ),
      ),
      body: RefreshIndicator(
        onRefresh: () async {
          ref.invalidate(stockDetailProvider(listingId));
          try {
            await ref.read(stockDetailProvider(listingId).future);
          } on Object {
            // The panel below already carries the failure; a throwing refresh would replace a
            // working page with a red screen.
          }
        },
        child: ListView(
          physics: const AlwaysScrollableScrollPhysics(),
          padding: consolePagePadding(context),
          children: <Widget>[
            AsyncSection<ListingDetail>(
              value: listing,
              onRetry: () => ref.invalidate(stockDetailProvider(listingId)),
              builder: (context, detail) => _Detail(detail: detail, strings: t),
            ),
          ],
        ),
      ),
    );
  }
}

class _Detail extends ConsumerWidget {
  const _Detail({required this.detail, required this.strings});

  final ListingDetail detail;
  final DealerStrings strings;

  @override
  Widget build(BuildContext context, WidgetRef ref) {
    final surfaces = context.ahSurfaces;
    final workflow = detail.workflow;

    return Column(
      crossAxisAlignment: CrossAxisAlignment.stretch,
      mainAxisSize: MainAxisSize.min,
      children: <Widget>[
        Wrap(
          spacing: AhSpace.sm,
          runSpacing: AhSpace.sm,
          crossAxisAlignment: WrapCrossAlignment.center,
          children: <Widget>[
            PlateBlock(
              registration: detail.registration.canonical,
              display: detail.registration.display,
              plateClass: plateClassFromWire(detail.registration.plateClass),
            ),
            if (workflow != null)
              StatusPill(
                category: workflow.currentState.category,
                label: workflow.currentState.name,
              ),
            // Badge *codes* from the server. The label a dealer reads comes from the i18n registry,
            // and an unknown code renders as itself rather than being dropped — a listing carrying
            // a badge this build has not heard of is still a listing.
            for (final code in detail.badges)
              AhBadge(
                label: strings.or('listing.badge.$code', code.replaceAll('_', ' ')),
                tone: AhBadgeTone.outline,
                size: AhBadgeSize.sm,
              ),
          ],
        ),
        const SizedBox(height: AhSpace.md),
        Text(detail.title, style: AhText.heading.copyWith(color: surfaces.textPrimary)),
        const SizedBox(height: AhSpace.lg),
        ConsoleMetrics(
          metrics: <ConsoleMetric>[
            ConsoleMetric(
              label: strings(DealerText.askingPrice),
              value: detail.priceMinor != null
                  ? MoneyValue(minorUnits: detail.priceMinor!, size: MoneySize.lg)
                  : Text(
                      strings(DealerText.onRequest),
                      style: AhText.subheading.copyWith(color: surfaces.textSecondary),
                    ),
            ),
            ConsoleMetric(
              label: strings(DealerText.views),
              value: Text(
                '${detail.viewCount ?? 0}',
                style: AhText.data.copyWith(fontSize: 20, color: surfaces.textPrimary),
              ),
            ),
          ],
        ),
        if (detail.specStrip.isNotEmpty) ...<Widget>[
          const SizedBox(height: kConsoleBlockGap),
          DecoratedBox(
            decoration: BoxDecoration(
              border: Border.symmetric(
                horizontal: BorderSide(color: surfaces.borderHairline),
              ),
            ),
            child: Padding(
              padding: const EdgeInsets.symmetric(vertical: AhSpace.md),
              child: SpecStrip(items: detail.specStrip),
            ),
          ),
        ],
        if (workflow != null) ...<Widget>[
          const SizedBox(height: kConsoleBlockGap),
          _AvailableActions(
            listingId: detail.listingId,
            snapshot: workflow,
            strings: strings,
          ),
        ],
        // Spec groups, ordered and labelled by the taxonomy registry. Nothing here maps
        // `kms_driven` to "Kms driven" — that mapping is a database row, and doing it in a widget
        // is how a product ends up unable to launch a truck category without an app release.
        for (final group in detail.specGroups) ...<Widget>[
          const SizedBox(height: kConsoleBlockGap),
          SectionHeading(title: group.label, size: SectionHeadingSize.sm, rule: true),
          const SizedBox(height: AhSpace.md),
          _SpecGrid(items: group.items),
        ],
        if (workflow != null) ...<Widget>[
          const SizedBox(height: kConsoleBlockGap),
          _History(snapshot: workflow, strings: strings),
        ],
      ],
    );
  }
}

class _SpecGrid extends StatelessWidget {
  const _SpecGrid({required this.items});

  final List<SpecItem> items;

  @override
  Widget build(BuildContext context) {
    return Wrap(
      spacing: AhSpace.lg,
      runSpacing: AhSpace.md,
      children: <Widget>[
        for (final item in items)
          SizedBox(
            // Two columns at 360dp, which is the width an inspector actually holds. A fluid grid
            // that reflows between one and two columns makes a spec sheet unreadable on the walk
            // from one car to the next.
            width: (MediaQuery.sizeOf(context).width - AhSpace.xxl - AhSpace.lg) / 2,
            child: FieldBlock(label: item.label, value: item.value, mono: true),
          ),
      ],
    );
  }
}

/// The buttons the server offered, and nothing else.
class _AvailableActions extends ConsumerStatefulWidget {
  const _AvailableActions({
    required this.listingId,
    required this.snapshot,
    required this.strings,
  });

  final String listingId;
  final WorkflowSnapshot snapshot;
  final DealerStrings strings;

  @override
  ConsumerState<_AvailableActions> createState() => _AvailableActionsState();
}

class _AvailableActionsState extends ConsumerState<_AvailableActions> {
  /// The action currently in flight. Its button loses its handler, which is how this design system
  /// says "working" — there is no spinner, because the whole motion budget is spent elsewhere.
  String? _running;

  @override
  Widget build(BuildContext context) {
    final t = widget.strings;
    final actions = widget.snapshot.availableActions;

    return Column(
      crossAxisAlignment: CrossAxisAlignment.stretch,
      mainAxisSize: MainAxisSize.min,
      children: <Widget>[
        SectionHeading(
          title: t(DealerText.availableNow),
          description: t(DealerText.availableNowHint),
          size: SectionHeadingSize.sm,
          rule: true,
        ),
        const SizedBox(height: AhSpace.md),
        if (actions.isEmpty)
          EmptyState(title: t(DealerText.noActions), align: CrossAxisAlignment.start)
        else
          for (final action in actions)
            Padding(
              padding: const EdgeInsets.only(bottom: AhSpace.sm),
              child: AhButton(
                label: action.label,
                onPressed: _running == null ? () => unawaited(_fire(action)) : null,
                variant: _variantFor(action.emphasis),
                block: true,
              ),
            ),
      ],
    );
  }

  /// Emphasis is presentation vocabulary the *contract* closes, so this switch is exhaustive and a
  /// fourth emphasis is a compile error rather than a silently grey button.
  ///
  /// `primary` earns the rationed yellow because this screen has no other claim on it: there is no
  /// live countdown here, and exactly one action per snapshot is marked primary by the registry.
  static AhButtonVariant _variantFor(ActionEmphasis? emphasis) => switch (emphasis) {
        ActionEmphasis.primary => AhButtonVariant.action,
        ActionEmphasis.destructive => AhButtonVariant.danger,
        ActionEmphasis.standard => AhButtonVariant.outline,
        null => AhButtonVariant.outline,
      };

  Future<void> _fire(AvailableAction action) async {
    final t = widget.strings;

    String? reason;
    if (action.requiresReason) {
      reason = await _askForReason(action);
      if (!mounted || reason == null) return;
    }

    Map<String, Object?>? payload;
    if (action.formKey != null) {
      payload = await _collectPayload(action.formKey!);
      if (!mounted || payload == null) return;
    }

    if (action.requiresConfirmation) {
      final confirmed = await _confirm(action);
      if (!mounted || !confirmed) return;
    }

    setState(() => _running = action.actionCode);

    final outcome = await ref.read(workflowActionRunnerProvider).fire(
          // `listings.listing` is the entity type the workflow registry names, and the workflow
          // path in §3 of the contract is `/{module}/{resource}/{id}/workflow`.
          module: 'listings',
          resource: 'listings',
          id: widget.listingId,
          actionCode: action.actionCode,
          reason: reason,
          payload: payload,
          activeCity: ref.read(activeCityProvider).valueOrNull,
        );

    if (!mounted) return;
    setState(() => _running = null);

    // Every branch refetches rather than patching local state. The GET and the action POST return
    // the same shape precisely so a screen never has to guess what a transition did.
    ref.invalidate(stockDetailProvider(widget.listingId));

    switch (outcome) {
      case ActionApplied() || ActionAccepted():
        return;
      case ActionQueued():
        await showFailureSheet(
          context,
          AhFailure(
            title: t(DealerText.offlineTitle),
            message: t(DealerText.actionQueued),
          ),
          t,
        );
      case ActionBlocked(:final code, :final detail):
        await showFailureSheet(
          context,
          AhFailure(
            title: t(DealerText.actionBlocked),
            message: detail ?? t(DealerText.actionBlocked),
            code: code,
          ),
          t,
        );
      case ActionFailed(:final code, :final detail):
        await showFailureSheet(
          context,
          AhFailure(
            title: t(DealerText.somethingFailed),
            message: detail ?? t(DealerText.somethingFailed),
            code: code,
          ),
          t,
        );
    }
  }

  Future<String?> _askForReason(AvailableAction action) {
    final controller = TextEditingController();
    final t = widget.strings;

    return showModalBottomSheet<String>(
      context: context,
      isScrollControlled: true,
      backgroundColor: context.ahSurfaces.card,
      showDragHandle: true,
      builder: (sheetContext) => Padding(
        padding: EdgeInsets.only(bottom: MediaQuery.viewInsetsOf(sheetContext).bottom),
        child: SafeArea(
          child: Padding(
            padding: const EdgeInsets.fromLTRB(AhSpace.lg, 0, AhSpace.lg, AhSpace.lg),
            child: Column(
              crossAxisAlignment: CrossAxisAlignment.stretch,
              mainAxisSize: MainAxisSize.min,
              children: <Widget>[
                SectionHeading(title: action.label, size: SectionHeadingSize.sm),
                const SizedBox(height: AhSpace.md),
                AhTextField(
                  controller: controller,
                  label: t(DealerText.actionReason),
                  maxLines: 3,
                  autofocus: true,
                ),
                const SizedBox(height: AhSpace.lg),
                AhButton(
                  label: t(DealerText.actionConfirm),
                  onPressed: () => Navigator.of(sheetContext).pop(controller.text.trim()),
                  block: true,
                ),
                const SizedBox(height: AhSpace.sm),
                AhButton(
                  label: t(DealerText.cancel),
                  onPressed: () => Navigator.of(sheetContext).pop(),
                  variant: AhButtonVariant.ghost,
                  block: true,
                ),
              ],
            ),
          ),
        ),
      ),
    ).whenComplete(controller.dispose);
  }

  /// Renders the transition's own form from the Form Schema Registry.
  ///
  /// This is the same `SchemaForm` the sell flow uses, walking a different document. A transition
  /// that starts requiring a rejection category, or a collection date, is a registry edit — the
  /// field appears on handsets that shipped months earlier, with its validation and its conditional
  /// visibility, and nothing here names it.
  Future<Map<String, Object?>?> _collectPayload(String formKey) async {
    final FormDefinition definition;
    try {
      definition = await ref.read(registryClientProvider).formDefinition(formKey);
    } on RegistryUnavailableException catch (error) {
      if (!mounted) return null;
      await showFailureSheet(context, AhFailure.from(error, widget.strings), widget.strings);
      return null;
    }

    if (!mounted) return null;

    return showModalBottomSheet<Map<String, Object?>>(
      context: context,
      isScrollControlled: true,
      backgroundColor: context.ahSurfaces.card,
      showDragHandle: true,
      builder: (sheetContext) => Padding(
        padding: EdgeInsets.only(bottom: MediaQuery.viewInsetsOf(sheetContext).bottom),
        child: SafeArea(
          child: SingleChildScrollView(
            padding: const EdgeInsets.fromLTRB(AhSpace.lg, 0, AhSpace.lg, AhSpace.lg),
            child: SchemaForm(
              definition: definition,
              // The payload arrives with hidden fields already stripped, so what the server sees is
              // exactly what the client validated.
              onSubmit: (payload) async => Navigator.of(sheetContext).pop(payload),
            ),
          ),
        ),
      ),
    );
  }

  Future<bool> _confirm(AvailableAction action) async {
    final t = widget.strings;

    final answer = await showModalBottomSheet<bool>(
      context: context,
      backgroundColor: context.ahSurfaces.card,
      showDragHandle: true,
      builder: (sheetContext) => SafeArea(
        child: Padding(
          padding: const EdgeInsets.fromLTRB(AhSpace.lg, 0, AhSpace.lg, AhSpace.lg),
          child: Column(
            crossAxisAlignment: CrossAxisAlignment.stretch,
            mainAxisSize: MainAxisSize.min,
            children: <Widget>[
              SectionHeading(title: action.label, size: SectionHeadingSize.sm),
              const SizedBox(height: AhSpace.lg),
              AhButton(
                label: t(DealerText.actionConfirm),
                onPressed: () => Navigator.of(sheetContext).pop(true),
                variant: _variantFor(action.emphasis),
                block: true,
              ),
              const SizedBox(height: AhSpace.sm),
              AhButton(
                label: t(DealerText.cancel),
                onPressed: () => Navigator.of(sheetContext).pop(false),
                variant: AhButtonVariant.ghost,
                block: true,
              ),
            ],
          ),
        ),
      ),
    );

    return answer ?? false;
  }
}

/// The audit trail, rendered from the workflow registry.
class _History extends ConsumerWidget {
  const _History({required this.snapshot, required this.strings});

  final WorkflowSnapshot snapshot;
  final DealerStrings strings;

  @override
  Widget build(BuildContext context, WidgetRef ref) {
    final definition = ref.watch(workflowDefinitionProvider(snapshot.workflowKey)).valueOrNull;

    return Column(
      crossAxisAlignment: CrossAxisAlignment.stretch,
      mainAxisSize: MainAxisSize.min,
      children: <Widget>[
        SectionHeading(
          title: strings(DealerText.history),
          description: strings(DealerText.historyDescription),
          size: SectionHeadingSize.sm,
          rule: true,
        ),
        const SizedBox(height: AhSpace.md),
        WorkflowTimeline(
          // Passed through untouched. Dropping an unparseable timestamp rather than drawing a 1970
          // row is still the behaviour — it just lives in the timeline now, so the customer and
          // partner apps cannot answer the same question differently.
          entries: snapshot.history,
          states: definition?.states ?? const <WorkflowState>[],
          empty: EmptyState(
            title: strings(DealerText.historyEmpty),
            align: CrossAxisAlignment.start,
          ),
        ),
      ],
    );
  }
}
