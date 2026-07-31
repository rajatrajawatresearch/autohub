import 'package:autohub_core/autohub_core.dart';
import 'package:autohub_forms/autohub_forms.dart';
import 'package:autohub_ui/autohub_ui.dart';
import 'package:flutter/material.dart' hide FormField;
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:go_router/go_router.dart';

import '../../app/error_surface.dart';
import '../../app/fault.dart';
import '../../app/router.dart';
import '../../data/app_providers.dart';
import '../../data/sell.dart';
import '../../i18n/customer_text.dart';

/// The registry's proof.
///
/// **There is not one vehicle field in this file.** No registration input, no odometer, no fuel
/// picker, no photo prompts, no EV battery question. `SchemaForm` renders whatever
/// `GET /platform/forms/sell_vehicle` returns, and the only product knowledge here is the form's
/// key and what to do with the payload.
///
/// That is the claim in `docs/10-mobile-architecture.md` §1 made concrete: an operations user adds
/// `battery_health_pct` to the EV category, and it appears on this screen at the next launch with
/// its validation, its unit and its `visibleWhen` — nothing rebuilt, nothing submitted to an app
/// store. If a field ever gets written here, every registry in the platform is decoration.
///
/// ## Surviving the tunnel
///
/// Three separate mechanisms, and all three are load-bearing on the one screen that creates supply:
///
/// * **The definition is served from disk.** `RegistryClient` returns the last known snapshot and
///   revalidates behind it, so a seller who opened this on a train keeps typing into a form that
///   was fetched days ago.
/// * **The values autosave.** `SchemaForm` debounces into `autohub_core`'s draft store on every
///   change and flushes unconditionally on `AppLifecycleState.paused` — Android kills a
///   backgrounded process without further warning, and a debounce timer does not fire in a process
///   that no longer exists.
/// * **The draft is recovered without asking.** No "restore your work?" dialogue: the user has no
///   idea what is in the draft until they see it, and answering wrong loses twenty minutes.
class SellScreen extends ConsumerWidget {
  const SellScreen({super.key});

  @override
  Widget build(BuildContext context, WidgetRef ref) {
    final AppStrings t = ref.watch(appStringsProvider);
    final SellState submission = ref.watch(sellProvider);

    return Scaffold(
      body: SafeArea(
        child: switch (submission) {
          SellSubmitted(:final String? listingId) => _Confirmation(
              eyebrowKey: CustomerText.sellDoneEyebrow,
              titleKey: CustomerText.sellDoneTitle,
              bodyKey: CustomerText.sellDoneBody,
              listingId: listingId,
            ),
          // Not "submitted". The seller's vehicle is on their phone, and telling them otherwise is
          // a lie they find out about when nobody calls.
          SellQueued() => const _Confirmation(
              eyebrowKey: CustomerText.sellQueuedEyebrow,
              titleKey: CustomerText.sellQueuedTitle,
              bodyKey: CustomerText.sellQueuedBody,
            ),
          SellComposing() => _Form(t: t),
        },
      ),
    );
  }
}

class _Form extends ConsumerWidget {
  const _Form({required this.t});

  final AppStrings t;

  @override
  Widget build(BuildContext context, WidgetRef ref) {
    final AsyncValue<FormDefinition> definition = ref.watch(formDefinitionProvider(sellFormKey));

    return definition.when(
      loading: () => const _FormSkeleton(),
      // The one failure this screen can have: a definition that has never reached this device, on a
      // network that is not answering. Every other launch is served from disk.
      error: (Object error, StackTrace _) => Padding(
        padding: const EdgeInsets.all(AhSpace.lg),
        child: AppErrorView(
          fault: AppFault.of(error, t),
          onRetry: () async => ref.invalidate(formDefinitionProvider(sellFormKey)),
        ),
      ),
      data: (FormDefinition schema) => ListView(
        padding: const EdgeInsets.all(AhSpace.lg),
        children: <Widget>[
          SectionHeading(
            eyebrow: t(CustomerText.sellEyebrow),
            // The registry's own title for the document — "Tell us about your car" — with the
            // app's heading only as the fallback for a definition that did not author one.
            title: schema.title.isEmpty ? t(CustomerText.sellTitle) : schema.title,
            rule: true,
          ),
          const SizedBox(height: AhSpace.xl),
          SchemaForm(
            definition: schema,
            showTitle: false,
            labels: t.formLabels,
            onSubmit: ref.read(sellProvider.notifier).submit,
            // The registries the form's option lists come from. There is no list of fuel types
            // anywhere in this binary; this is the function that goes and asks.
            loadOptions: ref.watch(formOptionsLoaderProvider),
            // Autosave. `null` here would be a sell flow that loses a tunnel.
            draftStore: ref.watch(draftStoreProvider),
            // Media travels out of band per §4 — resumable multipart, so a 40-photo set survives
            // the app being backgrounded. The endpoint for it is the one wire
            // `docs/contracts/registry-api.md` has not pinned, so no uploader is wired yet and
            // photographs stay on the device, in the draft, `pending`. That is the documented
            // degradation in `MediaUploadController`, and it loses nothing: `localPath` points at a
            // real file, and `resumeInterrupted` picks the transfer up when the uploader lands.
            uploader: null,
          ),
        ],
      ),
    );
  }
}

/// The end of the flow, in both of its honest forms.
class _Confirmation extends ConsumerWidget {
  const _Confirmation({
    required this.eyebrowKey,
    required this.titleKey,
    required this.bodyKey,
    this.listingId,
  });

  final String eyebrowKey;
  final String titleKey;
  final String bodyKey;
  final String? listingId;

  @override
  Widget build(BuildContext context, WidgetRef ref) {
    final AppStrings t = ref.watch(appStringsProvider);

    return Padding(
      padding: const EdgeInsets.all(AhSpace.lg),
      child: Column(
        crossAxisAlignment: CrossAxisAlignment.stretch,
        mainAxisAlignment: MainAxisAlignment.center,
        children: <Widget>[
          SectionHeading(
            eyebrow: t(eyebrowKey),
            title: t(titleKey),
            description: t(bodyKey),
          ),
          const SizedBox(height: AhSpace.xl),
          if (listingId != null) ...<Widget>[
            AhButton(
              label: t(CustomerText.sellDoneView),
              size: AhButtonSize.lg,
              block: true,
              onPressed: () => context.push(Routes.listing(listingId!)),
            ),
            const SizedBox(height: AhSpace.md),
          ],
          AhButton(
            label: t(CustomerText.sellDoneAnother),
            variant: AhButtonVariant.outline,
            size: AhButtonSize.lg,
            block: true,
            onPressed: () {
              ref.read(sellProvider.notifier).reset();
              context.go(Routes.home);
            },
          ),
        ],
      ),
    );
  }
}

class _FormSkeleton extends StatelessWidget {
  const _FormSkeleton();

  @override
  Widget build(BuildContext context) => ListView(
        padding: const EdgeInsets.all(AhSpace.lg),
        children: const <Widget>[
          AhSkeleton(width: 220, height: 28),
          SizedBox(height: AhSpace.xl),
          AhSkeleton.line(width: 120),
          SizedBox(height: AhSpace.sm),
          AhSkeleton(height: 48),
          SizedBox(height: AhSpace.xl),
          AhSkeleton.line(width: 120),
          SizedBox(height: AhSpace.sm),
          AhSkeleton(height: 48),
        ],
      );
}
