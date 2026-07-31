using System.Text.Json.Nodes;
using AutoHub.Modules.Payments.Domain;
using AutoHub.Modules.Payments.Domain.Ledger;
using AutoHub.Platform.Expressions;
using AutoHub.SharedKernel.Money;
using AutoHub.SharedKernel.Results;
using AutoHub.SharedKernel.Time;
using Microsoft.Extensions.Logging;

namespace AutoHub.Modules.Payments.Application;

/// <summary>
/// Turns priced lines into a balanced journal, using posting templates rather than code.
/// </summary>
/// <remarks>
/// The only writer to the ledger. Everything that moves money — a capture, a settlement, a deposit
/// release, a refund, a payout instruction — arrives here as a scope, an idempotency key and a list
/// of coded amounts, and leaves as entries. There is no second path, which is what makes "every
/// money movement writes a double-entry row" a property of the system rather than a rule people are
/// asked to remember.
/// <para>
/// <b>A missing template is a hard failure.</b> If a line arrives whose code no template maps, the
/// posting is refused rather than the line skipped. Skipping would produce a journal that balances —
/// because the line simply is not there — and a deal whose fee silently never reached the accounts.
/// That is the failure mode nobody catches, because nothing is broken enough to alert on.
/// </para>
/// </remarks>
public interface ILedgerPostingService
{
    Task<Result<Journal>> PostAsync(PostingRequest request, CancellationToken cancellationToken);

    /// <summary>Posts the journal that undoes another. Used by refunds and by operational corrections.</summary>
    Task<Result<Journal>> ReverseAsync(
        Guid journalId,
        string idempotencyKey,
        string reason,
        CancellationToken cancellationToken);
}

/// <summary>What a caller states in order to move money. No accounts, no directions, no arithmetic.</summary>
public sealed record PostingRequest
{
    /// <summary>Which set of templates applies, e.g. <c>deal.settle</c>. See <c>PostingScopes</c>.</summary>
    public required string ScopeCode { get; init; }

    /// <summary>Derived from the fact being posted so a retry lands on the journal that exists.</summary>
    public required string IdempotencyKey { get; init; }

    public required Currency Currency { get; init; }

    public required IReadOnlyList<PostingLine> Lines { get; init; }

    /// <summary>
    /// Party roles to owners, for resolving owner-scoped accounts.
    /// </summary>
    /// <remarks>
    /// A template says "credit <c>dealer_wallet</c> for the party in role <c>seller</c>"; this is
    /// where that role becomes a dealer id. Keyed by role rather than by account so one template
    /// serves every deal, and so a deal that names its seller differently — a consignor, a fleet
    /// operator — needs a different role code and not a different template.
    /// </remarks>
    public IReadOnlyDictionary<string, PostingParty> PartiesByRole { get; init; } =
        new Dictionary<string, PostingParty>(StringComparer.Ordinal);

    public string? SourceType { get; init; }

    public Guid? SourceId { get; init; }

    public string? Description { get; init; }

    /// <summary>Facts a template's condition may match on — the deal's city, state, intent.</summary>
    public JsonObject? Context { get; init; }
}

public sealed record PostingLine(string Code, Money Amount, string? Memo = null);

public sealed record PostingParty(string OwnerType, Guid OwnerId);

public sealed class LedgerPostingService : ILedgerPostingService
{
    private readonly ILedgerRepository _ledger;
    private readonly ILedgerAccountResolver _accounts;
    private readonly IPostingTemplateStore _templates;
    private readonly IWalletProjector _wallets;
    private readonly IClock _clock;
    private readonly ILogger<LedgerPostingService> _logger;

    public LedgerPostingService(
        ILedgerRepository ledger,
        ILedgerAccountResolver accounts,
        IPostingTemplateStore templates,
        IWalletProjector wallets,
        IClock clock,
        ILogger<LedgerPostingService> logger)
    {
        _ledger = ledger;
        _accounts = accounts;
        _templates = templates;
        _wallets = wallets;
        _clock = clock;
        _logger = logger;
    }

