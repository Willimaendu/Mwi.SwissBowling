using Microsoft.AspNetCore.Components.Authorization;

namespace SwissBowling.Client.Shared.Services;

public static class ServiceCollectionExtensions
{
    /// <summary>Client-Dienste – identisch im WebAssembly-Host und im Server (Prerendering / Interactive Server) registriert.</summary>
    public static IServiceCollection AddClientServices(this IServiceCollection services)
    {
        services.AddHttpClient<IApiClient, ApiClient>();
        services.AddScoped<ApiActivity>();
        services.AddScoped<ITokenStore, TokenStore>();
        services.AddScoped<JwtAuthenticationStateProvider>();
        services.AddScoped<AuthenticationStateProvider>(sp => sp.GetRequiredService<JwtAuthenticationStateProvider>());
        services.AddAuthorizationCore();
        services.AddCascadingAuthenticationState();
        return services;
    }
}
