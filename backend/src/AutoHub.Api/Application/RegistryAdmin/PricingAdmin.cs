using System.Text.Json.Nodes;
using AutoHub.Api.Application.Behaviours;
using AutoHub.Persistence;
using AutoHub.Platform.Access;
using AutoHub.Platform.Caching;
using AutoHub.Platform.Pricing;
using AutoHub.SharedKernel.Results;
using AutoHub.SharedKernel.Time;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace AutoHub.Api.Application.RegistryAdmin;

/// <summary>A whole rule set, authored as one document.</summary>
/// <remarks>
/// Amounts are minor units everywhere, and rates are fractions — <c>0.02</c> for 2%. Both are
/// stated in the property names rather than left to a comment, because percent-versus-fraction is a
/// hundred-fold error and rupees-versus-paise is another, and this is the document where an
/// operator types both.
/// </remarks>
public sealed record RuleSetBody(string Name, IReadOnlyList<PricingRuleBody> Rules);

public sealed record PricingRuleBody(
    string Code,
    string Name,
    int Priority,
    JsonNode? Condition,
    DateTimeOffset? EffectiveFrom,
    DateTimeOffset? EffectiveTo,
    string? Explanation,
    IReadOnlyList<FeeComponentBody> Components);

public sealed record FeeComponentBody(
    string Code,
    string Label,
    string Basis,
    decimal? Rate,
    long? FlatMinorUnits,
    long? MinMinorUnits,
    long? MaxMinorUnits,
    IReadOnlyList<string>? AppliesTo,
    int SortOrder,
    IReadOnlyList<FeeSlabBody>? Slabs);

public sealed record FeeSlabBody(long FromMinorUnits, long? ToMinorUnits, decimal? Rate, long? FlatMinorUnits);

internal sealed record CreateRuleSetCommand(string Scope, RuleSetBody Body)
    : IRequest<Result<RegistryWriteResponse>>, ITransactionalRequest;

internal sealed record PublishRuleSetCommand(string Scope, int Version)
    : IRequest<Result<RegistryWriteResponse>>, ITransactionalRequest;

internal sealed record RollbackRuleSetCommand(string Scope, int ToVersion)
    : IRequest<Result<RegistryWriteResponse>>, ITransactionalRequest;

internal sealed class CreateRuleSetValidator : AbstractValidator<CreateRuleSetCommand>
{
    public CreateRuleSetValidator()
    {
        RuleFor(command => command.Scope).NotEmpty().WithErrorCode("pricing.scope_required");
        RuleFor(command => command.Body.Rules).NotEmpty().WithErrorCode("pricing.rules_required");
    }
}

