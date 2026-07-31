import 'dart:async';

import 'package:autohub_core/autohub_core.dart';
import 'package:autohub_ui/autohub_ui.dart';
import 'package:flutter/material.dart';
import 'package:flutter/services.dart';

import 'strings.dart';

/// A failure, split into the part a dealer reads and the part support needs.
///
/// The split is the whole point. `01-design-system.md` §7: errors state the problem and the fix, in
/// the interface's voice. `platform.workflow.transition_not_allowed` states neither — it is a
/// *routing* token for whoever picks up the phone, and putting it in the sentence teaches a dealer
/// that this product talks to itself in front of them.
///
/// So [message] is what the screen says and [code] never appears until someone asks for it. It is
/// not hidden — a support agent says "tap Details and read me the code" — and it sits behind one
/// tap with a copy button beside it, because reading `platform.workflow.transition_not_allowed`
/// down a phone line is how it gets transcribed wrong.
@immutable
final class AhFailure {
  const AhFailure({
    required this.title,
    required this.message,
    this.code,
    this.traceId,
    this.statusCode,
  });

  /// Builds from anything a provider can throw.
  ///
  /// `ApiException` is the common case and carries everything. Everything else — a decode failure,
  /// a registry that has never been cached, a bug — resolves to the generic sentence with whatever
  /// machine detail can be salvaged. An unhandled exception rendered as a red screen is strictly
  /// worse than an honest "that did not go through" with a code beside it.
  factory AhFailure.from(Object error, DealerStrings t) {
    if (error is ApiException) {
      return AhFailure(
        title: error.isOffline ? t(DealerText.offlineTitle) : t(DealerText.somethingFailed),
        // The server's `detail` is already resolved through the i18n registry for this user's
        // locale, so it wins over anything this binary could say. The local sentence is the
        // fallback, not the default.
        message: error.message,
        code: error.code,
        traceId: error.traceId,
        statusCode: error.statusCode == 0 ? null : error.statusCode,
      );
    }

    if (error is RegistryUnavailableException) {
      return AhFailure(
        title: t(DealerText.offlineTitle),
        message: t(DealerText.somethingFailed),
        code: 'registry.unavailable.${error.name}',
      );
    }

    return AhFailure(
      title: t(DealerText.somethingFailed),
      message: t(DealerText.somethingFailed),
      code: 'client.unhandled',
    );
  }

  /// What is true. Short.
  final String title;

  /// What the dealer can do about it, in the product's voice. Never a machine code.
  final String message;

  /// `{module}.{resource}.{condition}`. For support, behind a disclosure.
  final String? code;

  /// Correlates the failure with the server's logs.
  final String? traceId;

  final int? statusCode;

  /// The one string support asks for, assembled once so the copy button and the label cannot drift
  /// apart.
  String get supportReference => <String>[
        if (code != null) code!,
        if (statusCode != null) 'HTTP $statusCode',
        if (traceId != null) traceId!,
      ].join(' · ');

  bool get hasSupportReference => supportReference.isNotEmpty;
}

/// A screen that could not load, as a block inside the page.
///
/// An [EmptyState] rather than a dialog, because a dialog demands a decision and this needs none:
/// the dealer's stock has not changed, the retry is right there, and the rest of the console still
/// works. Modal error handling is how an app becomes unusable on a bad connection.
class FailurePanel extends StatelessWidget {
  const FailurePanel({
    required this.failure,
    required this.strings,
    this.onRetry,
    super.key,
  });

  final AhFailure failure;
  final DealerStrings strings;
  final VoidCallback? onRetry;

  @override
  Widget build(BuildContext context) {
    return Column(
      crossAxisAlignment: CrossAxisAlignment.stretch,
      mainAxisSize: MainAxisSize.min,
      children: <Widget>[
        EmptyState(
          title: failure.title,
          body: failure.message,
          action: onRetry == null
              ? null
              : AhButton(
                  label: strings(DealerText.retry),
                  onPressed: onRetry,
                  variant: AhButtonVariant.outline,
                  size: AhButtonSize.sm,
                ),
        ),
        if (failure.hasSupportReference) ...<Widget>[
          const SizedBox(height: AhSpace.sm),
          SupportReference(failure: failure, strings: strings),
        ],
      ],
    );
  }
}

/// The machine code, collapsed.
///
/// Collapsed and not absent: a dealer on the phone to support has to produce it in one tap, and
/// support needs it transcribed correctly — which is why the copy button sits beside it rather than
/// leaving them to read a trace id aloud.
class SupportReference extends StatefulWidget {
  const SupportReference({required this.failure, required this.strings, super.key});

  final AhFailure failure;
  final DealerStrings strings;

  @override
  State<SupportReference> createState() => _SupportReferenceState();
}

class _SupportReferenceState extends State<SupportReference> {
  bool _open = false;
  bool _copied = false;

  @override
  Widget build(BuildContext context) {
    final surfaces = context.ahSurfaces;
    final t = widget.strings;
    final reference = widget.failure.supportReference;

    if (!_open) {
      return Align(
        alignment: Alignment.centerLeft,
        child: AhButton(
          label: t(DealerText.showDetails),
          onPressed: () => setState(() => _open = true),
          variant: AhButtonVariant.ghost,
          size: AhButtonSize.sm,
        ),
      );
    }

    return DecoratedBox(
      decoration: BoxDecoration(
        color: surfaces.sunken,
        borderRadius: const BorderRadius.all(AhRadius.control),
        border: Border.all(color: surfaces.borderHairline),
      ),
      child: Padding(
        padding: const EdgeInsets.all(AhSpace.md),
        child: Row(
          crossAxisAlignment: CrossAxisAlignment.start,
          children: <Widget>[
            Expanded(
              child: FieldBlock(
                label: t(DealerText.supportCode),
                value: reference,
                mono: true,
              ),
            ),
            const SizedBox(width: AhSpace.sm),
            AhButton(
              label: _copied ? t(DealerText.copied) : t(DealerText.copyCode),
              onPressed: () => unawaited(_copy(reference)),
              variant: AhButtonVariant.outline,
              size: AhButtonSize.sm,
            ),
          ],
        ),
      ),
    );
  }

  Future<void> _copy(String reference) async {
    await Clipboard.setData(ClipboardData(text: reference));
    // The clipboard is a platform channel, so this is an async gap and the sheet may already be
    // gone. Every `setState` past an `await` in this app is guarded the same way.
    if (!mounted) return;
    setState(() => _copied = true);
  }
}

/// A transient failure — an action that did not apply, a bid that did not send.
///
/// A sheet rather than a snackbar for anything with money in it. A snackbar times out, and a dealer
/// who looked away for four seconds is left with a screen showing no error and no bid, which they
/// will read as "it worked".
Future<void> showFailureSheet(
  BuildContext context,
  AhFailure failure,
  DealerStrings strings,
) {
  return showModalBottomSheet<void>(
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
            SectionHeading(title: failure.title, size: SectionHeadingSize.sm),
            const SizedBox(height: AhSpace.sm),
            Text(
              failure.message,
              style: AhText.body.copyWith(color: sheetContext.ahSurfaces.textSecondary),
            ),
            if (failure.hasSupportReference) ...<Widget>[
              const SizedBox(height: AhSpace.lg),
              SupportReference(failure: failure, strings: strings),
            ],
            const SizedBox(height: AhSpace.lg),
            AhButton(
              label: strings(DealerText.close),
              onPressed: () => Navigator.of(sheetContext).pop(),
              variant: AhButtonVariant.outline,
              block: true,
            ),
          ],
        ),
      ),
    ),
  );
}
