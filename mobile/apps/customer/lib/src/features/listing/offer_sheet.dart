import 'dart:async';

import 'package:autohub_forms/autohub_forms.dart' show minorToRupees, rupeesToMinor;
import 'package:autohub_ui/autohub_ui.dart';
import 'package:flutter/material.dart';
import 'package:flutter/services.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';

import '../../data/app_providers.dart';
import '../../data/listing.dart';
import '../../i18n/customer_text.dart';

/// Where a buyer names a figure.
///
/// The amount is converted to integer paise by [rupeesToMinor] — the same string-arithmetic
/// conversion `autohub_forms` uses for every money control in the product, and deliberately not
/// `(rupees * 100).round()`. `8.4 * 100` is `840.0000000000001` in binary floating point, and an
/// offer that is one paise out is a settlement that does not reconcile weeks later in a different
/// system.
class OfferSheet extends ConsumerStatefulWidget {
  const OfferSheet({required this.listingId, this.askingPriceMinor, super.key});

  final String listingId;

  /// Seeds the field, because the overwhelmingly common offer is "the asking price, less a bit".
  final int? askingPriceMinor;

  static Future<void> show(
    BuildContext context, {
    required String listingId,
    int? askingPriceMinor,
  }) {
    return showModalBottomSheet<void>(
      context: context,
      isScrollControlled: true,
      showDragHandle: true,
      backgroundColor: context.ahSurfaces.card,
      builder: (BuildContext context) => Padding(
        // The keyboard is the whole sheet's height on a small handset. Without this the amount
        // field is behind it, which is the one field the sheet exists for.
        padding: EdgeInsets.only(bottom: MediaQuery.viewInsetsOf(context).bottom),
        child: OfferSheet(listingId: listingId, askingPriceMinor: askingPriceMinor),
      ),
    );
  }

  @override
  ConsumerState<OfferSheet> createState() => _OfferSheetState();
}

class _OfferSheetState extends ConsumerState<OfferSheet> {
  // `minorToRupees` is `autohub_forms`'s, shared with every money control in the product: one
  // conversion, one convention about trailing paise, one place to be wrong.
  late final TextEditingController _amount =
      TextEditingController(text: minorToRupees(widget.askingPriceMinor));
  final TextEditingController _message = TextEditingController();

  @override
  void dispose() {
    _amount.dispose();
    _message.dispose();
    super.dispose();
  }

  int? get _amountMinor {
    final int? minor = rupeesToMinor(_amount.text);
    return (minor == null || minor <= 0) ? null : minor;
  }

  @override
  Widget build(BuildContext context) {
    final AppStrings t = ref.watch(appStringsProvider);
    final ListingActionState action = ref.watch(listingActionProvider(widget.listingId));
    final bool busy = action is ListingActionBusy;

    return SafeArea(
      child: Padding(
        padding: const EdgeInsets.fromLTRB(AhSpace.lg, 0, AhSpace.lg, AhSpace.lg),
        child: Column(
          mainAxisSize: MainAxisSize.min,
          crossAxisAlignment: CrossAxisAlignment.stretch,
          children: <Widget>[
            SectionHeading(title: t(CustomerText.listingOfferTitle), rule: true),
            const SizedBox(height: AhSpace.lg),
            AhTextField(
              controller: _amount,
              label: t(CustomerText.listingOfferAmount),
              mono: true,
              unit: '₹',
              autofocus: true,
              keyboardType: const TextInputType.numberWithOptions(decimal: true),
              inputFormatters: <TextInputFormatter>[
                FilteringTextInputFormatter.allow(RegExp(r'[0-9.]')),
              ],
              onChanged: (_) => setState(() {}),
            ),
            const SizedBox(height: AhSpace.lg),
            AhTextField(
              controller: _message,
              label: t(CustomerText.listingOfferMessage),
              maxLines: 3,
              maxLength: 500,
            ),
            const SizedBox(height: AhSpace.lg),
            AhButton(
              label: t(CustomerText.listingOfferSubmit),
              size: AhButtonSize.lg,
              block: true,
              onPressed: (_amountMinor == null || busy) ? null : _send,
            ),
          ],
        ),
      ),
    );
  }

  void _send() {
    final int? minor = _amountMinor;
    if (minor == null) return;
    unawaited(_submit(minor));
  }

  Future<void> _submit(int minor) async {
    await ref.read(listingActionProvider(widget.listingId).notifier).makeOffer(
          amountMinor: minor,
          message: _message.text,
        );
    // A real async gap: the sheet may already be closed, and popping a dead route throws.
    if (!mounted) return;
    final ListingActionState result = ref.read(listingActionProvider(widget.listingId));
    // Only close on an outcome the user does not have to act on. A refusal stays on the sheet
    // showing what the server said, with the figure they typed still in the field.
    if (result is ListingActionDone || result is ListingActionQueued) {
      Navigator.of(context).pop();
    }
  }
}
