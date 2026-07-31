import 'dart:async';

import 'package:autohub_core/autohub_core.dart';
import 'package:flutter/widgets.dart';

import '../theme/ah_theme.dart';
import '../tokens/colors.dart';
import '../tokens/typography.dart';
import 'money_value.dart';

/// Where the dealer stands on this lot, as the server last reported it.
///
/// Never derived on the client. Comparing your maximum against the current bid locally gives an
/// answer that is right most of the time, and "you are leading" being wrong even occasionally is
/// showing a dealer something false about money.
enum BidPosition { none, leading, outbid }

/// The words. Defaults are en-IN; pass resolved strings from the localisation registry in
/// production, or this ticket is the one screen that cannot ship in Marathi.
@immutable
class BidTicketLabels {
  const BidTicketLabels({
    this.timeLeft = 'Time left',
    this.closed = 'Closed',
    this.currentBid = 'Current bid',
    this.noBids = 'No bids yet',
    this.bids = 'Bids',
    this.leading = 'You lead',
    this.outbid = 'Outbid',
    this.yourMax = 'Your maximum',
    this.extended = 'Extended — a bid in the last minutes moves the close',
  });

  final String timeLeft;
  final String closed;
  final String currentBid;
  final String noBids;
  final String bids;
  final String leading;
  final String outbid;
  final String yourMax;
  final String extended;
}

/// Auction only — and the one place in the product where signal yellow is loud.
///
/// A live lot is the single most consequential thing on any screen it appears on: money moving, a
/// clock running, a position that can flip while you read it. The countdown strip carries the
/// yellow, so the bid control inside [child] should stay primary green and the viewport still
/// holds exactly one yellow element.
///
/// ## The clock
///
/// The countdown renders against [clock], an [AhServerClock] whose offset was measured at connect
/// time — **never** against `DateTime.now()`. A handset two minutes fast shows a live lot as
/// closed and the dealer stops bidding on a truck that is still available; two minutes slow is
/// worse, because they compose a bid on a lot the server has already settled. Neither failure
/// leaves a trace anyone can debug from a support ticket.
///
/// That is why [clock] is required and has no default. There is no parameter here that lets a
/// caller fall back to the device clock by omission.
///
/// ## What this widget does not do
///
/// It does not place bids. Bids go over HTTP to `POST /auction/lots/{id}/bids` with an
/// `Idempotency-Key` and the screen's `seenBidSeq`, never over the socket and never through a
/// countdown widget — so a dropped connection can neither lose a bid nor place it twice. The
/// socket is read-only; this ticket renders what it delivers.
///
/// It also never shows an optimistic figure. [currentBidMinor] is what the server said, full stop.
class BidTicket extends StatefulWidget {
  const BidTicket({
    required this.endsAt,
    required this.clock,
    this.currentBidMinor,
    this.position = BidPosition.none,
    this.bidCount,
    this.yourMaxMinor,
    this.extended = false,
    this.onEnd,
    this.labels = const BidTicketLabels(),
    this.child,
    super.key,
  });

  /// UTC close time from the lot. Re-passed on every `lot.extended` event, which is what makes
  /// anti-snipe extensions appear without a refresh.
  final DateTime endsAt;

  /// Measured at connect time. See the class doc — this is the point of the widget.
  final AhServerClock clock;

  /// Integer paise, or `null` while the lot has no bids.
  final int? currentBidMinor;

  final BidPosition position;
  final int? bidCount;

  /// The dealer's own ceiling. Shown so they can see it without opening the bid sheet, because
  /// the most common expensive mistake is re-bidding above a maximum you forgot you set.
  final int? yourMaxMinor;

  /// True after an anti-snipe extension, so the notice explains why the clock grew rather than
  /// leaving the dealer to assume the countdown is broken.
  final bool extended;

  /// Fired once, when the countdown reaches zero. The screen refetches; it does not assume the
  /// lot closed at exactly the moment its timer said so.
  final VoidCallback? onEnd;

  final BidTicketLabels labels;

  /// The bid control itself. The app owns the action, the ticket owns the state.
  final Widget? child;

