import 'dart:async';

import 'package:autohub_core/autohub_core.dart';
import 'package:autohub_ui/autohub_ui.dart';
import 'package:flutter/material.dart';
import 'package:flutter/services.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';

import '../../app/error_surface.dart';
import '../../app/providers.dart';
import '../../app/strings.dart';
import '../../data/bid_client.dart';
import 'lot_controller.dart';

/// Composing and sending one bid.
///
/// Everything §6.2 of `docs/10-mobile-architecture.md` asks for lives in this widget, and each of
/// the five rules is here because being slightly wrong on this screen is expensive and
/// irreversible:
///
/// 1. **The socket is read-only.** Nothing on this panel sends over it. The bid is an HTTP `POST`
///    with an `Idempotency-Key`, so a dropped connection can neither lose it nor place it twice.
/// 2. **`seenBidSeq` travels with the bid** — the sequence whose figure the dealer was looking at
///    when they confirmed, taken from the same `LotView` that painted the price.
/// 3. **The countdown runs on the server clock**, measured when the lot was fetched. It is
///    `BidTicket`'s job and there is no way to omit it.
/// 4. **A confirmation sheet restates the amount** before anything is sent.
/// 5. **No optimistic UI on the figure.** The panel renders what the server said, full stop; after
///    a bid it installs the lot the server returned rather than the amount it just sent.
///
/// And the sixth, from §4: **offline fails loudly and immediately.** A bid is never queued.
class BidPanel extends ConsumerStatefulWidget {
  const BidPanel({required this.view, super.key});

  final LotView view;

  @override
  ConsumerState<BidPanel> createState() => _BidPanelState();
}

class _BidPanelState extends ConsumerState<BidPanel> {
  final TextEditingController _amount = TextEditingController();

  /// True from the moment the dealer confirms until the server answers. The button loses its
  /// handler rather than gaining a spinner — this design system has one animation and it is not
  /// here.
  bool _sending = false;

  /// Set once the dealer edits the field, so the ladder stops following the floor underneath them.
  /// Rewriting a figure someone is typing is how a mis-tap becomes a bid nobody composed.
  bool _touched = false;

  int? _followedMinimum;

  @override
  void initState() {
    super.initState();
    _followLadder();
  }

  @override
  void didUpdateWidget(BidPanel oldWidget) {
    super.didUpdateWidget(oldWidget);
    _followLadder();
  }

  @override
  void dispose() {
    _amount.dispose();
    super.dispose();
  }

  /// Prefills the next legal bid, and keeps it current while the dealer has not typed.
  ///
  /// The figure comes from the server's `minNextBidMinor` — never from `currentBid + increment`
  /// computed here. The ladder can carry slab rules the client has never been told about, and a
  /// locally-derived minimum is a bid the server will reject at the exact moment it matters.
  void _followLadder() {
    if (_touched) return;
    final minimum = widget.view.lot.minNextBidMinor;
    if (_followedMinimum == minimum) return;
    _followedMinimum = minimum;
    _amount.text = '${minimum ~/ 100}';
  }

  @override
  Widget build(BuildContext context) {
    final t = ref.watch(stringsProvider);
    final surfaces = context.ahSurfaces;
    final view = widget.view;
    final lot = view.lot;

    final amountMinor = _amountMinor;
    final belowLadder = amountMinor < lot.minNextBidMinor;

    return Column(
      crossAxisAlignment: CrossAxisAlignment.stretch,
      mainAxisSize: MainAxisSize.min,
      children: <Widget>[
        Row(
          children: <Widget>[
            Expanded(
              child: FieldBlock(
                label: t(DealerText.minimumNextBid),
                valueWidget: MoneyValue(minorUnits: lot.minNextBidMinor, size: MoneySize.sm),
              ),
            ),
            Expanded(
              child: FieldBlock(
                label: t(DealerText.increment),
                valueWidget: MoneyValue(minorUnits: lot.incrementMinor, size: MoneySize.sm),
                align: CrossAxisAlignment.end,
              ),
            ),
          ],
        ),
        const SizedBox(height: AhSpace.lg),
        if (view.isClosed)
          Text(
            t(DealerText.bidClosed),
            style: AhText.body.copyWith(color: surfaces.textSecondary),
          )
        else ...<Widget>[
          AhTextField(
            controller: _amount,
            label: t(DealerText.yourBid),
            mono: true,
            unit: '₹',
            enabled: !_sending,
            keyboardType: const TextInputType.numberWithOptions(),
            inputFormatters: <TextInputFormatter>[
              FilteringTextInputFormatter.digitsOnly,
              // Nine digits is ₹99,99,99,999 — past any vehicle this platform lists, and short
              // enough that a stuck key cannot compose a figure that overflows the confirmation
              // sheet's line.
              LengthLimitingTextInputFormatter(9),
            ],
            error: belowLadder ? t(DealerText.belowLadder) : null,
            hint: belowLadder
                ? null
                : '${t(DealerText.ladderStep)} '
                    '${formatIndianMoney(lot.incrementMinor)}',
            onChanged: (_) => setState(() => _touched = true),
          ),
          const SizedBox(height: AhSpace.md),
          AhButton(
            label: _sending ? t(DealerText.bidSending) : t(DealerText.placeBid),
            // Disabled below the ladder and while a bid is in flight. A double-tap would be
            // deduplicated server-side by the idempotency key anyway — this is belt as well as
            // braces, because the dealer should not see two confirmation sheets either.
            onPressed: belowLadder || _sending ? null : () => unawaited(_confirmAndPlace()),
            // The ticket above already spends this screen's yellow on its countdown strip, so the
            // control that moves the money stays primary green. One yellow per viewport.
            block: true,
          ),
        ],
      ],
    );
  }

