using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.JSInterop;
using MudBlazor.Services;
using Mwi.SwissBowling.Contracts;
using Mwi.SwissBowling.Web.Components.Pages;
using Mwi.SwissBowling.Web.Components.Layout;
using Mwi.SwissBowling.Web.Client.Features.Administration;
using Mwi.SwissBowling.Web.Client.Infrastructure;

namespace Mwi.SwissBowling.UnitTests;

public sealed class PublicRenderingTests
{
    [Fact]
    public async Task RankingAndCorrectionNoticeAreRenderedWithoutBrowserRuntime()
    {
        var id = Guid.NewGuid();
        var snapshot = new PublicEventDto(id, "Clubmeisterschaft", "Anlage", 2026,
            new DateOnly(2026, 1, 1), new DateOnly(2026, 1, 2), DateTimeOffset.UtcNow, 2, true,
            [new RankedRow(1, "Ada Beispiel", "Klub", PlayerCategory.A, 800, 50, 20, true)]);
        using var handler = new StubHandler(request =>
        {
            Assert.Equal($"/api/public/events/{id}", request.RequestUri!.AbsolutePath);
            return new HttpResponseMessage(HttpStatusCode.OK) { Content = JsonContent.Create(snapshot) };
        });
        var (html, status) = await RenderAsync<PublicStandings>(handler, new Dictionary<string, object?> { [nameof(PublicStandings.EventId)] = id });
        Assert.Equal(200, status);
        Assert.Contains("Clubmeisterschaft", html);
        Assert.Contains("Ada Beispiel", html);
        Assert.Contains("800", html);
        Assert.Contains("In Korrektur", html);
        Assert.Contains("Abgebrochen", html);
        Assert.Contains("<table", html);
        Assert.DoesNotContain("PlayerId", html);
    }
    [Fact]
    public async Task UnpublishedEventReturns404AndNoRanking()
    {
        using var handler = new StubHandler(_ => new HttpResponseMessage(HttpStatusCode.NotFound));
        var (html, status) = await RenderAsync<PublicStandings>(handler, new Dictionary<string, object?> { [nameof(PublicStandings.EventId)] = Guid.NewGuid() });
        Assert.Equal(404, status);
        Assert.Contains("Keine ver", html);
        Assert.DoesNotContain("<table", html);
    }
    [Fact]
    public async Task PublicApiOutageReturns503InsteadOfEmptyRanking()
    {
        using var handler = new StubHandler(_ => new HttpResponseMessage(HttpStatusCode.ServiceUnavailable));
        var (html, status) = await RenderAsync<PublicStandings>(handler, new Dictionary<string, object?> { [nameof(PublicStandings.EventId)] = Guid.NewGuid() });
        Assert.Equal(503, status);
        Assert.Contains("derzeit nicht erreichbar", html);
        Assert.DoesNotContain("<table", html);
    }
    [Fact]
    public async Task EventListContainsNavigableServerRenderedLinks()
    {
        var id = Guid.NewGuid();
        using var handler = new StubHandler(_ => new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = JsonContent.Create(new[] { new PublicEventSummary(id, "Sommermeisterschaft", 2026, DateTimeOffset.UtcNow, 1, false) })
        });
        var (html, status) = await RenderAsync<Home>(handler, []);
        Assert.Equal(200, status);
        Assert.Contains($"href=\"/meisterschaften/{id}\"", html);
        Assert.Contains("Sommermeisterschaft", html);
    }
    [Fact]
    public async Task EventListOutageRetainsAutomaticRecoveryInsteadOfClaimingThereAreNoEvents()
    {
        using var handler = new StubHandler(_ => new HttpResponseMessage(HttpStatusCode.ServiceUnavailable));
        var (html, status) = await RenderAsync<Home>(handler, []);
        Assert.Equal(503, status);
        Assert.Contains("derzeit nicht erreichbar", html);
        Assert.Contains("live-status", html);
        Assert.DoesNotContain("Noch keine", html);
    }

    [Fact]
    public async Task AdministrationOutageDoesNotMasqueradeAsSignInRequired()
    {
        using var handler = new StubHandler(request =>
        {
            Assert.Equal("/bff/api/identity/me", request.RequestUri!.AbsolutePath);
            return new HttpResponseMessage(HttpStatusCode.ServiceUnavailable);
        });
        var (html, _) = await RenderAsync<AdminWorkspace>(handler, []);
        Assert.Contains("nicht erreichbar", html);
        Assert.Contains("live-status", html);
        Assert.DoesNotContain("Anmelden / Registrieren", html);
    }

    [Fact]
    public async Task ConfirmedUnauthorizedSessionDisplaysSignIn()
    {
        using var handler = new StubHandler(_ => new HttpResponseMessage(HttpStatusCode.Unauthorized));
        var (html, _) = await RenderAsync<AdminWorkspace>(handler, []);
        Assert.Contains("Anmelden / Registrieren", html);
        Assert.Contains("/account/login?returnUrl=/administration", html);
    }

    [Theory]
    [InlineData("/meisterschaften", "Meisterschaften")]
    [InlineData("/administration", "Administration")]
    public async Task DashboardShellRendersNavigationContentAndWorkspaceContext(string path, string workspace)
    {
        using var handler = new StubHandler(_ => throw new InvalidOperationException("The layout must not call the API."));
        RenderFragment body = builder => builder.AddMarkupContent(0, "<h1>Testbereich</h1>");
        var (html, status) = await RenderAsync<MainLayout>(handler,
            new Dictionary<string, object?> { [nameof(MainLayout.Body)] = body }, path);

        Assert.Equal(200, status);
        Assert.Contains("application-shell", html);
        Assert.Contains("href=\"#main\"", html);
        Assert.Contains("aria-label=\"Hauptnavigation\"", html);
        Assert.Contains("href=\"/meisterschaften\"", html);
        Assert.Contains("href=\"/administration\"", html);
        Assert.Contains("Testbereich", html);
        Assert.Matches($"app-bar-caption[^>]*>{workspace}</span>", html);
    }

    [Fact]
    public async Task AuthenticatedAdministrationRendersWorkflowNavigationAndOverview()
    {
        using var handler = new StubHandler(request => request.RequestUri!.AbsolutePath switch
        {
            "/bff/api/identity/me" => new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = JsonContent.Create(new IdentityProfile(Guid.NewGuid(), "Test Administrator", true, true, ["SportAdministrator"]))
            },
            "/bff/api/setup" => new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = JsonContent.Create(new SetupDto([], [], [], [], [], [], []))
            },
            _ => throw new InvalidOperationException("Unexpected API request.")
        });
        var (html, _) = await RenderAsync<AdminWorkspace>(handler, []);

        Assert.Contains("workspace-nav", html);
        Assert.Contains("workspace-content", html);
        Assert.Contains("Test Administrator", html);
        Assert.Contains("Stammdaten", html);
        Assert.Contains("Freigaben", html);
        Assert.Contains("Noch keine Meisterschaften", html);
        Assert.DoesNotContain("Anmelden / Registrieren", html);
    }

    private static async Task<(string Html, int Status)> RenderAsync<T>(HttpMessageHandler handler, Dictionary<string, object?> parameters, string path = "/") where T : IComponent
    {
        var context = new DefaultHttpContext();
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddMudServices();
        services.AddSingleton<NavigationManager>(new TestNavigationManager(path));
        services.AddSingleton<IJSRuntime>(new NoBrowserJsRuntime());
        // HtmlRenderer renders the same initial content without a browser activation boundary.
        if (typeof(T) == typeof(Home) || typeof(T) == typeof(PublicStandings))
            parameters["LiveRenderMode"] = null;
        services.AddSingleton<IConfiguration>(new ConfigurationBuilder().Build());
        services.AddSingleton<IHttpContextAccessor>(new HttpContextAccessor { HttpContext = context });
        services.AddSingleton<IHttpClientFactory>(new ClientFactory(handler));
        services.AddScoped<IAdminApi>(provider => new AdminApi(provider.GetRequiredService<IHttpClientFactory>().CreateClient("bff")));
        services.AddScoped<Mwi.SwissBowling.Web.Features.Standings.PublicEventsClient>();
        await using var provider = services.BuildServiceProvider();
        await using var renderer = new HtmlRenderer(provider, provider.GetRequiredService<ILoggerFactory>());
        var html = await renderer.Dispatcher.InvokeAsync(async () =>
        {
            var result = await renderer.RenderComponentAsync<T>(ParameterView.FromDictionary(parameters));
            return result.ToHtmlString();
        });
        return (html, context.Response.StatusCode);
    }
    private sealed class TestNavigationManager : NavigationManager
    {
        public TestNavigationManager(string path) => Initialize("https://web.test/", new Uri(new Uri("https://web.test/"), path).AbsoluteUri);
        protected override void NavigateToCore(string uri, bool forceLoad) => throw new InvalidOperationException("Static rendering must not navigate.");
    }
    private sealed class NoBrowserJsRuntime : IJSRuntime
    {
        // MudBlazor performs best-effort JS cleanup when its services are disposed.
        // Model an unavailable browser using the standard interop exception it handles.
        public ValueTask<TValue> InvokeAsync<TValue>(string identifier, object?[]? args) => throw new JSDisconnectedException("No browser is connected during static rendering.");
        public ValueTask<TValue> InvokeAsync<TValue>(string identifier, CancellationToken cancellationToken, object?[]? args) => InvokeAsync<TValue>(identifier, args);
    }
    private sealed class ClientFactory(HttpMessageHandler handler) : IHttpClientFactory
    {
        public HttpClient CreateClient(string name) => new(handler, disposeHandler: false) { BaseAddress = new Uri("https://api.test/") };
    }
    private sealed class StubHandler(Func<HttpRequestMessage, HttpResponseMessage> respond) : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken) => Task.FromResult(respond(request));
    }
}
