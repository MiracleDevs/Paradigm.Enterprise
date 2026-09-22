using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using OpenTelemetry.Logs;
using OpenTelemetry.Metrics;
using OpenTelemetry.Trace;

namespace Paradigm.Enterprise.Aspire.ServiceDefaults;

/// <summary>
/// Provides the baseline cross-cutting service registrations used by Paradigm applications.
/// </summary>
public static class ServiceDefaultsExtensions
{
    /// <summary>
    /// Adds telemetry, service discovery, resilient HTTP clients, and liveness health checks.
    /// </summary>
    /// <param name="builder">The host application builder.</param>
    /// <returns>The supplied application builder.</returns>
    public static IHostApplicationBuilder AddParadigmServiceDefaults(this IHostApplicationBuilder builder)
    {
        builder.Logging.AddOpenTelemetry();
        builder.Services.Configure<OpenTelemetryLoggerOptions>(logging => logging.AddOtlpExporter());

        builder.Services.AddOpenTelemetry()
            .WithMetrics(metrics => metrics
                .AddRuntimeInstrumentation()
                .AddAspNetCoreInstrumentation()
                .AddHttpClientInstrumentation()
                .AddOtlpExporter())
            .WithTracing(tracing => tracing
                .AddSource(builder.Environment.ApplicationName)
                .AddAspNetCoreInstrumentation()
                .AddHttpClientInstrumentation()
                .AddOtlpExporter());

        builder.Services.AddServiceDiscovery();
        builder.Services.ConfigureHttpClientDefaults(http => http.AddStandardResilienceHandler());

        builder.Services.AddHealthChecks()
            .AddCheck("self", () => HealthCheckResult.Healthy(), ["live"]);

        return builder;
    }

    /// <summary>
    /// Maps the liveness endpoint exposed by the Paradigm service defaults.
    /// </summary>
    /// <param name="app">The web application.</param>
    /// <returns>The supplied web application.</returns>
    public static WebApplication MapParadigmServiceDefaultsEndpoints(this WebApplication app)
    {
        app.MapHealthChecks("/alive", new HealthCheckOptions
        {
            Predicate = registration => registration.Tags.Contains("live")
        });

        return app;
    }
}
