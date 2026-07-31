using System.Globalization;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;
using Serilog;
using Serilog.Events;

namespace AutoHub.Api.Extensions;

internal static class ObservabilityExtensions
{
    /// <summary>The OTLP collector endpoint. Absent means "collect in process, export nowhere".</summary>
    private const string OtlpEndpointKey = "Observability:Otlp:Endpoint";

    /// <summary>
    /// Structured console logging, plus traces and metrics.
    /// </summary>
    /// <remarks>
    /// Serilog rather than the default logger for one reason that matters in this codebase: the
    /// registry endpoints log a version and a registry name on every publish, and a log pipeline
    /// that flattens those into a formatted sentence makes "show me every pricing publish this
    /// month" a grep instead of a query.
    /// </remarks>
    public static WebApplicationBuilder AddObservability(this WebApplicationBuilder builder)
    {
        builder.Services.AddSerilog((_, configuration) => configuration
            .ReadFrom.Configuration(builder.Configuration)
            .Enrich.FromLogContext()
            .Enrich.WithProperty("service", ServiceName)
            .MinimumLevel.Override("Microsoft.AspNetCore", LogEventLevel.Warning)
            .WriteTo.Console(
                outputTemplate:
                "[{Timestamp:HH:mm:ss} {Level:u3}] {Message:lj} {Properties:j}{NewLine}{Exception}",
                formatProvider: CultureInfo.InvariantCulture));

        var otlpEndpoint = builder.Configuration[OtlpEndpointKey]
            ?? Environment.GetEnvironmentVariable("OTEL_EXPORTER_OTLP_ENDPOINT");

        builder.Services.AddOpenTelemetry()
            .ConfigureResource(resource => resource.AddService(
                serviceName: ServiceName,
                serviceVersion: typeof(ObservabilityExtensions).Assembly.GetName().Version?.ToString()))
            .WithTracing(tracing =>
            {
                tracing
                    .AddAspNetCoreInstrumentation(options =>
                    {
                        // The probes run every few seconds forever. Tracing them buries every real
                        // request in noise and costs money per span at any collector worth using.
                        options.Filter = context =>
                            !context.Request.Path.StartsWithSegments("/health", StringComparison.Ordinal);
                    })
                    .AddHttpClientInstrumentation()

                    // The activity source Npgsql publishes. Named rather than pulled in through
                    // Npgsql.OpenTelemetry's extension method, whose name collides with the
                    // DbContext registration extension of the same name on IServiceCollection.
                    .AddSource("Npgsql");

                AddOtlpIfConfigured(tracing, otlpEndpoint);
            })
            .WithMetrics(metrics =>
            {
                metrics
                    .AddAspNetCoreInstrumentation()
                    .AddHttpClientInstrumentation();

                AddOtlpIfConfigured(metrics, otlpEndpoint);
            });

        return builder;
    }

    /// <summary>
    /// Request logging, at one line per request rather than the framework's four.
    /// </summary>
    /// <remarks>
    /// The probe filter here is the same decision as the tracing filter, for the same reason: a
    /// readiness check every two seconds is 43,000 log lines a day per node that nobody will ever
    /// read and that push the lines somebody does need out of the retention window.
    /// </remarks>
    public static WebApplication UseObservability(this WebApplication app)
    {
        app.UseSerilogRequestLogging(options =>
        {
            options.GetLevel = (context, _, exception) =>
                exception is not null || context.Response.StatusCode >= 500 ? LogEventLevel.Error
                : context.Request.Path.StartsWithSegments("/health", StringComparison.Ordinal)
                    ? LogEventLevel.Verbose
                    : LogEventLevel.Information;

            options.EnrichDiagnosticContext = (diagnostic, context) =>
            {
                diagnostic.Set("requestHost", context.Request.Host.Value ?? string.Empty);
                diagnostic.Set("userId", context.User.FindFirst("sub")?.Value ?? string.Empty);
            };
        });

        return app;
    }

    private const string ServiceName = "autohub-api";

    /// <summary>
    /// Adds the OTLP exporter only when somebody has said where to send data.
    /// </summary>
    /// <remarks>
    /// Registering it unconditionally is the usual advice and is wrong for a service that must boot
    /// on a laptop: the exporter retries against <c>localhost:4317</c>, and every failed export is a
    /// log line, a background allocation and a shutdown delay while the SDK flushes a queue that
    /// will never drain. Absent configuration means telemetry is still collected in process — so
    /// <c>Activity.Current</c> is populated and every problem response carries a real trace id — and
    /// simply goes nowhere.
    /// </remarks>
    private static void AddOtlpIfConfigured(TracerProviderBuilder tracing, string? endpoint)
    {
        if (!string.IsNullOrWhiteSpace(endpoint))
        {
            tracing.AddOtlpExporter(options => options.Endpoint = new Uri(endpoint));
        }
    }

    private static void AddOtlpIfConfigured(MeterProviderBuilder metrics, string? endpoint)
    {
        if (!string.IsNullOrWhiteSpace(endpoint))
        {
            metrics.AddOtlpExporter(options => options.Endpoint = new Uri(endpoint));
        }
    }
}
