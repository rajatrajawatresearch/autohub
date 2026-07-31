using System.Text.Json.Nodes;
using AutoHub.Api.Application.Platform;
using AutoHub.Api.Http;
using AutoHub.Api.Middleware;
using AutoHub.Platform.Caching;
using AutoHub.Platform.Forms;
using AutoHub.SharedKernel.Results;
using MediatR;

namespace AutoHub.Api.Endpoints;

/// <summary>
/// The registry plane — everything the product refuses to hardcode, served over HTTP.
/// </summary>
/// <remarks>
/// Anonymous by design. The taxonomy, the sell form, the workflow vocabulary and the city list are
/// all needed before a user has signed in — the homepage renders a category picker and a valuation
/// form to a first-time visitor — and requiring a token would put a sign-in wall in front of the
/// funnel. None of it is confidential: it is the same data every client renders.
/// <para>
/// <c>/platform/features</c> is the exception in behaviour rather than in policy. It is anonymous
/// too, but it is evaluated for the caller, so an authenticated request gets a different answer than
/// an anonymous one — which is why it varies on more than the URL.
/// </para>
/// </remarks>
internal static class PlatformEndpoints
{
    public static IEndpointRouteBuilder MapPlatformEndpoints(this IEndpointRouteBuilder app)
    {
        var platform = app.MapGroup("/platform").WithTags("Platform registries").AllowAnonymous();

        MapTaxonomy(platform);
        MapForms(platform);
        MapWorkflows(platform);
        MapPricing(platform);
        MapFeatures(platform);
        MapLocalization(platform);
        MapGeography(platform);

        return app;
    }

    private static void MapTaxonomy(IEndpointRouteBuilder platform)
    {
        var taxonomy = platform.MapGroup("/taxonomy");

        taxonomy.MapGet("/tree", async (ISender sender, HttpContext context, CancellationToken ct) =>
            RegistryResponse.From(
                context,
                await sender.Send(new GetTaxonomyTreeQuery(), ct),
                RegistryNames.Taxonomy,
                tree => tree.Version))
            .WithName("GetTaxonomyTree")
            .WithSummary("The category tree with each node's attribute codes.");

        taxonomy.MapGet("/attributes", async (
                string category,
                ISender sender,
                HttpContext context,
                CancellationToken ct) =>
            RegistryResponse.From(
                context,
                await sender.Send(new GetCategoryAttributesQuery(category), ct),
                RegistryNames.Taxonomy,
                attributes => attributes.Version))
            .WithName("GetCategoryAttributes")
            .WithSummary("The attribute definitions a category exposes, inheritance already resolved.");

        taxonomy.MapGet("/makes", async (
                string? category,
                ISender sender,
                HttpContext context,
                CancellationToken ct) =>
            RegistryResponse.From(
                context,
                await sender.Send(new GetMakesQuery(category), ct),
                RegistryNames.Taxonomy,
                makes => makes.Version))
            .WithName("GetMakes");

        taxonomy.MapGet("/models", async (
                string make,
                ISender sender,
                HttpContext context,
                CancellationToken ct) =>
            RegistryResponse.From(
                context,
                await sender.Send(new GetModelsQuery(make), ct),
                RegistryNames.Taxonomy,
                models => models.Version))
            .WithName("GetModels");

        taxonomy.MapGet("/variants", async (
                string model,
                int? year,
                ISender sender,
                HttpContext context,
                CancellationToken ct) =>
            RegistryResponse.From(
                context,
                await sender.Send(new GetVariantsQuery(model, year), ct),
                RegistryNames.Taxonomy,
                variants => variants.Version))
            .WithName("GetVariants");
    }

    private static void MapForms(IEndpointRouteBuilder platform)
    {
        platform.MapGet("/forms/{key}", async (
                string key,
                string? context,
                ISender sender,
                HttpContext http,
                IRegistryCache cache,
                CancellationToken ct) =>
            {
                var form = await sender.Send(new GetFormQuery(key, context), ct);

                if (form.IsFailure)
                {
                    return ApiProblem.Result(form.Error, http);
                }

                // Serialised by the form registry's own options, which omit nulls — the renderer
                // treats a present-but-null `options` as an empty dropdown rather than as "not that
                // kind of field". Round-tripping through the host serializer would put them back.
                //
                // The ETag is the form's own version, not the forms registry generation: a client
                // holding v7 of the sell form should revalidate to a 304 when an unrelated form is
                // republished.
                return RegistryResponse.RawJson(
                    http,
                    new RegistryVersion($"{RegistryNames.Forms}:{key}", form.Value.Version),
                    FormJson.Serialize(form.Value));
            })
            .WithName("GetForm")
            .WithSummary("The published form schema for a key, in the variant that fits the context.");

        platform.MapPost("/forms/{key}/validate", async (
                string key,
                string? context,
                bool? complete,
                JsonObject payload,
                ISender sender,
                HttpContext http,
                CancellationToken ct) =>
            {
                var outcome = await sender.Send(
                    new ValidateFormCommand(key, context, payload, complete ?? false), ct);

                // 200 {"valid":true} or the 422 with metadata.fieldErrors that the contract fixes —
                // and the field errors come from the form's own rule codes, never from codes this
                // endpoint invented, so web and Flutter report the same thing.
                return outcome.IsSuccess
                    ? Results.Ok(new { valid = true })
                    : ApiProblem.Result(outcome.Error, http);
            })
            .WithName("ValidateForm")
            .WithMetadata(new IdempotencyExemptAttribute())
            .WithSummary("Server-side validation of a partial or complete payload.");
    }

