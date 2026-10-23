using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using OpenTelemetry;
using OpenTelemetry.Logs;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;

namespace Mwi.SwissBowling.Core.Hosting;

public static class OperationalDefaults
{
    public static T AddOperationalDefaults<T>(this T builder) where T : IHostApplicationBuilder
    {
        builder.Services.AddSingleton(TimeProvider.System);
        builder.Services.AddProblemDetails();
        builder.Services.AddExceptionHandler<SafeExceptionHandler>();
        builder.Services.AddHealthChecks().AddCheck("self", () => HealthCheckResult.Healthy(), ["live"]);
        builder.Services.AddServiceDiscovery();
        builder.Services.ConfigureHttpClientDefaults(client => client.AddServiceDiscovery());
        builder.Services.AddOpenTelemetry()
            .ConfigureResource(resource => resource.AddService(builder.Environment.ApplicationName))
            .WithMetrics(metrics => metrics.AddAspNetCoreInstrumentation().AddHttpClientInstrumentation().AddRuntimeInstrumentation())
            .WithTracing(tracing => tracing.AddAspNetCoreInstrumentation(options =>
                options.Filter = context => !context.Request.Path.StartsWithSegments("/health") && !context.Request.Path.StartsWithSegments("/alive"))
                .AddHttpClientInstrumentation());
        if (!string.IsNullOrWhiteSpace(builder.Configuration["OTEL_EXPORTER_OTLP_ENDPOINT"]))
        {
            builder.Services.AddOpenTelemetry().UseOtlpExporter();
            builder.Logging.AddOpenTelemetry(logging =>
            {
                logging.IncludeFormattedMessage = false;
                logging.IncludeScopes = false;
                logging.ParseStateValues = false;
                logging.AddOtlpExporter();
            });
        }
        return builder;
    }

    public static void MapOperationalEndpoints(this WebApplication app)
    {
        app.MapHealthChecks("/alive", new HealthCheckOptions { Predicate = check => check.Tags.Contains("live") }).AllowAnonymous();
        app.MapHealthChecks("/health", new HealthCheckOptions { Predicate = check => check.Tags.Contains("ready") || check.Tags.Contains("live") }).AllowAnonymous();
    }
}

public sealed class DatabaseReadinessCheck<T>(IServiceScopeFactory scopes) : IHealthCheck where T : DbContext
{
    public async Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default)
    {
        await using var scope = scopes.CreateAsyncScope();
        return await scope.ServiceProvider.GetRequiredService<T>().Database.CanConnectAsync(cancellationToken)
            ? HealthCheckResult.Healthy() : HealthCheckResult.Unhealthy();
    }
}

public sealed class SafeExceptionHandler(ILogger<SafeExceptionHandler> logger) : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(HttpContext httpContext, Exception exception, CancellationToken cancellationToken)
    {
        if (exception is Mwi.SwissBowling.Core.Http.RequestRejectedException rejected)
        {
            await Results.Problem(statusCode: rejected.StatusCode, title: rejected.Message).ExecuteAsync(httpContext);
            return true;
        }
        var conflict = exception is DbUpdateConcurrencyException || exception is DbUpdateException { InnerException: Npgsql.PostgresException { SqlState: "23505" or "23503" or "23514" } };
        logger.LogError("Request failed with {ErrorType}; trace {TraceId}", exception.GetType().Name, httpContext.TraceIdentifier);
        await Results.Problem(statusCode: conflict ? 409 : 500,
            title: conflict ? "Die Daten wurden verändert oder widersprechen einer bestehenden Zuordnung." : "Die Anfrage konnte nicht verarbeitet werden.",
            extensions: new Dictionary<string, object?> { ["traceId"] = httpContext.TraceIdentifier }).ExecuteAsync(httpContext);
        return true;
    }
}