  /// Rupees typed → integer paise.
  ///
  /// Integer arithmetic throughout. `double.parse(text) * 100` is how a marketplace accumulates a
  /// bid of ₹8,47,349.99999 and reconciles short at the end of the month.
  int get _amountMinor {
    final digits = _amount.text.replaceAll(RegExp('[^0-9]'), '');
    if (digits.isEmpty) return 0;
    return (int.tryParse(digits) ?? 0) * 100;
  }

  Future<void> _confirmAndPlace() async {
    final t = ref.read(stringsProvider);
    final amountMinor = _amountMinor;

    // Read *before* the sheet opens, so the sequence sent is the one that was on screen when the
    // dealer decided. A frame arriving while they read the confirmation must not silently change
    // what they are agreeing to; if it does, the server rejects the bid as an outbid race and this
    // screen shows them the new truth. That is the intended outcome, not a race to be papered over.
    final seenBidSeq = widget.view.seenBidSeq;

    final confirmed = await _confirmSheet(amountMinor, t);
    if (!mounted || !confirmed) return;

    setState(() => _sending = true);

    final result = await ref.read(bidClientProvider).place(
          lotId: widget.view.lot.lotId,
          amountMinor: amountMinor,
          seenBidSeq: seenBidSeq,
          activeCity: ref.read(activeCityProvider).valueOrNull,
        );

    if (!mounted) return;
    setState(() => _sending = false);

    await _apply(result, t);
  }

  /// Exhaustive over [BidResult]. A seventh outcome is a compile error here, which is why the type
  /// is sealed — the wrong thing to do with a `409` is whatever the `default` branch would have
  /// done.
  Future<void> _apply(BidResult result, DealerStrings t) async {
    // Read inside the branches that need it. Reaching for the controller on a path that does not —
    // an unreachable network, an expired session — would start a lot fetch as a side effect of
    // reporting a failure, on the one screen where an extra request is the last thing wanted.
    LotController controller() => ref.read(lotProvider(widget.view.lot.lotId).notifier);

    switch (result) {
      case BidPlaced(:final response):
        // The server's lot, wholesale. Nothing here composes a figure.
        controller().applyServerLot(response.lot);
        _touched = false;
        if (!mounted) return;
        _announce(t(DealerText.bidPlaced));

      case BidStale(:final code, :final detail):
        // The floor moved. Refresh the truth rather than guessing the next increment — and let the
        // dealer decide again against a figure they can see.
        await controller().refresh();
        if (!mounted) return;
        _touched = false;
        await showFailureSheet(
          context,
          AhFailure(
            title: t(DealerText.outbid),
            message: detail ?? t(DealerText.errOutbidRace),
            code: code,
          ),
          t,
        );

      case BidRefused(:final statusCode, :final code, :final detail):
        await controller().refresh();
        if (!mounted) return;
        await showFailureSheet(
          context,
          AhFailure(
            title: t(DealerText.somethingFailed),
            message: detail ?? t(_messageKeyFor(code)),
            code: code,
            statusCode: statusCode,
          ),
          t,
        );

      case BidServerFailure(:final statusCode, :final code):
        if (!mounted) return;
        await showFailureSheet(
          context,
          AhFailure(
            title: t(DealerText.somethingFailed),
            message: t(DealerText.errBidGeneric),
            code: code,
            statusCode: statusCode,
          ),
          t,
        );

      case BidNeedsSignIn(:final code):
        if (!mounted) return;
        await showFailureSheet(
          context,
          AhFailure(
            title: t(DealerText.somethingFailed),
            message: t(DealerText.errSignedOut),
            code: code,
          ),
          t,
        );

      case BidUnsent(:final code):
        // Loud, immediate, and unambiguous. Nothing was queued and nothing will arrive later; the
        // lot is still open and the dealer is still looking at it.
        if (!mounted) return;
        await showFailureSheet(
          context,
          AhFailure(
            title: t(DealerText.offlineTitle),
            message: t(DealerText.errBidUnsent),
            code: code,
          ),
          t,
        );
    }
  }

