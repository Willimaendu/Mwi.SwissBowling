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
using SwissBowling.Server.Features.Venues;
using SwissBowling.Server.Infrastructure;

var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();
builder.AddNpgsqlDbContext<AppDbContext>("swissbowling");
builder.Services.AddHostedService<DbInitializer>();
builder.Services.AddJwtIdentity();
builder.Services.AddProblemDetails();
builder.Services.AddOpenApi();

// Import: Stammdaten von www.sfkv.ch, Resultate von www.uvweb.kegler.ch
builder.Services.AddHttpClient<SfkvScraper>(client => client.BaseAddress = new Uri("https://www.sfkv.ch/"));
builder.Services.AddHttpClient<UvwebScraper>(client =>
    {
        client.BaseAddress = new Uri("https://www.uvweb.kegler.ch/");
        // WebForms rendert für unbekannte Browser abweichend – deshalb als normaler Browser auftreten.
        client.DefaultRequestHeaders.UserAgent.ParseAdd("Mozilla/5.0 (Windows NT 10.0; Win64; x64) SwissBowling/1.0");
    })
    .ConfigurePrimaryHttpMessageHandler(() => new HttpClientHandler { UseCookies = true, CookieContainer = new System.Net.CookieContainer() });
builder.Services.AddScoped<MasterDataImporter>();
builder.Services.AddScoped<UvwebImporter>();
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
api.MapVenues();
api.MapImport();

app.MapStaticAssets();
app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode()
    .AddInteractiveWebAssemblyRenderMode()
    .AddAdditionalAssemblies(typeof(Routes).Assembly);

await app.RunAsync();
