import 'dart:async';

import 'package:autohub_core/autohub_core.dart';
import 'package:autohub_forms/autohub_forms.dart';
import 'package:autohub_ui/autohub_ui.dart';
import 'package:flutter/material.dart' hide FormField;
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:go_router/go_router.dart';

import '../app/partner_text.dart';
import '../data/partner_providers.dart';
import '../data/registry_options.dart';
import '../widgets/failure_view.dart';
import '../widgets/partner_page.dart';

/// Publishing a post, whichever side of the market it is.
///
/// Adding a vehicle and posting a requirement are the same screen with a different registry
/// address, and that is not a shortcut — it is the architecture working. `attach.supply_post` and
/// `attach.demand_post` are two rows in `platform.form_versions`, and everything that distinguishes
/// them — the duty pattern, the route corridor, the driver arrangement, the permit class, which
/// fields appear only when `driver_supplied_by` is `owner` — is data. There is no `if (side ==
/// supply)` below because there is nothing for it to decide.
///
/// **On submission and the offline queue.** The payload is enqueued, not posted: an owner fills
/// this in standing beside their vehicle photographing a permit, which is exactly where signal is
/// worst, and losing ten minutes of typing to a dead spot would be losing the supply this module
/// exists to collect. The queue is then asked to run immediately, so a partner who *is* online gets
/// the same instant confirmation they would have got from a direct POST.
///
/// One consequence is worth stating plainly rather than discovering: the outbox persists a failed
/// write's machine code and message but not its `fieldErrors` map, so a server `422` arrives here
/// as a banner with its code rather than pinned to the field that caused it. The alternative —
/// posting outside the queue so the problem body survives — trades a ten-minute form against a
/// tunnel, and that is the worse trade. The client-side validator, which shares the registry's rule
/// vocabulary and its machine codes, is what makes a `422` rare in the first place.
class SchemaPostScreen extends ConsumerStatefulWidget {
  const SchemaPostScreen({
    required this.formKey,
    required this.titleKey,
    required this.buildIntent,
    required this.doneLocation,
    super.key,
  });

  /// The form's address in the Form Schema Registry. Never its version.
  final String formKey;

  final String titleKey;

  /// Captures the write at the moment of the gesture. Supplied by the caller so the
  /// `Idempotency-Key` is minted by `attachment_writes.dart` and nowhere else.
  final WriteIntent Function(Map<String, Object?> payload, String? activeCity) buildIntent;

  /// Where to go once the post is away — queued or accepted, the partner is finished here.
  final String doneLocation;

  @override
  ConsumerState<SchemaPostScreen> createState() => _SchemaPostScreenState();
}

class _SchemaPostScreenState extends ConsumerState<SchemaPostScreen> {
  /// Set when the write was captured but has not yet reached the server. Rendered as a note rather
  /// than an error: nothing was lost, and the partner should stop worrying about it.
  bool _queued = false;

  @override
  Widget build(BuildContext context) {
    final PartnerText text = ref.watch(partnerTextProvider);

    return PartnerDetailPage(
      title: text(widget.titleKey),
      children: <Widget>[
        if (_queued) ...<Widget>[
          _QueuedNote(text: text),
          const SizedBox(height: AhSpace.lg),
        ],
        AsyncSection<FormDefinition>(
          value: ref.watch(formDefinitionProvider(widget.formKey)),
          onRetry: () => ref.invalidate(formDefinitionProvider(widget.formKey)),
          builder: (BuildContext context, FormDefinition definition) => SchemaForm(
            definition: definition,
            // Chrome strings from the same bundle the rest of the app reads, so "Continue" is one
            // word across the product rather than one word per renderer.
            labels: FormLabels(
              ref.watch(partnerStringsProvider).valueOrNull?.primary.resources ??
                  const <String, String>{},
            ),
            loadOptions: registryOptionsLoader(ref.watch(registryClientProvider)),
            // Autosave on every field change, keyed by form key. A half-finished supply post has
            // to survive the tunnel, the lock screen and Android killing the process for memory.
            draftStore: ref.watch(draftStoreProvider),
            onSubmit: _submit,
          ),
        ),
      ],
    );
  }

  Future<void> _submit(FormValues payload) async {
    final String? city = ref.read(activeCityProvider).valueOrNull;
    final WriteIntent intent = widget.buildIntent(payload, city);

    final Outbox outbox = ref.read(outboxProvider);
    await outbox.enqueue(intent);

    // Ask the queue to run now. On a live connection this makes the enqueue invisible — the partner
    // gets the same immediate answer a direct POST would have given. Offline it is a no-op and the
    // row waits for the drainer's connectivity edge.
    await ref.read(outboxDrainerProvider).drain();

    // Gone from the queue means the server took it. Anything else is still in flight or stuck, and
    // the row itself says which.
    final OutboxEntry? entry = await outbox.find(intent.idempotencyKey);

    if (entry != null && entry.needsAttention) {
      // Handed back to the form, which shows it above the submit button with its machine code. The
      // row stays in the queue so the partner can retry or discard it from the settings screen
      // after fixing whatever the server objected to.
      throw FormSubmissionRejected(
        message: entry.lastError ?? entry.lastErrorCode ?? 'partner.write.rejected',
      );
    }

    if (!mounted) return;

    if (entry == null) {
      context.go(widget.doneLocation);
      return;
    }

    // Still pending: captured, keyed, and waiting for signal. Say so and get out of the way.
    setState(() => _queued = true);
  }
}

class _QueuedNote extends StatelessWidget {
  const _QueuedNote({required this.text});

  final PartnerText text;

  @override
  Widget build(BuildContext context) {
    final AhSurfaces surfaces = context.ahSurfaces;

    return DecoratedBox(
      decoration: BoxDecoration(
        color: surfaces.sunken,
        borderRadius: const BorderRadius.all(AhRadius.card),
        border: Border.all(color: surfaces.borderHairline),
      ),
      child: Padding(
        padding: const EdgeInsets.all(AhSpace.md),
        child: Column(
          crossAxisAlignment: CrossAxisAlignment.start,
          mainAxisSize: MainAxisSize.min,
          children: <Widget>[
            Text(
              text(PartnerKeys.queuedTitle).toUpperCase(),
              style: AhText.label.copyWith(color: surfaces.textSecondary),
            ),
            const SizedBox(height: AhSpace.xs),
            Text(
              text(PartnerKeys.queuedBody),
              style: AhText.bodySm.copyWith(color: surfaces.textSecondary),
            ),
          ],
        ),
      ),
    );
  }
}
