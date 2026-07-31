using System.Reflection;
using AutoHub.Modules.Payments.Application;
using AutoHub.Modules.Payments.Domain.Ledger;
using AutoHub.Modules.Payments.Integration;
using AutoHub.Platform.Access;
using AutoHub.Platform.Integrations;
using AutoHub.Platform.Workflow;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace AutoHub.Modules.Payments;

/// <summary>
/// Registers the payments module.
/// </summary>
/// <remarks>
/// The repositories are supplied by the persistence layer; this project never references EF Core.
/// The development gateway adapters are registered unconditionally and are still inert unless a
/// route in the integration registry points at them and their config says <c>"mode": "development"</c>
/// — registration is not enablement, which is what lets one build serve a laptop and production.
/// </remarks>
public static class PaymentsModule
{
    public static IServiceCollection AddPaymentsModule(this IServiceCollection services)
    {
        services.AddMediatR(configuration =>
            configuration.RegisterServicesFromAssembly(Assembly.GetExecutingAssembly()));

        services.TryAddScoped<ILedgerPostingService, LedgerPostingService>();
        services.TryAddScoped<IWalletProjector, WalletProjector>();
        services.TryAddScoped<IPaymentGateway, PaymentGateway>();

        // The workflow registry's vocabulary for moving money. An operator wires it to whichever
        // transitions should post, with the scope as the effect's argument.
        services.AddScoped<IWorkflowEffectHandler, DealJournalEffectHandler>();

        services.AddSingleton<IIntegrationAdapter, DevelopmentPaymentCollectAdapter>();
        services.AddSingleton<IIntegrationAdapter<
            Platform.Integrations.PaymentCollect.PaymentCollectRequest,
            Platform.Integrations.PaymentCollect.PaymentCollectResponse>, DevelopmentPaymentCollectAdapter>();
        services.AddSingleton<IIntegrationAdapter, DevelopmentPaymentPayoutAdapter>();
        services.AddSingleton<IIntegrationAdapter<PaymentPayoutRequest, PaymentPayoutResponse>,
            DevelopmentPaymentPayoutAdapter>();

        // A fallback so a fresh database still posts correctly. Replaced by the EF-backed store the
        // moment the persistence layer registers one, because operations must be able to edit these.
        services.TryAddScoped<IPostingTemplateStore, CatalogPostingTemplateStore>();

        services.AddSingleton<IPermissionContributor, PaymentPermissionContributor>();

        return services;
    }
}

/// <summary>
/// Serves the default posting templates from code when the database has none.
/// </summary>
/// <remarks>
/// The alternative — an empty template set — makes every posting fail with
/// <c>ledger.posting_scope_unconfigured</c> on a fresh environment, which reads as a broken build
/// rather than as missing seed data and costs an afternoon every time somebody clones the repo.
/// <para>
/// It is registered with <c>TryAdd</c> so the persistence layer's store wins wherever one exists.
/// This is a floor, not a policy: the moment finance edits a template, the database is the answer.
/// </para>
/// </remarks>
public sealed class CatalogPostingTemplateStore : IPostingTemplateStore
{
    private static readonly IReadOnlyList<PostingTemplate> Templates = PostingTemplateCatalog.DefaultTemplates();

    public Task<IReadOnlyList<PostingTemplate>> GetForScopeAsync(
        string scopeCode,
        CancellationToken cancellationToken)
        => Task.FromResult<IReadOnlyList<PostingTemplate>>(
            Templates
                .Where(t => string.Equals(t.ScopeCode, scopeCode, StringComparison.Ordinal))
                .ToList());
}
