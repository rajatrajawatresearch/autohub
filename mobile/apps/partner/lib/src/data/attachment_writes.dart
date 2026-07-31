/// Every mutation this app makes, captured at the instant the partner acted.
///
/// These are factories for `WriteIntent`, not senders, and the split is the point. A `WriteIntent`
/// mints its `Idempotency-Key` **in its constructor, on the gesture** — never at send time — so a
/// tap that leaves as a train enters a tunnel replays byte-identically afterwards and the server
/// recognises it rather than acting twice. If these lived on `AttachmentApi` as
/// `Future<void> accept()` methods, the key would be born when the HTTP call was made, and one tap
/// on *Accept match* over patchy signal would become two commitment blocks on one vehicle.
///
/// The other decision recorded here is **which writes may wait**. `autohub_core` gives two paths
/// and exactly two, so that "does this write queue?" has one answer per call site:
///
/// * **Queued** — the write is worth more late than lost. Publishing a supply post, firing a
///   contract transition. A tunnel must not cost an owner the fifteen minutes they spent filling
///   in a form.
/// * **Immediate** — the write is worth nothing late, and silence would read as success. Accepting
///   a match is the only one in this app, and [acceptProposal] argues it.
library;

import 'package:autohub_core/autohub_core.dart';

/// Headers frozen at the moment of the gesture.
///
/// `X-Active-City` above all: it scopes pricing and feature evaluation server-side, and a write
/// drained after the partner switched cities belongs to the city they were in when they made it.
/// An owner in Pune who accepts a Pune match on the drive to Mumbai must not have it priced under
/// Maharashtra's other metro's rule set because the queue happened to drain late.
Map<String, String> _frozenHeaders(String? activeCity) =>
    (activeCity == null || activeCity.isEmpty)
        ? const <String, String>{}
        : <String, String>{'X-Active-City': activeCity};

/// Publishing a vehicle as attachment supply.
///
/// Queued. The payload is a schema form the owner may have spent ten minutes on, standing beside
/// the vehicle photographing its permit, which is exactly where signal is worst. Losing it to a
/// dead spot would be losing the supply this whole module exists to collect.
WriteIntent publishSupplyPost({
  required Map<String, Object?> payload,
  String? activeCity,
}) =>
    WriteIntent.now(
      endpoint: '/attachment/supply-posts',
      method: WriteMethod.post,
      body: payload,
      headers: _frozenHeaders(activeCity),
    );

/// Publishing a fleet requirement.
///
/// Queued, for the same reason.
WriteIntent publishDemandPost({
  required Map<String, Object?> payload,
  String? activeCity,
}) =>
    WriteIntent.now(
      endpoint: '/attachment/demand-posts',
      method: WriteMethod.post,
      body: payload,
      headers: _frozenHeaders(activeCity),
    );

/// Accepting a proposal — the one write in this app that refuses to be queued.
///
/// The reasoning is the bid argument from `docs/10-mobile-architecture.md` §4, applied to a
/// contract rather than a lot:
///
/// * **It commits an exclusive resource.** Acceptance inserts a commitment block, and two
///   operators accepting the same exclusive vehicle over the same period is a race the database
///   settles with an exclusion constraint. One of them gets `attachment.vehicle.already_committed`
///   with the winning contract's end date, and the matcher immediately offers alternates. That
///   exchange only works if somebody is looking at the screen.
/// * **Silence reads as success.** A partner who taps *Accept* and sees nothing assumes they have
///   a contract. On a queued path they have a row in SQLite; they stop reading the board, decline
///   two other operators, and find out a day later that they lost the vehicle to a `409`.
/// * **The terms it accepts have an expiry.** A proposal lapses, and a drained acceptance against
///   a lapsed proposal is a `410` the partner cannot act on because they have moved on.
///
/// The `Idempotency-Key` still matters and is still minted here: not queueing is not the same as
/// not deduplicating, and a thumb that bounces over showroom wifi must produce one acceptance.
WriteIntent acceptProposal({
  required String proposalId,
  String? activeCity,
}) =>
    WriteIntent.immediateNow(
      endpoint: '/attachment/proposals/$proposalId/accept',
      method: WriteMethod.post,
      headers: _frozenHeaders(activeCity),
    );

/// Firing a workflow action on a contract.
///
/// Queued. A trial confirmation, a renewal acceptance and a notice served are all consequential,
/// and none of them is time-critical to the minute — the workflow's own SLA timers are measured in
/// days. What they must not be is *lost*, and what they must not do on a `409` is retry: the
/// entity moved, the server said what it is now, and the queue surfaces that rather than
/// re-applying an intent formed against a state that no longer exists.
///
/// The action code and its label both come from the contract's `availableActions`, evaluated
/// server-side for this user. Nothing in this app decides which transitions exist or who may fire
/// them.
WriteIntent fireContractAction({
  required String contractId,
  required String actionCode,
  String? reason,
  Map<String, Object?>? payload,
  String? activeCity,
}) =>
    WriteIntent.now(
      endpoint: '/attachment/contracts/$contractId/workflow/actions',
      method: WriteMethod.post,
      body: <String, Object?>{
        'actionCode': actionCode,
        if (reason != null && reason.isNotEmpty) 'reason': reason,
        if (payload != null) 'payload': payload,
      },
      headers: _frozenHeaders(activeCity),
    );
