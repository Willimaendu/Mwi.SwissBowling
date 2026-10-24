using MudBlazor.Services;
using Scalar.AspNetCore;
using SwissBowling.Client;
using SwissBowling.Client.Shared.Services;
using SwissBowling.Server.Auth;
using SwissBowling.Server.Components;
using SwissBowling.Server.Data;
using SwissBowling.Server.Features.Championships;
using SwissBowling.Server.Features.Clubs;
using SwissBowling.Server.Features.Players;
using SwissBowling.Server.Features.Results;
using SwissBowling.Server.Features.Seasons;
using SwissBowling.Server.Infrastructure;

var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();
builder.AddNpgsqlDbContext<AppDbContext>("swissbowling");
builder.Services.AddHostedService<DbInitializer>();
builder.Services.AddJwtIdentity();
builder.Services.AddProblemDetails();
builder.Services.AddOpenApi();

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
api.MapClubs();
api.MapPlayers();
api.MapSeasons();
api.MapChampionships();
api.MapResults();

app.MapStaticAssets();
app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode()
    .AddInteractiveWebAssemblyRenderMode()
    .AddAdditionalAssemblies(typeof(Routes).Assembly);

app.Run();