    private static void MapWorkflows(IEndpointRouteBuilder platform)
        => platform.MapGet("/workflows/{key}", async (
                string key,
                ISender sender,
                HttpContext context,
                CancellationToken ct) =>
            {
                var definition = await sender.Send(new GetWorkflowDefinitionQuery(key), ct);

                return definition.IsSuccess
                    ? RegistryResponse.Json(
                        context,
                        new RegistryVersion($"{RegistryNames.Workflow}:{key}", definition.Value.Version),
                        definition.Value)
                    : ApiProblem.Result(definition.Error, context);
            })
            .WithName("GetWorkflowDefinition")
            .WithSummary("The states and transitions of a published workflow.");

    private static void MapPricing(IEndpointRouteBuilder platform)
        => platform.MapPost("/pricing/quote", async (
                PriceQuoteRequestBody body,
                ISender sender,
                HttpContext context,
                CancellationToken ct) =>
            {
                var quote = await sender.Send(
                    new GetPriceQuoteCommand(body.Scope, body.Context, body.Amount), ct);

                // Never cached. A quote depends on the caller, the city, the party's tier and the
                // instant it was asked for; an ETag on it would be a promise the pricing registry
                // cannot keep.
                return quote.IsSuccess
                    ? Results.Ok(quote.Value)
                    : ApiProblem.Result(quote.Error, context);
            })
            .WithName("GetPriceQuote")
            .WithMetadata(new IdempotencyExemptAttribute())
            .WithSummary("Prices one commercial moment. Nothing in the product computes a fee itself.");

    private static void MapFeatures(IEndpointRouteBuilder platform)
        => platform.MapGet("/features", async (ISender sender, HttpContext context, CancellationToken ct) =>
            {
                var features = await sender.Send(new GetFeaturesQuery(), ct);

                if (features.IsFailure)
                {
                    return ApiProblem.Result(features.Error, context);
                }

                // Private, not public. The answer is evaluated for this caller, so a shared cache
                // holding it would serve one user's enabled modules to everybody behind the proxy.
                context.Response.Headers.CacheControl = "private, max-age=30";
                context.Response.Headers[RegistryResponse.VersionHeader] =
                    features.Value.Version.ToString(System.Globalization.CultureInfo.InvariantCulture);

                return Results.Ok(features.Value);
            })
            .WithName("GetFeatures")
            .WithSummary("Modules and flags, evaluated server-side for the calling user.");

    private static void MapLocalization(IEndpointRouteBuilder platform)
        => platform.MapGet("/i18n/{locale}", async (
                string locale,
                string? @namespace,
                ISender sender,
                HttpContext context,
                CancellationToken ct) =>
            RegistryResponse.From(
                context,
                await sender.Send(new GetI18nBundleQuery(locale, @namespace ?? "web"), ct),
                $"{RegistryNames.I18n}:{locale}",
                bundle => bundle.Version))
            .WithName("GetI18nBundle")
            .WithSummary("A translation bundle, already merged down its fallback chain.");

    private static void MapGeography(IEndpointRouteBuilder platform)
        => platform.MapGet("/geo/cities", async (
                bool? serviceable,
                string? module,
                ISender sender,
                HttpContext context,
                CancellationToken ct) =>
            {
                // `serviceable` is accepted because the contract publishes it, and ignored because
                // the registry only ever returns serviceable cities — a non-serviceable city is not
                // a place the product operates and has nothing to render. Accepting and ignoring is
                // kinder than a 400 on a parameter every client already sends.
                _ = serviceable;

                return RegistryResponse.From(
                    context,
                    await sender.Send(new GetCitiesQuery(module), ct),
                    RegistryNames.Geography,
                    cities => cities.Version);
            })
            .WithName("GetCities")
            .WithSummary("Cities the business operates in, optionally narrowed to one module.");
}

/// <summary>The request body of <c>POST /platform/pricing/quote</c>.</summary>
/// <remarks>
/// <see cref="Amount"/> is accepted here as well as inside <see cref="Context"/>, where §4 of the
/// contract nests it. The handler folds a top-level amount into the context before the engine sees
/// either, so a rule banding on the amount matches identically whichever position the caller chose —
/// which is the property that makes accepting both safe rather than merely lenient.
/// </remarks>
public sealed record PriceQuoteRequestBody(string Scope, JsonObject? Context, JsonObject? Amount);
