using AutoHub.Api.Application.Platform;
using AutoHub.Api.Application.RegistryAdmin;
using AutoHub.Api.Http;
using AutoHub.Platform.Access;
using AutoHub.SharedKernel.Results;
using MediatR;

namespace AutoHub.Api.Endpoints;

/// <summary>
/// The write side the admin console needs.
/// </summary>
/// <remarks>
/// <b>Every route here names the permission it requires, and every publish writes an audit row.</b>
/// Those two facts are the reason the prime directive is safe: a registry that anyone can change
/// without a trace is not configuration, it is an unlogged deployment mechanism. The permission is a
/// code from <see cref="PlatformPermissions"/> — never a role — and the policy for it is
/// materialised on demand by the permission policy provider.
/// <para>
/// The vocabulary is deliberately uniform across registries: <c>POST</c> to create a draft,
/// <c>POST .../publish</c> to make it live, <c>POST .../rollback</c> to restore an earlier version
/// by republishing it forward. An operator who has learned one registry has learned all of them.
/// </para>
/// </remarks>
internal static class RegistryAdminEndpoints
{
    public static IEndpointRouteBuilder MapRegistryAdminEndpoints(this IEndpointRouteBuilder app)
    {
        var admin = app.MapGroup("/admin/registry").WithTags("Registry admin");

        MapDiagnostics(admin);
        MapTaxonomy(admin);
        MapForms(admin);
        MapWorkflows(admin);
        MapPricing(admin);
        MapFeatures(admin);
        MapContent(admin);

        return app;
    }

    private static void MapDiagnostics(IEndpointRouteBuilder admin)
    {
        admin.MapGet("/versions", async (ISender sender, HttpContext context, CancellationToken ct) =>
                Respond(await sender.Send(new GetRegistryVersionsQuery(), ct), context))
            .RequireAuthorization(PlatformPermissions.AccessView)
            .WithName("GetRegistryVersions")
            .WithSummary("Every registry's current generation, for the cache panel.");

        admin.MapPost("/{registry}/invalidate", async (
                string registry,
                ISender sender,
                HttpContext context,
                CancellationToken ct) =>
                Respond(await sender.Send(new InvalidateRegistryCommand(registry), ct), context))
            .RequireAuthorization(PlatformPermissions.CacheInvalidate)
            .WithName("InvalidateRegistry")
            .WithSummary("Forces a rebuild of one registry's cache. Diagnostic; audited.");
    }

    private static void MapTaxonomy(IEndpointRouteBuilder admin)
    {
        var taxonomy = admin.MapGroup("/taxonomy")
            .RequireAuthorization(PlatformPermissions.TaxonomyPublish);

        taxonomy.MapPost("/categories", async (
                CreateCategoryBody body,
                ISender sender,
                HttpContext context,
                CancellationToken ct) =>
                Respond(await sender.Send(new CreateCategoryCommand(body), ct), context))
            .WithName("CreateCategory");

        taxonomy.MapPost("/attributes", async (
                CreateAttributeBody body,
                ISender sender,
                HttpContext context,
                CancellationToken ct) =>
                Respond(await sender.Send(new CreateAttributeCommand(body), ct), context))
            .WithName("CreateAttribute")
            .WithSummary("Defines an attribute. It becomes a form field, a facet and a spec row.");

        taxonomy.MapPost("/categories/{code}/attributes", async (
                string code,
                AttachAttributeBody body,
                ISender sender,
                HttpContext context,
                CancellationToken ct) =>
                Respond(await sender.Send(new AttachAttributeCommand(code, body), ct), context))
            .WithName("AttachAttribute")
            .WithSummary("Attaches an attribute to a category, with optional per-category overrides.");
    }

    private static void MapForms(IEndpointRouteBuilder admin)
    {
        var forms = admin.MapGroup("/forms").RequireAuthorization(PlatformPermissions.FormsPublish);

        forms.MapPost("/", async (
                CreateFormBody body,
                ISender sender,
                HttpContext context,
                CancellationToken ct) =>
                Respond(await sender.Send(new CreateFormCommand(body), ct), context))
            .WithName("CreateForm");

        forms.MapPost("/{key}/versions", async (
                string key,
                FormVersionBody body,
                ISender sender,
                HttpContext context,
                CancellationToken ct) =>
                Respond(await sender.Send(new AddFormVersionCommand(key, body), ct), context))
            .WithName("AddFormVersion")
            .WithSummary("Drafts the next version. Invisible to clients until published.");

        forms.MapPost("/{key}/versions/{version:int}/publish", async (
                string key,
                int version,
                ISender sender,
                HttpContext context,
                CancellationToken ct) =>
                Respond(await sender.Send(new PublishFormVersionCommand(key, version), ct), context))
            .WithName("PublishFormVersion")
            .WithSummary("Validates and freezes a version, then serves it to web, Android and iOS.");

        forms.MapPost("/{key}/rollback", async (
                string key,
                RollbackBody body,
                ISender sender,
                HttpContext context,
                CancellationToken ct) =>
                Respond(await sender.Send(new RollbackFormCommand(key, body.ToVersion), ct), context))
            .WithName("RollbackForm")
            .WithSummary("Republishes an earlier version's document as the next version.");
    }

