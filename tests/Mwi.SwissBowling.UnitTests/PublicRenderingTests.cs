using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Mwi.SwissBowling.Contracts;
using Mwi.SwissBowling.Web.Components.Pages;

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
    private static async Task<(string Html, int Status)> RenderAsync<T>(HttpMessageHandler handler, Dictionary<string, object?> parameters) where T : IComponent
    {
        var context = new DefaultHttpContext();
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddSingleton<IConfiguration>(new ConfigurationBuilder().Build());
        services.AddSingleton<IHttpContextAccessor>(new HttpContextAccessor { HttpContext = context });
        services.AddSingleton<IHttpClientFactory>(new ClientFactory(handler));
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
    private sealed class ClientFactory(HttpMessageHandler handler) : IHttpClientFactory
    {
        public HttpClient CreateClient(string name) => new(handler, disposeHandler: false) { BaseAddress = new Uri("https://api.test/") };
    }
    private sealed class StubHandler(Func<HttpRequestMessage, HttpResponseMessage> respond) : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken) => Task.FromResult(respond(request));
    }
}
