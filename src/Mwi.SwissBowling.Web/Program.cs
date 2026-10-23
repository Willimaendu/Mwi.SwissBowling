using Microsoft.EntityFrameworkCore;
using MudBlazor.Services;
using Mwi.SwissBowling.Core.Hosting;
using Mwi.SwissBowling.Core.Persistence;
using Mwi.SwissBowling.Web.Authentication;
using Mwi.SwissBowling.Web.Components;

var builder = WebApplication.CreateBuilder(args);
builder.AddOperationalDefaults();
builder.Services.AddDbContextFactory<WebStateDbContext>(options => options.UseNpgsql(
    builder.Configuration.GetConnectionString("webstate") ?? throw new InvalidOperationException("ConnectionStrings:webstate is required.")));
builder.Services.AddHealthChecks().AddCheck<DatabaseReadinessCheck<WebStateDbContext>>("postgres", tags: ["ready"]);
if (args.Contains("--migrate", StringComparer.Ordinal))
{
    using var migrationApp = builder.Build();
    await using var scope = migrationApp.Services.CreateAsyncScope();
    await SchemaDeployment.ApplyAsync(scope.ServiceProvider.GetRequiredService<WebStateDbContext>(), typeof(WebStateDbContext).Assembly, "Mwi.SwissBowling.Core.Persistence.Migrations.");
    return;
}
builder.AddWebAuthentication();
builder.Services.AddHttpContextAccessor();
builder.Services.AddRazorComponents().AddInteractiveWebAssemblyComponents();
builder.Services.AddMudServices();
builder.Services.AddHttpClient("api", client =>
{
    client.BaseAddress = new Uri(builder.Configuration["Api:BaseAddress"] ?? "https+http://api/");
    client.Timeout = TimeSpan.FromSeconds(15);
}).ConfigurePrimaryHttpMessageHandler(() => new SocketsHttpHandler { UseCookies = false, AllowAutoRedirect = false });
builder.WebHost.ConfigureKestrel(options => options.Limits.MaxRequestBodySize = 64 * 1024);
var app = builder.Build();
app.UseExceptionHandler();
if (!app.Environment.IsDevelopment()) app.UseHsts();
app.UseHttpsRedirection();
app.Use(async (context, next) =>
{
    context.Response.Headers.XContentTypeOptions = "nosniff";
    context.Response.Headers.XFrameOptions = "DENY";
    context.Response.Headers["Referrer-Policy"] = "strict-origin-when-cross-origin";
    context.Response.Headers.ContentSecurityPolicy = "default-src 'self'; script-src 'self' 'wasm-unsafe-eval'; style-src 'self'; img-src 'self' data:; font-src 'self'; connect-src 'self'; object-src 'none'; frame-ancestors 'none'; base-uri 'self'; form-action 'self'";
    if (context.Request.Path.StartsWithSegments("/bff") || context.Request.Path.StartsWithSegments("/account") || context.Request.Path.StartsWithSegments("/administration"))
        context.Response.Headers.CacheControl = "no-store";
    await next();
});
app.UseAuthentication();
app.UseAuthorization();
app.UseAntiforgery();
app.MapStaticAssets();
app.MapOperationalEndpoints();
app.MapBffEndpoints();
app.MapRazorComponents<App>().AddInteractiveWebAssemblyRenderMode().AddAdditionalAssemblies(typeof(Mwi.SwissBowling.Web.Client.Program).Assembly);
await app.RunAsync();
