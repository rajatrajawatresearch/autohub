using AutoHub.Modules.Valuation.Application.Abstractions;
using AutoHub.Modules.Valuation.Application.RequestValuation;
using AutoHub.Modules.Valuation.Contracts;
using AutoHub.Platform.Access;
using FluentValidation;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace AutoHub.Modules.Valuation.Infrastructure;

/// <summary>
/// The permissions the valuation module checks.
/// </summary>
/// <remarks>
/// Requesting a quote is deliberately not one of them. §10 of the contract is explicit that the
/// first quote needs no auth, because asking for a phone number before showing a number is why users
/// bounce — and a permission on the homepage hero would be exactly that gate wearing a different hat.
/// <para>
/// What is permissioned is changing the coefficients, and it is dangerous for the same reason
/// publishing a pricing registry is: whoever holds it decides what every seller in India is told
/// their car is worth.
/// </para>
/// </remarks>
public static class ValuationPermissions
{
    public const string ModelPublish = "registry.valuation_model.publish";

    /// <summary>Seeing the adjustment ledger and the model version behind a quote. Support and audit.</summary>
    public const string QuoteInspect = "valuation.quote.inspect";
}

/// <summary>Declares <see cref="ValuationPermissions"/> to the RBAC catalog.</summary>
public sealed class ValuationPermissionContributor : IPermissionContributor
{
    public IReadOnlyList<PermissionDefinition> Permissions { get; } =
    [
        PermissionDefinition.Declare(
            ValuationPermissions.ModelPublish, "Publish valuation models", isDangerous: true,
            description: "Change the coefficients every price on the platform is derived from."),

        PermissionDefinition.Declare(
            ValuationPermissions.QuoteInspect, "Inspect a quote",
            description: "See the adjustment ledger and model version behind a published price."),
    ];
}

/// <summary>
/// Everything the valuation module needs from the container.
/// </summary>
/// <remarks>
/// Both models are registered unconditionally, and which of them answers a request is decided at
/// runtime by the integration registry — that is the entire point of the routing indirection. A
/// build that registered only the model currently in use would make promotion a deployment again.
/// </remarks>
public static class ValuationModule
{
    public static IServiceCollection AddValuationModule(this IServiceCollection services)
    {
        services.AddMediatR(configuration =>
            configuration.RegisterServicesFromAssemblyContaining<RequestValuationCommand>());

        services.AddScoped<IValidator<RequestValuationCommand>, RequestValuationValidator>();

        // Registered as IValuationModel rather than by concrete type: the router indexes them by
        // ModelKey, so adding a third model is a registration here and a route row, with no change
        // to anything that asks for a price.
        services.AddScoped<IValuationModel, HedonicValuationModel>();
        services.AddScoped<IValuationModel, MlValuationModel>();

        services.TryAddScoped<IValuationModelRouter, ValuationModelRouter>();
        services.TryAddScoped<IValuationDirectory, ValuationDirectory>();

        services.AddSingleton<IPermissionContributor, ValuationPermissionContributor>();

        return services;
    }
}