  @override
  State<BidTicket> createState() => _BidTicketState();
}

class _BidTicketState extends State<BidTicket> {
  Timer? _timer;
  late Duration _remaining;
  bool _fired = false;

  @override
  void initState() {
    super.initState();
    _remaining = widget.clock.remainingUntil(widget.endsAt);
    _timer = Timer.periodic(const Duration(seconds: 1), (_) => _tick());
  }

  @override
  void didUpdateWidget(BidTicket oldWidget) {
    super.didUpdateWidget(oldWidget);
    // An extension moves the close time. Re-arming `_fired` is what lets a lot that was about to
    // close, and then grew, fire `onEnd` again when it genuinely ends.
    if (oldWidget.endsAt != widget.endsAt || oldWidget.clock != widget.clock) {
      _fired = false;
      _tick();
    }
  }

  @override
  void dispose() {
    _timer?.cancel();
    _timer = null;
    super.dispose();
  }

  void _tick() {
    if (!mounted) return;
    final left = widget.clock.remainingUntil(widget.endsAt);
    setState(() => _remaining = left);
    if (left == Duration.zero && !_fired) {
      _fired = true;
      widget.onEnd?.call();
    }
  }

  @override
  Widget build(BuildContext context) {
    final surfaces = context.ahSurfaces;
    final live = _remaining > Duration.zero;
    final labels = widget.labels;

    return DecoratedBox(
      decoration: BoxDecoration(
        color: surfaces.card,
        borderRadius: const BorderRadius.all(AhRadius.card),
        border: Border.all(color: surfaces.borderHairline),
      ),
      child: ClipRRect(
        borderRadius: const BorderRadius.all(AhRadius.card),
        child: Column(
          crossAxisAlignment: CrossAxisAlignment.start,
          mainAxisSize: MainAxisSize.min,
          children: <Widget>[
            _CountdownStrip(
              live: live,
              remaining: _remaining,
              label: live ? labels.timeLeft : labels.closed,
            ),
            if (widget.extended && live)
              _Notice(
                text: labels.extended,
                ink: AhColors.caution,
                fill: AhColors.cautionSoft,
              ),
            Padding(
              padding: const EdgeInsets.all(AhSpace.lg),
              child: Column(
                crossAxisAlignment: CrossAxisAlignment.start,
                mainAxisSize: MainAxisSize.min,
                children: <Widget>[
                  Row(
                    crossAxisAlignment: CrossAxisAlignment.end,
                    children: <Widget>[
                      Expanded(
                        child: Column(
                          crossAxisAlignment: CrossAxisAlignment.start,
                          mainAxisSize: MainAxisSize.min,
                          children: <Widget>[
                            Text(
                              labels.currentBid.toUpperCase(),
                              style: AhText.label.copyWith(color: surfaces.textSecondary),
                            ),
                            const SizedBox(height: AhSpace.xs),
                            if (widget.currentBidMinor != null)
                              MoneyValue(
                                minorUnits: widget.currentBidMinor!,
                                size: MoneySize.xl,
                              )
                            else
                              Text(
                                labels.noBids,
                                style: AhText.data.copyWith(
                                  fontSize: 22,
                                  color: surfaces.textSecondary,
                                ),
                              ),
                          ],
                        ),
                      ),
                      if (widget.bidCount != null)
                        Column(
                          crossAxisAlignment: CrossAxisAlignment.end,
                          mainAxisSize: MainAxisSize.min,
                          children: <Widget>[
                            Text(
                              labels.bids.toUpperCase(),
                              style: AhText.label.copyWith(color: surfaces.textSecondary),
                            ),
                            const SizedBox(height: AhSpace.xs),
                            Text(
                              '${widget.bidCount}',
                              style: AhText.data.copyWith(color: surfaces.textPrimary),
                            ),
                          ],
                        ),
                    ],
                  ),
                  if (widget.position != BidPosition.none) ...<Widget>[
                    const SizedBox(height: AhSpace.md),
                    _Notice(
                      text: widget.position == BidPosition.leading
                          ? labels.leading
                          : labels.outbid,
                      ink: widget.position == BidPosition.leading
                          ? AhColors.clear
                          : AhColors.flag,
                      fill: widget.position == BidPosition.leading
                          ? AhColors.clearSoft
                          : AhColors.flagSoft,
                    ),
                  ],
                  if (widget.yourMaxMinor != null) ...<Widget>[
                    const SizedBox(height: AhSpace.md),
                    DecoratedBox(
                      decoration: BoxDecoration(
                        border: Border(top: BorderSide(color: surfaces.borderHairline)),
                      ),
                      child: Padding(
                        padding: const EdgeInsets.only(top: AhSpace.md),
                        child: Row(
                          mainAxisAlignment: MainAxisAlignment.spaceBetween,
                          children: <Widget>[
                            Text(
                              labels.yourMax.toUpperCase(),
                              style: AhText.label.copyWith(color: surfaces.textSecondary),
                            ),
                            MoneyValue(
                              minorUnits: widget.yourMaxMinor!,
                              size: MoneySize.sm,
                              tone: MoneyTone.secondary,
                            ),
                          ],
                        ),
                      ),
                    ),
                  ],
                  if (widget.child != null) ...<Widget>[
                    const SizedBox(height: AhSpace.lg),
                    widget.child!,
                  ],
                ],
              ),
            ),
          ],
        ),
      ),
    );
  }
}

