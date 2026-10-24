using System.Globalization;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using MudBlazor.Services;
using SwissBowling.Client.Shared.Services;

var builder = WebAssemblyHostBuilder.CreateDefault(args);

CultureInfo.DefaultThreadCurrentCulture = CultureInfo.DefaultThreadCurrentUICulture = new CultureInfo("de-CH");

builder.Services.AddMudServices();
builder.Services.AddClientServices();

await builder.Build().RunAsync();
