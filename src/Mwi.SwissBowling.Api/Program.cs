using Microsoft.EntityFrameworkCore;
using Mwi.SwissBowling.Api.Features.Identity;
using Mwi.SwissBowling.Api.Features.Setup;
using Mwi.SwissBowling.Api.Features.Results;
using Mwi.SwissBowling.Api.Features.Standings;
using Mwi.SwissBowling.Api.Features.Publication;
using Mwi.SwissBowling.Api.Persistence;
using Mwi.SwissBowling.Core.Hosting;

var builder = WebApplication.CreateBuilder(args);
builder.AddOperationalDefaults();
builder.Services.AddDbContext<ApplicationDbContext>(options => options.UseNpgsql(
    builder.Configuration.GetConnectionString("application") ?? throw new InvalidOperationException("ConnectionStrings:application is required.")));
builder.Services.AddHealthChecks().AddCheck<DatabaseReadinessCheck<ApplicationDbContext>>("postgres", tags: ["ready"]);
builder.Services.AddResultRecording(builder.Configuration);
builder.Services.AddScoped<PublicationService>();
if (!DeploymentCommands.IsRequested(args)) builder.AddApiIdentity();
var app = builder.Build();
if (DeploymentCommands.IsRequested(args))
{
    await DeploymentCommands.ExecuteAsync(app, args);
    return;
}
app.UseExceptionHandler();
app.UseAuthentication();
app.UseAuthorization();
app.Use(async (context, next) =>
{
    if (!context.Request.Path.StartsWithSegments("/api/public")) context.Response.Headers.CacheControl = "no-store";
    await next();
});
app.MapOperationalEndpoints();
app.MapIdentityEndpoints();
app.MapSetupEndpoints();
app.MapResultEndpoints();
app.MapStandingEndpoints();
app.MapPublicationEndpoints();
await app.RunAsync();

public partial class Program
{
    protected Program() { }
}
