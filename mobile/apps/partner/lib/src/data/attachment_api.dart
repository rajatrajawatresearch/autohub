import 'package:autohub_core/autohub_core.dart';

import '../app/partner_side.dart';
import 'attachment_models.dart';
import 'wire.dart';

/// Every attachment endpoint this app reads, named once.
///
/// Reads go through [ApiClient]; **writes do not go through this class at all**. A write here would
/// be a write with an `Idempotency-Key` minted at send time, and the whole correctness argument of
/// `autohub_core`'s outbox is that the key is minted on the user's gesture. So writes are captured
/// as a `WriteIntent` at the widget that owns the tap and handed to the queue — see
/// `attachment_writes.dart`. Keeping the two in separate files is what stops a future screen from
/// reaching for a convenient `Future<void> acceptMatch()` here and quietly losing that guarantee.
///
/// There is no cancellation parameter anywhere below, and that is deliberate: cancelling a dio
/// request needs a `CancelToken`, which would make this app depend on dio directly and know which
/// HTTP client `autohub_core` chose. The cost of not cancelling is a response that arrives for a
/// screen that has been popped and is discarded by Riverpod's `autoDispose` — a few kilobytes, on
/// reads that are already cheap.
///
/// Two routes deserve a note.
///
/// **Workflow.** The module spec writes the contract transition as
/// `POST /attachment/contracts/{id}/transitions`, and `contracts/registry-api.md` §3 writes the
/// platform-wide form as `POST /{module}/{resource}/{id}/workflow/actions`. The contract file is
/// authoritative for anyone writing code — it says so in its own first paragraph — so this app uses
/// the platform form, which is also the shape `WorkflowSnapshot` decodes.
///
/// **The match board.** §5.12 exposes matches per post. A phone opens on a board, not on a post, so
/// this adds an aggregate read of the same rows scoped by side. It is marked PROVISIONAL for the
/// same reason the models are, and it degrades to nothing worse than an empty list.
final class AttachmentApi {
  const AttachmentApi(this._client);

  /// Form keys in the Form Schema Registry.
  ///
  /// These are *addresses*, not contents — the same status `RegistryDocuments` has in
  /// `autohub_core`, which names paths and knows nothing about what comes back. The version is
  /// deliberately absent: `platform.form_versions` publishes `attach.supply_post.v4`, and pinning
  /// the `.v4` in a binary would freeze a handset on the version that shipped with it, which is the
  /// exact failure this architecture exists to prevent.
  static const String supplyFormKey = 'attach.supply_post';
  static const String demandFormKey = 'attach.demand_post';

  /// The workflow definition behind a contract's timeline, when the server does not name a more
  /// specific one on the contract itself.
  static const String contractWorkflowKey = 'attachment_contract';

  final ApiClient _client;

  /* ── Supply ─────────────────────────────────────────────────────────────── */

  Future<CursorPage<SupplyPost>> supplyPosts({String? cursor}) =>
      _client.get<CursorPage<SupplyPost>>(
        '/attachment/supply-posts',
        read: (Map<String, Object?> json) =>
            CursorPage<SupplyPost>.fromJson(json, SupplyPost.fromJson),
        query: <String, Object?>{'cursor': cursor},
      );

  /* ── Demand ─────────────────────────────────────────────────────────────── */

  Future<CursorPage<DemandPost>> demandPosts({String? cursor}) =>
      _client.get<CursorPage<DemandPost>>(
        '/attachment/demand-posts',
        read: (Map<String, Object?> json) =>
            CursorPage<DemandPost>.fromJson(json, DemandPost.fromJson),
        query: <String, Object?>{'cursor': cursor},
      );

  /* ── Matching ───────────────────────────────────────────────────────────── */

  /// The board this partner sees from their side.
  ///
  /// `includeIneligible` is sent on the supply side and only there. An owner needs to know which
  /// requirements filtered them out and why — that is the screen that turns a dead end into a
  /// permit renewal — while an operator scrolling forty candidates does not need the eight hundred
  /// vehicles that failed a geo filter.
  Future<CursorPage<AttachmentMatch>> matches({
    required PartnerSide side,
    String? postId,
    String? cursor,
  }) =>
      _client.get<CursorPage<AttachmentMatch>>(
        '/attachment/matches',
        read: (Map<String, Object?> json) =>
            CursorPage<AttachmentMatch>.fromJson(json, AttachmentMatch.fromJson),
        query: <String, Object?>{
          'side': side.storageValue,
          'postId': postId,
          'cursor': cursor,
          if (side == PartnerSide.supply) 'includeIneligible': 'true',
        },
      );

  /* ── Contracts ──────────────────────────────────────────────────────────── */

  Future<CursorPage<AttachmentContract>> contracts({
    required PartnerSide side,
    String? cursor,
  }) =>
      _client.get<CursorPage<AttachmentContract>>(
        '/attachment/contracts',
        read: (Map<String, Object?> json) =>
            CursorPage<AttachmentContract>.fromJson(json, AttachmentContract.fromJson),
        query: <String, Object?>{'side': side.storageValue, 'cursor': cursor},
      );

  Future<AttachmentContract> contract(String id) => _client.get<AttachmentContract>(
        '/attachment/contracts/$id',
        read: AttachmentContract.fromJson,
      );

  /// The live state of one contract and the actions **this** user may fire on it right now.
  ///
  /// Read separately from [contract] rather than trusted from a list row: guards and permissions
  /// are evaluated server-side per request, and a stale `availableActions` offers a partner a
  /// button the server will refuse — which, on a termination, is a genuinely alarming thing to
  /// happen to somebody's livelihood.
  Future<WorkflowSnapshot> contractWorkflow(String id) => _client.get<WorkflowSnapshot>(
        '/attachment/contracts/$id/workflow',
        read: WorkflowSnapshot.fromJson,
      );

  /// Per-document status and expiry for one contract.
  Future<List<ComplianceDocument>> compliance(String contractId) =>
      _client.get<List<ComplianceDocument>>(
        '/attachment/contracts/$contractId/compliance',
        read: (Map<String, Object?> json) =>
            decodeAll<ComplianceDocument>(json['items'], ComplianceDocument.fromJson),
      );

  /* ── Settlement ─────────────────────────────────────────────────────────── */

  Future<CursorPage<SettlementCycle>> cycles({String? contractId, String? cursor}) =>
      _client.get<CursorPage<SettlementCycle>>(
        contractId == null ? '/attachment/cycles' : '/attachment/contracts/$contractId/cycles',
        read: (Map<String, Object?> json) =>
            CursorPage<SettlementCycle>.fromJson(json, SettlementCycle.fromJson),
        query: <String, Object?>{'cursor': cursor},
      );

  /// The full statement, with every line and every rule that fired.
  Future<SettlementCycle> cycle(String id) => _client.get<SettlementCycle>(
        '/attachment/cycles/$id',
        read: SettlementCycle.fromJson,
      );
}
