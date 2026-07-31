import 'dart:async';

import 'package:autohub_core/autohub_core.dart';
import 'package:flutter/widgets.dart';

import '../theme/ah_theme.dart';
import '../tokens/typography.dart';

enum OdometerSize { md, lg, xl }

/// The one animation in the entire product.
///
/// When a valuation returns, the rupee figure rolls up like a mechanical odometer — digits
/// counting into place, ~600ms, easing out, each settling a beat after the one to its left. Once,
/// on the reveal. Nowhere else. Everything else in AutoHub is instant: no scroll reveals, no
/// page-load choreography, no shimmer on a skeleton, no ripple under a button. The restraint is
/// the whole reason this lands, and adding a second animation anywhere in the product is what
/// takes it away.
///
/// **Reduced motion is checked, not assumed.** [MediaQuery.disableAnimationsOf] is read on every
/// dependency change, and when it is true the widget renders a plain [Text] of the final value —
/// not a zero-duration roll, and not ten stacked digits with the strip pre-positioned. Someone
/// who has switched animations off has usually done it for a vestibular reason, and a 600ms
/// vertical scroll of ten digits behind a clip rect is exactly the motion they turned off.
///
/// Formatting is [formatIndianMoney] from `autohub_core`. This widget rolls digits; it does not
/// know what a lakh is.
class OdometerValue extends StatefulWidget {
  const OdometerValue({
    required this.minorUnits,
    this.compact = false,
    this.size = OdometerSize.lg,
    this.duration = const Duration(milliseconds: 600),
    this.stagger = const Duration(milliseconds: 45),
    super.key,
  });

  /// Integer paise — the figure being revealed.
  final int minorUnits;

  /// Almost always `true` here. A valuation reveal is a spoken number — `₹8.4L` — and rolling
  /// eight digits of `₹8,42,750` is a slot machine, not an instrument.
  final bool compact;

  final OdometerSize size;

  /// Per-digit roll time. The total is this plus [stagger] × (digits − 1).
  final Duration duration;

  /// The beat between one digit settling and the next starting. Left-to-right, like a real
  /// odometer's carry.
  final Duration stagger;

  @override
  State<OdometerValue> createState() => _OdometerValueState();
}

class _OdometerValueState extends State<OdometerValue> with SingleTickerProviderStateMixin {
  late final AnimationController _controller = AnimationController(vsync: this);
  String _formatted = '';
  int _digitCount = 0;
  bool _reduced = false;

  @override
  void initState() {
    super.initState();
    _resolveValue();
  }

  @override
  void didChangeDependencies() {
    super.didChangeDependencies();
    // MediaQuery is not readable in initState, and it can change under the widget — a user can
    // flip the accessibility setting while the app is backgrounded. Reading it here covers both.
    final reduced = MediaQuery.disableAnimationsOf(context);
    if (reduced != _reduced) {
      _reduced = reduced;
      _restart();
    } else if (_controller.duration == null) {
      _restart();
    }
  }

  @override
  void didUpdateWidget(OdometerValue oldWidget) {
    super.didUpdateWidget(oldWidget);
    if (oldWidget.minorUnits != widget.minorUnits ||
        oldWidget.compact != widget.compact ||
        oldWidget.duration != widget.duration ||
        oldWidget.stagger != widget.stagger) {
      _resolveValue();
      _restart();
    }
  }

  @override
  void dispose() {
    _controller.dispose();
    super.dispose();
  }

  void _resolveValue() {
    _formatted = formatIndianMoney(widget.minorUnits, compact: widget.compact);
    _digitCount = _formatted.split('').where(_isDigit).length;
  }

  void _restart() {
    _controller.duration = _totalDuration;
    if (_reduced) {
      // Parked at the end rather than animated to it. Nothing moves, and the value on screen is
      // the value from the first frame.
      _controller.value = 1;
      return;
    }
    unawaited(_controller.forward(from: 0));
  }

  Duration get _totalDuration {
    final spans = _digitCount > 1 ? _digitCount - 1 : 0;
    return widget.duration + widget.stagger * spans;
  }