    private static void MapWorkflows(IEndpointRouteBuilder admin)
    {
        var workflows = admin.MapGroup("/workflows")
            .RequireAuthorization(PlatformPermissions.WorkflowPublish);

        workflows.MapGet("/{key}/versions/{version:int}", async (
                string key,
                int version,
                ISender sender,
                HttpContext context,
                CancellationToken ct) =>
                Respond(await sender.Send(new GetWorkflowVersionQuery(key, version), ct), context))
            .WithName("GetWorkflowVersion")
            .WithSummary("Reads one version, published or not, for the editor.");

        workflows.MapPost("/{key}/versions", async (
                string key,
                WorkflowVersionBody body,
                ISender sender,
                HttpContext context,
                CancellationToken ct) =>
                Respond(await sender.Send(new CreateWorkflowVersionCommand(key, body), ct), context))
            .WithName("CreateWorkflowVersion");

        workflows.MapPost("/{key}/versions/{version:int}/publish", async (
                string key,
                int version,
                ISender sender,
                HttpContext context,
                CancellationToken ct) =>
                Respond(await sender.Send(new PublishWorkflowVersionCommand(key, version), ct), context))
            .WithName("PublishWorkflowVersion")
            .WithSummary("Proves the definition is runnable, then makes it live for new instances.");

        workflows.MapPost("/{key}/rollback", async (
                string key,
                RollbackBody body,
                ISender sender,
                HttpContext context,
                CancellationToken ct) =>
                Respond(await sender.Send(new RollbackWorkflowCommand(key, body.ToVersion), ct), context))
            .WithName("RollbackWorkflow")
            .WithSummary("Republishes an earlier version forward. Running instances are untouched.");
    }

    private static void MapPricing(IEndpointRouteBuilder admin)
    {
        var pricing = admin.MapGroup("/pricing")
            .RequireAuthorization(PlatformPermissions.PricingPublish);

        pricing.MapPost("/{scope}/versions", async (
                string scope,
                RuleSetBody body,
                ISender sender,
                HttpContext context,
                CancellationToken ct) =>
                Respond(await sender.Send(new CreateRuleSetCommand(scope, body), ct), context))
            .WithName("CreateRuleSet");

        pricing.MapPost("/{scope}/versions/{version:int}/publish", async (
                string scope,
                int version,
                ISender sender,
                HttpContext context,
                CancellationToken ct) =>
                Respond(await sender.Send(new PublishRuleSetCommand(scope, version), ct), context))
            .WithName("PublishRuleSet")
            .WithSummary("Changes what every dealer is charged. Dangerous by design; always audited.");

        pricing.MapPost("/{scope}/rollback", async (
                string scope,
                RollbackBody body,
                ISender sender,
                HttpContext context,
                CancellationToken ct) =>
                Respond(await sender.Send(new RollbackRuleSetCommand(scope, body.ToVersion), ct), context))
            .WithName("RollbackRuleSet");
    }

    private static void MapFeatures(IEndpointRouteBuilder admin)
    {
        var features = admin.MapGroup("/features")
            .RequireAuthorization(PlatformPermissions.FeaturesManage);

        features.MapPost("/flags", async (
                UpsertFlagBody body,
                ISender sender,
                HttpContext context,
                CancellationToken ct) =>
                Respond(await sender.Send(new UpsertFlagCommand(body), ct), context))
            .WithName("UpsertFlag")
            .WithSummary("Creates or replaces a flag and its targeting rules.");

        features.MapPost("/modules", async (
                UpsertModuleBody body,
                ISender sender,
                HttpContext context,
                CancellationToken ct) =>
                Respond(await sender.Send(new UpsertModuleCommand(body), ct), context))
            .WithName("UpsertModule")
            .WithSummary("Turns a whole module on or off, optionally behind a flag.");
    }

    private static void MapContent(IEndpointRouteBuilder admin)
    {
        admin.MapPost("/i18n", async (
                UpsertI18nBody body,
                ISender sender,
                HttpContext context,
                CancellationToken ct) =>
                Respond(await sender.Send(new UpsertI18nCommand(body), ct), context))
            .RequireAuthorization(PlatformPermissions.I18nPublish)
            .WithName("UpsertI18nResources")
            .WithSummary("Publishes a batch of strings for one locale and namespace.");

        admin.MapPost("/templates", async (
                UpsertTemplateBody body,
                ISender sender,
                HttpContext context,
                CancellationToken ct) =>
                Respond(await sender.Send(new UpsertTemplateCommand(body), ct), context))
            .RequireAuthorization(PlatformPermissions.NotificationsPublish)
            .WithName("UpsertNotificationTemplate")
            .WithSummary("Creates or updates a message template for one channel and locale.");
    }

    private static IResult Respond<T>(Result<T> result, HttpContext context)
        => result.IsSuccess
            ? Results.Ok(result.Value)
            : ApiProblem.Result(result.Error, context);
}

/// <summary>The one-field body every rollback endpoint takes.</summary>
public sealed record RollbackBody(int ToVersion);
