using OpenTelemetry;
using OpenTelemetry.Metrics;
using OpenTelemetry.Trace;

namespace SwissBowling.Server.Infrastructure;

/// <summary>Telemetrie und Health Checks für das Aspire Dashboard.</summary>
public static class ServiceDefaults
{
    public static WebApplicationBuilder AddServiceDefaults(this WebApplicationBuilder builder)
    {
        builder.Logging.AddOpenTelemetry(o => o.IncludeFormattedMessage = true);

        var telemetry = builder.Services.AddOpenTelemetry()
            .WithMetrics(m => m.AddAspNetCoreInstrumentation().AddHttpClientInstrumentation().AddRuntimeInstrumentation())
            .WithTracing(t => t.AddAspNetCoreInstrumentation().AddHttpClientInstrumentation());

        if (!string.IsNullOrWhiteSpace(builder.Configuration["OTEL_EXPORTER_OTLP_ENDPOINT"]))
        {
            telemetry.UseOtlpExporter();
        }

        builder.Services.AddHealthChecks();
        return builder;
    }

    public static WebApplication MapDefaultEndpoints(this WebApplication app)
    {
        if (app.Environment.IsDevelopment())
        {
            app.MapHealthChecks("/health");
        }

        return app;
    }
}
