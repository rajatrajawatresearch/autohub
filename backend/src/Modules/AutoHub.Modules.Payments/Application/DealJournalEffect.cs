using System.Text.Json.Nodes;
using AutoHub.Modules.Payments.Domain.Ledger;
using AutoHub.Platform.Workflow;
using AutoHub.SharedKernel.Money;
using AutoHub.SharedKernel.Results;
using Microsoft.Extensions.Logging;

namespace AutoHub.Modules.Payments.Application;

/// <summary>The workflow effect codes this module implements.</summary>
/// <remarks>
/// The vocabulary an operator wires transitions to. A developer adds one of these once, with tests;
/// operations then decides which transitions post which journals, and changes its mind later without
/// a deploy.
/// </remarks>
public static class PaymentEffects
{
    /// <summary>Posts a journal from a deal's lines. Scope comes from the effect's own arguments.</summary>
    public const string PostDealJournal = "ledger.post_deal_journal";
}

/// <summary>
/// Posts the ledger journal for a deal transition.
/// </summary>
/// <remarks>
/// <b>This is the seam between the settlement spine and the accounts, and it deliberately carries no
/// dependency on the deals module.</b> Everything it needs — the journal key, the currency, the
/// priced lines, the parties and their roles — arrives on the workflow context that the deal
/// assembled before firing the transition. The payments module therefore never references the deals
/// module, never joins to <c>deals.deals</c>, and never has to be redeployed because a deal grew a
/// column.
/// <para>
/// <b>Which posting scope to use is the effect's argument, not this class's decision.</b> An
/// operator wires <c>{"scope": "deal.collect"}</c> onto the transition that takes money and
/// <c>{"scope": "deal.settle"}</c> onto the one that releases it, and inserts a third with a
/// different scope if a new state is added between them. No code changes for any of that.
/// </para>
/// <para>
/// <b>Failure aborts the transition.</b> The effect returns a failed <c>Result</c>, the engine
/// refuses the move, and the deal stays exactly where it was. That ordering is the entire reason
/// effects run before the state commits: a deal marked settled with no entries behind it is a number
/// on a dashboard that no money supports, and it cannot be detected afterwards because nothing looks
/// broken.
/// </para>
/// </remarks>
public sealed class DealJournalEffectHandler : IWorkflowEffectHandler
{
    private readonly ILedgerPostingService _postings;
    private readonly ILogger<DealJournalEffectHandler> _logger;

    public DealJournalEffectHandler(
        ILedgerPostingService postings,
        ILogger<DealJournalEffectHandler> logger)
    {
        _postings = postings;
        _logger = logger;
    }

    public string EffectCode => PaymentEffects.PostDealJournal;

    public async Task<Result> ExecuteAsync(
        WorkflowEffectContext context,
        JsonNode? arguments,
        CancellationToken cancellationToken)
    {
        var scope = arguments?["scope"]?.GetValue<string>();
        if (string.IsNullOrWhiteSpace(scope))
        {
            return Error.Unexpected(
                "ledger.effect_scope_missing",
                "This transition is wired to post a journal but does not say under which scope.")
                .With("effectCode", EffectCode);
        }

        var settlement = context.EvaluationContext["settlement"] as JsonObject;
        if (settlement is null)
        {
            return Error.Unexpected(
                "ledger.effect_context_missing",
                "The transition context carries no settlement block to post from.");
        }

        var request = BuildRequest(context, settlement, scope);
        if (request.IsFailure)
        {
            return Result.Failure(request.Error);
        }

        var posted = await _postings.PostAsync(request.Value, cancellationToken);
        if (posted.IsFailure)
        {
            _logger.LogError(
                "Posting {Scope} for {EntityId} failed: {Code}",
                scope, context.EntityId, posted.Error.Code);

            return Result.Failure(posted.Error);
        }

        _logger.LogInformation(
            "Posted journal {Reference} ({Scope}) for {EntityId}",
            posted.Value.Reference, scope, context.EntityId);

        return Result.Success();
    }

    private static Result<PostingRequest> BuildRequest(
        WorkflowEffectContext context,
        JsonObject settlement,
        string scope)
    {
        var journalKey = settlement["journalKey"]?.GetValue<string>();
        if (string.IsNullOrWhiteSpace(journalKey))
        {
            return Error.Unexpected(
                "ledger.effect_key_missing",
                "The settlement context carries no journal key, so the posting could not be made repeatable.");
        }

        var currencyCode = settlement["currency"]?.GetValue<string>() ?? Currency.Inr.Code;

        Currency currency;
        try
        {
            currency = Currency.FromCode(currencyCode);
        }
        catch (ArgumentOutOfRangeException)
        {
            return Error.Unexpected("ledger.effect_currency_unsupported", "That currency is not supported.")
                .With("currency", currencyCode);
        }

        var lines = new List<PostingLine>();
        foreach (var node in settlement["lines"] as JsonArray ?? [])
        {
            if (node is not JsonObject line)
            {
                continue;
            }

            var code = line["code"]?.GetValue<string>();
            var minorUnits = line["amountMinorUnits"]?.GetValue<long>();

            if (string.IsNullOrWhiteSpace(code) || minorUnits is null)
            {
                return Error.Unexpected(
                    "ledger.effect_line_malformed",
                    "A settlement line is missing its code or its amount.");
            }

            lines.Add(new PostingLine(code, Money.FromMinorUnits(minorUnits.Value, currency)));
        }

        if (lines.Count == 0)
        {
            return Error.Unexpected(
                "ledger.effect_no_lines",
                "There is nothing to post: the settlement context carries no priced lines.");
        }

        var parties = new Dictionary<string, PostingParty>(StringComparer.Ordinal);
        foreach (var node in settlement["parties"] as JsonArray ?? [])
        {
            if (node is not JsonObject party)
            {
                continue;
            }

            var role = party["role"]?.GetValue<string>();
            var partyId = party["partyId"]?.GetValue<string>();
            var partyType = party["partyType"]?.GetValue<string>();

            // A party with no id is the platform itself, which owns no wallet and needs no sub-ledger.
            if (string.IsNullOrWhiteSpace(role) || !Guid.TryParse(partyId, out var ownerId))
            {
                continue;
            }

            parties[role] = new PostingParty(partyType ?? PaymentOwnerTypes.User, ownerId);
        }

        return new PostingRequest
        {
            ScopeCode = scope,
            IdempotencyKey = journalKey,
            Currency = currency,
            Lines = lines,
            PartiesByRole = parties,
            SourceType = context.WorkflowKey,
            SourceId = context.EntityId,
            Description = settlement["note"]?.GetValue<string>(),
            Context = context.EvaluationContext.DeepClone() as JsonObject,
        };
    }
}
