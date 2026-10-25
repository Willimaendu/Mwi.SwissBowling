using System.Net;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Http.Resilience;
using MudBlazor.Services;
using Scalar.AspNetCore;
using SwissBowling.Client;
using SwissBowling.Client.Shared.Services;
using SwissBowling.Server.Auth;
using SwissBowling.Server.Components;
using SwissBowling.Server.Data;
using SwissBowling.Server.Features.Associations;
using SwissBowling.Server.Features.Championships;
using SwissBowling.Server.Features.Clubs;
using SwissBowling.Server.Features.Import;
using SwissBowling.Server.Features.Players;
using SwissBowling.Server.Features.Results;
using SwissBowling.Server.Infrastructure;

var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();
// DbContextFactory: jede Verwendung erzeugt ihren eigenen, kurzlebigen Kontext (Endpoints, Import im Hintergrund).
// AddDbContextFactory registriert AppDbContext zusätzlich als Scoped – das braucht ASP.NET Core Identity.
// EnrichNpgsqlDbContext ergänzt Aspire-Retries, Health Check und Telemetrie.
builder.Services.AddDbContextFactory<AppDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("swissbowling") ?? throw new InvalidOperationException("ConnectionString 'swissbowling' fehlt.")));
builder.EnrichNpgsqlDbContext<AppDbContext>();
builder.Services.AddHostedService<DbInitializer>();
builder.Services.AddJwtIdentity();
builder.Services.AddProblemDetails();
builder.Services.AddOpenApi();

// Import: Stammdaten von www.sfkv.ch, Resultate von www.uvweb.kegler.ch
// Beide Quellen antworten zeitweise langsam oder mit 5xx – deshalb grosszügiges Timeout und Wiederholungen.
// Beide Seiten werden wie von einem Browser abgerufen (UVweb rendert für unbekannte Browser abweichend) und antworten
// zeitweise langsam oder mit 5xx – der Standard-Resilience-Handler wiederholt solche Anfragen mit Backoff.
builder.Services.AddHttpClient<SfkvScraper>(client => ConfigureBrowserClient(client, "https://www.sfkv.ch/"))
    .ConfigurePrimaryHttpMessageHandler(() => new HttpClientHandler { AutomaticDecompression = DecompressionMethods.All })
    .AddStandardResilienceHandler(ConfigureResilience);
builder.Services.AddHttpClient<UvwebScraper>(client => ConfigureBrowserClient(client, "https://www.uvweb.kegler.ch/"))
    .ConfigurePrimaryHttpMessageHandler(() => new HttpClientHandler { AutomaticDecompression = DecompressionMethods.All, UseCookies = true, CookieContainer = new CookieContainer() })
    .AddStandardResilienceHandler(ConfigureResilience);
builder.Services.AddSingleton<ImportRunner>();
builder.Services.AddHostedService<UvwebSyncService>();

// Blazor (Interactive Auto) – Prerendering und Interactive Server laufen hier und brauchen die Client-Dienste.
builder.Services.AddRazorComponents().AddInteractiveServerComponents().AddInteractiveWebAssemblyComponents();
builder.Services.AddMudServices();
builder.Services.AddClientServices();

var app = builder.Build();

app.UseExceptionHandler();
app.UseStatusCodePages();

if (app.Environment.IsDevelopment())
{
    app.UseWebAssemblyDebugging();
    app.MapOpenApi();
    app.MapScalarApiReference(o => o.WithTitle("Swiss Bowling API"));
}
else
{
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseAuthentication();
app.UseAuthorization();
app.UseAntiforgery();

app.MapDefaultEndpoints();

var api = app.MapGroup(string.Empty).AddEndpointFilter<ValidationFilter>();
api.MapAuth();
api.MapAssociations();
api.MapClubs();
api.MapPlayers();
api.MapChampionships();
api.MapResults();
api.MapImport();

app.MapStaticAssets();
app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode()
    .AddInteractiveWebAssemblyRenderMode()
    .AddAdditionalAssemblies(typeof(Routes).Assembly);

await app.RunAsync();

static void ConfigureResilience(HttpStandardResilienceOptions options)
{
    // Die UV-Seiten brauchen teils über 30 s; drei Wiederholungen mit 3, 6, 12 s Backoff.
    options.AttemptTimeout.Timeout = TimeSpan.FromSeconds(60);
    options.TotalRequestTimeout.Timeout = TimeSpan.FromMinutes(5);
    options.CircuitBreaker.SamplingDuration = TimeSpan.FromMinutes(2);
    options.Retry.MaxRetryAttempts = 3;
    options.Retry.Delay = TimeSpan.FromSeconds(3);
}

static void ConfigureBrowserClient(HttpClient client, string baseAddress)
{
    client.BaseAddress = new Uri(baseAddress);
    // Das Gesamt-Timeout steuert der Resilience-Handler.
    client.Timeout = Timeout.InfiniteTimeSpan;
    client.DefaultRequestHeaders.UserAgent.ParseAdd("Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/130.0.0.0 Safari/537.36 SwissBowling/1.0");
    client.DefaultRequestHeaders.Accept.ParseAdd("text/html,application/xhtml+xml,application/xml;q=0.9,*/*;q=0.8");
    client.DefaultRequestHeaders.AcceptLanguage.ParseAdd("de-CH,de;q=0.9");
}