    public async Task<Result<Journal>> PostAsync(PostingRequest request, CancellationToken cancellationToken)
    {
        var existing = await _ledger.FindByIdempotencyKeyAsync(request.IdempotencyKey, cancellationToken);
        if (existing is not null)
        {
            // Not an error. A redelivered event, a retried effect and an operator re-running a batch
            // all arrive here, and all three want the journal that was posted, not a conflict.
            return existing;
        }

        var templates = await _templates.GetForScopeAsync(request.ScopeCode, cancellationToken);
        if (templates.Count == 0)
        {
            _logger.LogError("No posting templates configured for scope {Scope}", request.ScopeCode);

            return Error.Unexpected(
                "ledger.posting_scope_unconfigured",
                $"No posting templates are configured for '{request.ScopeCode}'.")
                .With("scope", request.ScopeCode);
        }

        var context = request.Context ?? [];
        var builder = JournalBuilder.For(request.ScopeCode, request.IdempotencyKey, _clock.UtcNow);

        if (request.SourceType is not null && request.SourceId is { } sourceId)
        {
            builder.Against(request.SourceType, sourceId);
        }

        if (request.Description is not null)
        {
            builder.Describing(request.Description);
        }

        foreach (var line in request.Lines)
        {
            if (line.Amount.IsZero)
            {
                // A zero line is a legitimate quote outcome — a waived fee, a nil deposit — and an
                // entry for it would tell a reader nothing while failing the positive-amount check.
                continue;
            }

            var template = templates
                .Where(t => t.IsActive && string.Equals(t.LineCode, line.Code, StringComparison.Ordinal))
                .OrderBy(t => t.SortOrder)
                .FirstOrDefault(t => Matches(t, context));

            if (template is null)
            {
                _logger.LogError(
                    "Scope {Scope} has no posting template for line {LineCode}",
                    request.ScopeCode, line.Code);

                return Error.Unexpected(
                    "ledger.posting_template_missing",
                    $"No posting rule maps '{line.Code}' in scope '{request.ScopeCode}'.")
                    .With("scope", request.ScopeCode)
                    .With("lineCode", line.Code);
            }

            var debit = await ResolveAsync(
                template.DebitAccountCode, template.DebitOwnerRole, request, cancellationToken);
            if (debit.IsFailure)
            {
                return Result.Failure<Journal>(debit.Error);
            }

            var credit = await ResolveAsync(
                template.CreditAccountCode, template.CreditOwnerRole, request, cancellationToken);
            if (credit.IsFailure)
            {
                return Result.Failure<Journal>(credit.Error);
            }

            builder.Debit(debit.Value, line.Amount, line.Code, line.Memo);
            builder.Credit(credit.Value, line.Amount, line.Code, line.Memo);
        }

        var journal = builder.Build();
        if (journal.IsFailure)
        {
            return journal;
        }

        await _ledger.AddAsync(journal.Value, cancellationToken);

        // Projections move in the same unit of work as the entries that caused them. Anything else
        // leaves a window in which a wallet screen and the ledger disagree, and a dealer screenshots
        // the window.
        var projected = await _wallets.ApplyAsync(journal.Value, cancellationToken);
        if (projected.IsFailure)
        {
            return Result.Failure<Journal>(projected.Error);
        }

        await _ledger.SaveChangesAsync(cancellationToken);

        return journal;
    }

