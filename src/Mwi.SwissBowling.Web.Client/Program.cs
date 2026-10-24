using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using Microsoft.FluentUI.AspNetCore.Components;
using MudBlazor.Services;
using Mwi.SwissBowling.Web.Client.Infrastructure;

namespace Mwi.SwissBowling.Web.Client;

public static class Program
{
    public static async Task Main(string[] args)
    {
        var builder = WebAssemblyHostBuilder.CreateDefault(args);
        builder.Services.AddFluentUIComponents();
        builder.Services.AddMudServices();
        builder.Services.AddScoped(_ => new HttpClient { BaseAddress = new Uri(builder.HostEnvironment.BaseAddress) });
        builder.Services.AddScoped<IAdminApi, AdminApi>();
        builder.Services.AddScoped<Mwi.SwissBowling.Web.Client.Services.IDashboardApi, Mwi.SwissBowling.Web.Client.Services.DashboardApi>();
        await builder.Build().RunAsync();
    }
}