class _CountdownStrip extends StatelessWidget {
  const _CountdownStrip({required this.live, required this.remaining, required this.label});

  final bool live;
  final Duration remaining;
  final String label;

  @override
  Widget build(BuildContext context) {
    final surfaces = context.ahSurfaces;
    final ink = live ? AhColors.tarmac : surfaces.textSecondary;

    return DecoratedBox(
      decoration: BoxDecoration(
        color: live ? AhColors.signal : surfaces.sunken,
        border: live ? null : Border(bottom: BorderSide(color: surfaces.borderHairline)),
      ),
      child: Padding(
        padding: const EdgeInsets.symmetric(horizontal: AhSpace.lg, vertical: AhSpace.sm + 2),
        child: Row(
          mainAxisAlignment: MainAxisAlignment.spaceBetween,
          crossAxisAlignment: CrossAxisAlignment.center,
          children: <Widget>[
            Text(label.toUpperCase(), style: AhText.label.copyWith(color: ink)),
            Text(
              formatRemaining(remaining),
              style: AhText.data.copyWith(
                fontSize: 22,
                height: 1,
                fontWeight: FontWeight.w700,
                letterSpacing: 0.8,
                color: ink,
              ),
            ),
          ],
        ),
      ),
    );
  }
}

class _Notice extends StatelessWidget {
  const _Notice({required this.text, required this.ink, required this.fill});

  final String text;
  final Color ink;
  final Color fill;

  @override
  Widget build(BuildContext context) {
    return DecoratedBox(
      decoration: BoxDecoration(
        color: fill,
        border: Border(left: BorderSide(color: ink, width: 2)),
      ),
      child: Padding(
        padding: const EdgeInsets.symmetric(horizontal: AhSpace.sm, vertical: AhSpace.xs + 2),
        child: Text(
          text,
          style: AhText.bodySm.copyWith(color: ink, fontWeight: FontWeight.w600),
        ),
      ),
    );
  }
}

/// `01:14:09`, or `2d 03:11:57` past a day.
///
/// Days are split out rather than rolled into hours because `51:14:09` is a number a dealer has
/// to do arithmetic on, and the whole job of this strip is that they do not have to.
String formatRemaining(Duration remaining) {
  final total = remaining.inSeconds;
  final days = total ~/ 86400;
  final hours = (total % 86400) ~/ 3600;
  final minutes = (total % 3600) ~/ 60;
  final seconds = total % 60;

  final clock = '${_pad(hours)}:${_pad(minutes)}:${_pad(seconds)}';
  return days > 0 ? '${days}d $clock' : clock;
}

String _pad(int value) => value < 10 ? '0$value' : '$value';
