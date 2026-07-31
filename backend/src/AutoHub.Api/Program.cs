using AutoHub.Api.Application.Auth;
using AutoHub.Api.Application.RegistryAdmin;
using AutoHub.Api.Application.Workflow;
using AutoHub.Api.Endpoints;
using AutoHub.Api.Extensions;
using AutoHub.Api.Middleware;
using AutoHub.Api.Security;
using AutoHub.Modules.Auction;
using AutoHub.Modules.Bookings;
using AutoHub.Modules.Catalog.Infrastructure;
using AutoHub.Modules.Deals;
using AutoHub.Modules.Listings.Infrastructure;
using AutoHub.Modules.Payments;
using AutoHub.Modules.Valuation.Infrastructure;
using AutoHub.Persistence;
using AutoHub.Persistence.Seed;
using Scalar.AspNetCore;
using Serilog;

var builder = WebApplication.CreateBuilder(args);

builder.AddObservability();

// The database, the interceptors, the registry stores and the outbox processor. Everything else the
// API needs is layered on top of this by the modules that own it.
builder.Services.AddPersistence(builder.Configuration);

builder.AddPlatformRegistries();
builder.AddApiSecurity();
builder.AddApplicationServices();
builder.AddHealthChecks();

// The domain modules. Each registers its own handlers, validators, contract implementation,
// workflow effects and permission declarations; the persistence layer above has already supplied
// their repositories. Registering them here rather than inside AddPersistence keeps the direction of
// the dependency honest — the host composes the modules, the modules do not compose each other.
builder.Services.AddCatalogModule();
builder.Services.AddListingsModule();
builder.Services.AddValuationModule();

// The transaction spine. Deals settle what auction, bookings and listings agree; payments post what
// deals settle. Registered after the three above and in this order for readability only — the
// container resolves by type, and none of these four reaches into another's Domain or Application.
builder.Services.AddAuctionModule();
builder.Services.AddDealsModule();
builder.Services.AddPaymentsModule();
builder.Services.AddBookingsModule(
    builder.Configuration.GetSection(HandoverOptions.SectionName).Get<HandoverOptions>()?.Validated()
    ?? new HandoverOptions().Validated());

builder.Services.AddScoped<SessionFactory>();
builder.Services.AddScoped<RegistryPublisher>();
builder.Services.AddScoped<WorkflowRouteResolver>();
builder.Services.AddScoped<IOtpDeliveryChannel, LoggingOtpDeliveryChannel>();

builder.Services.AddExceptionHandler<UnhandledExceptionHandler>();

// Registered so the framework's own failures — an unroutable path, a body it cannot bind — come
// back in the same envelope as ours rather than as an HTML error page a JSON client cannot parse.
builder.Services.AddProblemDetails();

builder.Services.AddOpenApi();

// Gated on the environment rather than on a configuration flag, and gated at registration rather
// than at the call site. Seed data is a staged marketplace — real plates, fabricated transactions —
// and the failure worth engineering against is not "somebody set the flag wrong" but "the flag
// existed at all". In a non-Development host the seeder is never even in the container.
if (builder.Environment.IsDevelopment())
{
    builder.Services.AddDevelopmentDataSeeding();
}

var app = builder.Build();

app.UseExceptionHandler();
app.UseObservability();

// Explicit, because everything after it needs the matched endpoint: the idempotency middleware reads
// the route template for its ledger key and the exemption metadata for whether to require a key at
// all, and both are null until routing has run.
app.UseRouting();

app.UseApiSecurity();
app.UseMiddleware<IdempotencyMiddleware>();

app.MapHealthEndpoints();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();

    // Scalar mounts at /scalar/{documentName}; /scalar alone is what everybody actually types.
    app.MapScalarApiReference(options => options
        .WithTitle("AutoHub API")
        .WithTheme(ScalarTheme.BluePlanet));

    app.MapGet("/scalar", () => Results.Redirect("/scalar/v1")).ExcludeFromDescription();
}

// Everything the contract publishes lives under /api/v1 — the base URL the shipped API client
// defaults to. The version is in the path rather than in a header because a URL is the thing that
// ends up in a log, a bug report and a partner's integration document.
var v1 = app.MapGroup("/api/v1");

v1.MapAuthEndpoints();
v1.MapPlatformEndpoints();
v1.MapRegistryAdminEndpoints();
v1.MapCatalogEndpoints();
v1.MapListingEndpoints();
v1.MapValuationEndpoints();

// Last. The generic {module}/{resource} pattern is broad enough to shadow a module's own routes,
// and while ASP.NET Core's route precedence prefers literal segments to parameters, registration
// order is the belt to that braces.
v1.MapWorkflowEndpoints();

try
{
    // Before the first request, not in the background. A seeder racing the first request answers
    // /platform/taxonomy/tree with an empty tree, the web app caches that against its ETag, and a
    // developer loses twenty minutes to categories that are visibly in the database.
    if (app.Environment.IsDevelopment())
    {
        await app.SeedDevelopmentDataAsync();
    }

    app.Run();
}
catch (Exception ex) when (ex is not OperationCanceledException)
{
    // Without this, a failure during startup — a missing signing key, an unreachable database at
    // migration time — is written by the default logger to a console nobody is tailing, and the
    // container simply exits with a status code and no explanation.
    Log.Fatal(ex, "AutoHub API terminated unexpectedly");
    throw;
}
finally
{
    Log.CloseAndFlush();
}

/// <summary>
/// Exposed so an integration test host can reference this assembly's entry point.
/// </summary>
/// <remarks>
/// A top-level program's generated class is internal, and <c>WebApplicationFactory&lt;T&gt;</c>
/// needs a public type to hang off. Declaring the partial here is the documented way to do that
/// without making anything else public.
/// </remarks>
public partial class Program;