  /// Maps the server's stable code onto the sentence for it.
  ///
  /// Branching on `code` is what the contract asks clients to do; branching on `message` would be
  /// branching on text a content person can reword, which on a handset ships to everyone at once
  /// and cannot be rolled back.
  static String _messageKeyFor(String code) => switch (code) {
        'auction.bid.outbid_race' => DealerText.errOutbidRace,
        'auction.bid.below_increment' => DealerText.errBelowIncrement,
        'auction.lot.closed' => DealerText.errLotClosed,
        'auction.bidder.not_verified' => DealerText.errNotVerified,
        _ => DealerText.errBidGeneric,
      };

  /// The sheet a dealer has to read before any money moves.
  ///
  /// It restates the **exact** figure — uncompacted, because `₹8.4L` is not a number anyone can be
  /// held to — beside the plate of the vehicle it buys. A mis-tap on a scrolling wall of countdowns
  /// is an expensive way to buy a truck, and this is the step that costs one second and prevents it.
  Future<bool> _confirmSheet(int amountMinor, DealerStrings t) async {
    final lot = widget.view.lot;

    final answer = await showModalBottomSheet<bool>(
      context: context,
      backgroundColor: context.ahSurfaces.card,
      showDragHandle: true,
      // Not dismissible by tapping outside: the two buttons are the only ways out, so a stray tap
      // cannot be mistaken for either answer.
      isDismissible: false,
      builder: (sheetContext) {
        final surfaces = sheetContext.ahSurfaces;

        return SafeArea(
          child: Padding(
            padding: const EdgeInsets.fromLTRB(AhSpace.lg, 0, AhSpace.lg, AhSpace.lg),
            child: Column(
              crossAxisAlignment: CrossAxisAlignment.stretch,
              mainAxisSize: MainAxisSize.min,
              children: <Widget>[
                SectionHeading(
                  eyebrow: t(DealerText.confirmBidQuestion),
                  title: formatIndianMoney(amountMinor),
                  size: SectionHeadingSize.md,
                ),
                const SizedBox(height: AhSpace.lg),
                Row(
                  children: <Widget>[
                    PlateBlock(
                      registration: lot.listing.registration.canonical,
                      display: lot.listing.registration.display,
                      plateClass: plateClassFromWire(lot.listing.registration.plateClass),
                      size: PlateSize.sm,
                    ),
                    const SizedBox(width: AhSpace.sm),
                    Expanded(
                      child: Text(
                        lot.listing.title,
                        style: AhText.bodySm.copyWith(color: surfaces.textSecondary),
                        maxLines: 2,
                        overflow: TextOverflow.ellipsis,
                      ),
                    ),
                  ],
                ),
                const SizedBox(height: AhSpace.lg),
                Text(
                  t(DealerText.confirmBidTerms),
                  style: AhText.bodySm.copyWith(color: surfaces.textSecondary),
                ),
                const SizedBox(height: AhSpace.lg),
                AhButton(
                  label: t(DealerText.confirmBid),
                  onPressed: () => Navigator.of(sheetContext).pop(true),
                  // The one yellow control in this sheet, and the most consequential action in the
                  // product. The countdown strip is not in view here.
                  variant: AhButtonVariant.action,
                  size: AhButtonSize.lg,
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
        );
      },
    );

    return answer ?? false;
  }

  /// Confirmation of something that went right. A snackbar is correct here and wrong for a failure:
  /// success needs no decision and the new figure is already on the ticket above.
  void _announce(String message) {
    ScaffoldMessenger.of(context).showSnackBar(
      SnackBar(
        content: Text(message, style: AhText.bodySm),
        behavior: SnackBarBehavior.floating,
      ),
    );
  }
}
