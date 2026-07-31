import 'dart:math';

import 'package:flutter/foundation.dart';

/// How long the queue waits before trying a write again, and when it gives up.
///
/// Two knobs matter and neither is arbitrary.
///
/// **The curve is exponential with jitter.** Exponential because a server returning `503` is a
/// server under load, and a fleet of handsets retrying on a fixed interval is that load. Jittered
/// because the correlation is the dangerous part: when a cell tower comes back, every phone behind
/// it retries in the same millisecond, and a synchronised thundering herd can hold an API down
/// after the outage that caused it has ended.
///
/// **The poison limit exists so failure is visible.** A write that has been rejected eight times
/// is not going to succeed on the ninth, and retrying it forever means a seller's listing sits in
/// a queue nobody surfaces, marked neither sent nor failed. Poisoning it is what puts it on a
/// screen where a human can decide.
@immutable
final class BackoffPolicy {
  const BackoffPolicy({
    this.base = const Duration(seconds: 2),
    this.ceiling = const Duration(minutes: 5),
    this.jitterFraction = 0.25,
    this.poisonAfter = 8,
    this.outageRetry = const Duration(seconds: 15),
  });

  /// Delay after the first server failure.
  final Duration base;

  /// The curve flattens here.
  ///
  /// Five minutes rather than an hour because the drain is also triggered by connectivity coming
  /// back, so a long ceiling buys nothing except a queue that looks stuck to a user staring at it.
  final Duration ceiling;

  /// ±25% of the computed delay.
  final double jitterFraction;

  /// Server rejections tolerated before a write is parked for a human.
  final int poisonAfter;

  /// Delay used when the request never reached a server.
  ///
  /// Flat and short, and it does *not* consume an attempt. Distance out of signal is not evidence
  /// about the request, so backing off exponentially on it means a user who was in a lift when
  /// they hit submit waits five minutes after reaching the street for a write that would have
  /// succeeded instantly.
  final Duration outageRetry;

  /// Delay after [attempts] server failures. [attempts] is 1 for the first.
  ///
  /// Pass a seeded [random] to make the jitter reproducible; tests need a fixed curve and
  /// production needs an unpredictable one, which is the whole reason this is a parameter.
  Duration delayFor(int attempts, {Random? random}) {
    if (attempts <= 0) return Duration.zero;

    // 2^30 microseconds is already ~18 minutes, well past any sane ceiling; clamping the exponent
    // keeps a corrupt attempt count from overflowing the shift into a negative duration.
    final exponent = min(attempts - 1, 30);
    final scaled = base.inMicroseconds * (1 << exponent);
    final capped = min(scaled, ceiling.inMicroseconds);

    final rng = random ?? _shared;
    final spread = jitterFraction <= 0 ? 0.0 : (rng.nextDouble() * 2 - 1) * jitterFraction;
    final jittered = (capped * (1 + spread)).round();

    return Duration(microseconds: max(jittered, 0));
  }

  /// True once a write has been rejected by a server often enough to stop asking.
  bool isPoisoned(int attempts) => attempts >= poisonAfter;

  static final Random _shared = Random();
}