  @override
  Widget build(BuildContext context) {
    final surfaces = context.ahSurfaces;
    final fontSize = _fontSize(widget.size);
    final style = AhText.data.copyWith(
      fontSize: fontSize,
      height: 1,
      fontWeight: FontWeight.w700,
      color: surfaces.textPrimary,
    );

    if (_reduced) {
      return Text(_formatted, style: style, maxLines: 1, softWrap: false);
    }

    // The cell is taller than the glyph so ascenders and descenders are not clipped by the same
    // rect that hides the neighbouring digits.
    final cellHeight = fontSize * 1.25;
    final digitWidth = _measureDigitWidth(style);
    final total = _totalDuration.inMicroseconds;
    final span = widget.duration.inMicroseconds;
    final step = widget.stagger.inMicroseconds;

    var digitIndex = 0;
    final children = <Widget>[];

    for (final char in _formatted.split('')) {
      if (!_isDigit(char)) {
        children.add(
          SizedBox(
            height: cellHeight,
            child: Center(
              child: Text(
                char,
                // The rupee sign sits at 0.85em here for the same reason it does in MoneyValue:
                // the digits are what the user is reading, and they hold the line.
                style: char == '₹' ? style.copyWith(fontSize: fontSize * 0.85) : style,
              ),
            ),
          ),
        );
        continue;
      }

      final begin = total == 0 ? 0.0 : (digitIndex * step) / total;
      final end = total == 0 ? 1.0 : ((digitIndex * step) + span) / total;
      children.add(
        _RollingDigit(
          controller: _controller,
          value: int.parse(char),
          interval: Interval(begin.clamp(0.0, 1.0), end.clamp(0.0, 1.0), curve: ahEaseInstrument),
          cellHeight: cellHeight,
          width: digitWidth,
          style: style,
        ),
      );
      digitIndex += 1;
    }

    return Semantics(
      label: _formatted,
      // Ten stacked digits per column would otherwise be read out as "zero one two three…".
      excludeSemantics: true,
      child: Row(mainAxisSize: MainAxisSize.min, children: children),
    );
  }

  /// One text layout of a single glyph, once per build.
  ///
  /// The strip has to be a fixed width or the [ClipRect] has nothing to size against, and in a
  /// monospaced face every digit is that width by definition — so measuring `0` measures all ten.
  static double _measureDigitWidth(TextStyle style) {
    final painter = TextPainter(
      text: TextSpan(text: '0', style: style),
      textDirection: TextDirection.ltr,
    )..layout();
    final width = painter.width;
    painter.dispose();
    return width;
  }

  static bool _isDigit(String char) {
    final code = char.codeUnitAt(0);
    return code >= 0x30 && code <= 0x39;
  }

  static double _fontSize(OdometerSize size) => switch (size) {
        OdometerSize.md => 24,
        OdometerSize.lg => 34,
        OdometerSize.xl => 44,
      };
}

/// One digit column: 0–9 stacked vertically, translated behind a clip so exactly one shows.
class _RollingDigit extends StatelessWidget {
  const _RollingDigit({
    required this.controller,
    required this.value,
    required this.interval,
    required this.cellHeight,
    required this.width,
    required this.style,
  });

  final Animation<double> controller;
  final int value;
  final Interval interval;
  final double cellHeight;
  final double width;
  final TextStyle style;

  @override
  Widget build(BuildContext context) {
    final strip = Column(
      mainAxisSize: MainAxisSize.min,
      children: <Widget>[
        for (var digit = 0; digit <= 9; digit += 1)
          SizedBox(
            height: cellHeight,
            width: width,
            child: Center(child: Text('$digit', style: style)),
          ),
      ],
    );

    return SizedBox(
      width: width,
      height: cellHeight,
      child: ClipRect(
        child: OverflowBox(
          alignment: Alignment.topCenter,
          minHeight: 0,
          maxHeight: cellHeight * 10,
          child: AnimatedBuilder(
            animation: controller,
            // The strip is built once and only the transform is rebuilt — ten Text widgets per
            // digit per frame for the length of the roll would be the most expensive 600ms in
            // the product, on the screen where a customer is forming their first impression.
            child: strip,
            builder: (context, child) => Transform.translate(
              offset: Offset(0, -value * cellHeight * interval.transform(controller.value)),
              child: child,
            ),
          ),
        ),
      ),
    );
  }
}