internal sealed class PricingAdminHandler
    : IRequestHandler<CreateRuleSetCommand, Result<RegistryWriteResponse>>,
      IRequestHandler<PublishRuleSetCommand, Result<RegistryWriteResponse>>,
      IRequestHandler<RollbackRuleSetCommand, Result<RegistryWriteResponse>>
{
    private const string Entity = "platform.pricing_rule_set";

    private readonly AutoHubDbContext _context;
    private readonly RegistryPublisher _publisher;
    private readonly IClock _clock;

    public PricingAdminHandler(AutoHubDbContext context, RegistryPublisher publisher, IClock clock)
    {
        _context = context;
        _publisher = publisher;
        _clock = clock;
    }

    public async Task<Result<RegistryWriteResponse>> Handle(
        CreateRuleSetCommand command,
        CancellationToken cancellationToken)
    {
        var nextVersion = await NextVersionAsync(command.Scope, cancellationToken);

        var built = Build(command.Scope, nextVersion, command.Body);
        if (built.IsFailure)
        {
            return Result.Failure<RegistryWriteResponse>(built.Error);
        }

        _context.PricingRuleSets.Add(built.Value);

        _publisher.Record(
            PlatformPermissions.PricingPublish,
            Entity,
            command.Scope,
            built.Value.Id,
            $"Drafted pricing rule set '{command.Scope}' v{nextVersion}.",
            after: Describe(built.Value));

        return new RegistryWriteResponse(RegistryNames.Pricing, nextVersion, command.Scope, built.Value.Id);
    }

    public async Task<Result<RegistryWriteResponse>> Handle(
        PublishRuleSetCommand command,
        CancellationToken cancellationToken)
    {
        var ruleSet = await LoadAsync(command.Scope, command.Version, cancellationToken);

        if (ruleSet is null)
        {
            return Error.NotFound(
                "pricing.rule_set_not_found",
                $"Scope '{command.Scope}' has no version {command.Version}.");
        }

        if (ruleSet.IsPublished)
        {
            return Error.Conflict("pricing.already_published", "That version is already published.");
        }

        var superseded = await WithdrawCurrentAsync(command.Scope, cancellationToken);
        ruleSet.Publish(_clock.UtcNow);

        var version = await _publisher.PublishAsync(
            RegistryNames.Pricing,
            PlatformPermissions.PricingPublish,
            Entity,
            command.Scope,
            ruleSet.Id,
            superseded is null
                ? $"Published pricing rule set '{command.Scope}' v{ruleSet.Version}."
                : $"Published pricing rule set '{command.Scope}' v{ruleSet.Version}, "
                  + $"superseding v{superseded.Version}.",
            before: superseded is null ? null : Describe(superseded),
            after: Describe(ruleSet),
            cancellationToken);

        return new RegistryWriteResponse(RegistryNames.Pricing, version, command.Scope, ruleSet.Id);
    }

    /// <summary>
    /// Republishes an earlier rule set as a new version.
    /// </summary>
    /// <remarks>
    /// Forward, like every rollback in this codebase. Quotes already issued reference the version
    /// that produced them, so unpublishing would make an invoice unexplainable — and "why was I
    /// charged this?" is the one question a pricing registry exists to answer.
    /// </remarks>
    public async Task<Result<RegistryWriteResponse>> Handle(
        RollbackRuleSetCommand command,
        CancellationToken cancellationToken)
    {
        var source = await LoadAsync(command.Scope, command.ToVersion, cancellationToken);

        if (source is null)
        {
            return Error.NotFound(
                "pricing.rule_set_not_found",
                $"Scope '{command.Scope}' has no version {command.ToVersion}.");
        }

        var nextVersion = await NextVersionAsync(command.Scope, cancellationToken);
        var clone = RuleSet.Draft(source.Scope, nextVersion, source.Name);

        foreach (var rule in source.Rules)
        {
            var copy = new PricingRule(
                rule.Code, rule.Name, rule.Priority, rule.ConditionJson,
                rule.EffectiveFrom, rule.EffectiveTo, rule.Explanation);

            foreach (var component in rule.Components)
            {
                var componentCopy = new FeeComponent(
                    component.Code, component.Label, component.Basis, component.Rate,
                    component.FlatMinorUnits, component.MinMinorUnits, component.MaxMinorUnits,
                    component.AppliesToCsv, component.SortOrder);

                foreach (var slab in component.Slabs)
                {
                    componentCopy.AddSlab(new FeeSlab(
                        slab.FromMinorUnits, slab.ToMinorUnits, slab.Rate, slab.FlatMinorUnits));
                }

                copy.AddComponent(componentCopy);
            }

            clone.AddRule(copy);
        }

        await WithdrawCurrentAsync(command.Scope, cancellationToken);
        clone.Publish(_clock.UtcNow);
        _context.PricingRuleSets.Add(clone);

        var version = await _publisher.PublishAsync(
            RegistryNames.Pricing,
            PlatformPermissions.PricingPublish,
            Entity,
            command.Scope,
            clone.Id,
            $"Rolled pricing '{command.Scope}' back to v{command.ToVersion}, republished as v{nextVersion}.",
            before: Describe(source),
            after: Describe(clone),
            cancellationToken);

        return new RegistryWriteResponse(RegistryNames.Pricing, version, command.Scope, clone.Id);
    }

    /// <summary>
    /// Stands the currently published rule set for a scope down, if there is one.
    /// </summary>
    /// <remarks>
    /// The database enforces exactly one published rule set per scope with a partial unique index, so
    /// this is not tidiness — without it the very next <c>SaveChanges</c> raises a constraint
    /// violation and the publish fails with a 500 nobody can act on.
    /// <para>
    /// The constraint is the right one for money. "Which rules am I being charged under?" has to have
    /// exactly one answer; a highest-version tiebreak, which is fine for a workflow whose in-flight
    /// instances are pinned to their own version, would here mean two rule sets both claiming to be
    /// current and a race deciding which one billed a dealer.
    /// </para>
    /// </remarks>
    private async Task<RuleSet?> WithdrawCurrentAsync(string scope, CancellationToken cancellationToken)
    {
        var current = await _context.PricingRuleSets
            .FirstOrDefaultAsync(ruleSet => ruleSet.Scope == scope && ruleSet.IsPublished, cancellationToken);

        current?.Withdraw();
        return current;
    }

    private static Result<RuleSet> Build(string scope, int version, RuleSetBody body)
    {
        var ruleSet = RuleSet.Draft(scope, version, body.Name);

        foreach (var rule in body.Rules)
        {
            var built = new PricingRule(
                rule.Code, rule.Name, rule.Priority, rule.Condition?.ToJsonString(),
                rule.EffectiveFrom, rule.EffectiveTo, rule.Explanation);

            foreach (var component in rule.Components)
            {
                var basis = PricingWireNames.ParseBasis(component.Basis);
                if (basis.IsFailure)
                {
                    return Result.Failure<RuleSet>(basis.Error);
                }

                var feeComponent = new FeeComponent(
                    component.Code,
                    component.Label,
                    basis.Value,
                    component.Rate,
                    component.FlatMinorUnits,
                    component.MinMinorUnits,
                    component.MaxMinorUnits,
                    component.AppliesTo is { Count: > 0 } targets ? string.Join(',', targets) : null,
                    component.SortOrder);

                foreach (var slab in component.Slabs ?? [])
                {
                    feeComponent.AddSlab(new FeeSlab(
                        slab.FromMinorUnits, slab.ToMinorUnits, slab.Rate, slab.FlatMinorUnits));
                }

                built.AddComponent(feeComponent);
            }

            ruleSet.AddRule(built);
        }

        return ruleSet;
    }

    private static JsonObject Describe(RuleSet ruleSet)
        => new JsonObject
        {
            ["scope"] = ruleSet.Scope,
            ["version"] = ruleSet.Version,
            ["rules"] = ruleSet.Rules.Count,
            ["isPublished"] = ruleSet.IsPublished,
        };

    private async Task<int> NextVersionAsync(string scope, CancellationToken cancellationToken)
    {
        var highest = await _context.PricingRuleSets
            .Where(ruleSet => ruleSet.Scope == scope)
            .MaxAsync(ruleSet => (int?)ruleSet.Version, cancellationToken);

        return (highest ?? 0) + 1;
    }

    private async Task<RuleSet?> LoadAsync(string scope, int version, CancellationToken cancellationToken)
        => await _context.PricingRuleSets
            .Include($"{nameof(RuleSet.Rules)}.{nameof(PricingRule.Components)}.{nameof(FeeComponent.Slabs)}")
            .AsSplitQuery()
            .FirstOrDefaultAsync(
                ruleSet => ruleSet.Scope == scope && ruleSet.Version == version,
                cancellationToken);
}