    public async Task<Result<Journal>> ReverseAsync(
        Guid journalId,
        string idempotencyKey,
        string reason,
        CancellationToken cancellationToken)
    {
        var existing = await _ledger.FindByIdempotencyKeyAsync(idempotencyKey, cancellationToken);
        if (existing is not null)
        {
            return existing;
        }

        var original = await _ledger.GetAsync(journalId, cancellationToken);
        if (original is null)
        {
            return Error.NotFound("ledger.journal_not_found", "No journal with that id.")
                .With("journalId", journalId);
        }

        var accounts = new Dictionary<Guid, LedgerAccount>();
        foreach (var entry in original.Entries)
        {
            if (accounts.ContainsKey(entry.AccountId))
            {
                continue;
            }

            var account = await _accounts.ResolveAsync(
                entry.AccountCode,
                entry.AccountOwnerId is null ? null : OwnerTypeOf(entry.AccountCode),
                entry.AccountOwnerId,
                original.Currency,
                cancellationToken);

            if (account is null)
            {
                return Error.Unexpected(
                    "ledger.account_missing",
                    "An account referenced by the journal being reversed no longer resolves.")
                    .With("accountCode", entry.AccountCode);
            }

            accounts[entry.AccountId] = account;
        }

        var reversal = JournalBuilder
            .For(original.ScopeCode, idempotencyKey, _clock.UtcNow)
            .Describing(reason)
            .Reversing(original.Id)
            .AddRange(original.ReversalLines(accounts))
            .Build();

        if (reversal.IsFailure)
        {
            return reversal;
        }

        await _ledger.AddAsync(reversal.Value, cancellationToken);

        var projected = await _wallets.ApplyAsync(reversal.Value, cancellationToken);
        if (projected.IsFailure)
        {
            return Result.Failure<Journal>(projected.Error);
        }

        await _ledger.SaveChangesAsync(cancellationToken);

        return reversal;
    }

    private async Task<Result<LedgerAccount>> ResolveAsync(
        string accountCode,
        string? ownerRole,
        PostingRequest request,
        CancellationToken cancellationToken)
    {
        string? ownerType = null;
        Guid? ownerId = null;

        if (ownerRole is not null)
        {
            if (!request.PartiesByRole.TryGetValue(ownerRole, out var party))
            {
                // The template asked for a party this posting does not carry. Refusing is the only
                // safe direction: the alternative is crediting the control account, which parks the
                // money in a pool nobody is named on and nobody notices until a dealer asks.
                return Error.Unexpected(
                    "ledger.posting_party_missing",
                    $"The posting rule needs a party in role '{ownerRole}' and none was supplied.")
                    .With("role", ownerRole)
                    .With("accountCode", accountCode);
            }

            ownerType = party.OwnerType;
            ownerId = party.OwnerId;
        }

        var account = await _accounts.ResolveAsync(
            accountCode, ownerType, ownerId, request.Currency, cancellationToken);

        return account is null
            ? Error.Unexpected("ledger.account_missing", $"No ledger account '{accountCode}' exists.")
                .With("accountCode", accountCode)
            : Result.Success(account);
    }

    private static bool Matches(PostingTemplate template, JsonNode context)
    {
        var condition = ConditionExpression.FromJson(template.ConditionJson);

        // An unparseable condition must not match, exactly as in the pricing engine. The safe
        // direction for a posting rule is "does not apply", which surfaces as a missing template and
        // a refused journal rather than as money quietly landing in the wrong account.
        return condition.IsSuccess && condition.Value.Evaluate(context);
    }

    /// <summary>
    /// Which module owns an owner-scoped account's subject.
    /// </summary>
    /// <remarks>
    /// Only needed on the reversal path, where the original entry recorded an owner id but not the
    /// type. A future migration can denormalise the type onto the entry; until then this is the one
    /// place the two wallet accounts are told apart, and it is small enough to be obviously correct.
    /// </remarks>
    private static string OwnerTypeOf(string accountCode)
        => string.Equals(accountCode, LedgerAccounts.DealerWallet, StringComparison.Ordinal)
            ? PaymentOwnerTypes.Dealer
            : PaymentOwnerTypes.User;
}

/// <summary>The owner types this module resolves wallets for.</summary>
public static class PaymentOwnerTypes
{
    public const string Dealer = "identity.dealer";

    public const string User = "identity.user";
}
