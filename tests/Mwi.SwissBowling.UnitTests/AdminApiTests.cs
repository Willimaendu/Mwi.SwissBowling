using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Mwi.SwissBowling.Contracts;
using Mwi.SwissBowling.Web.Client.Infrastructure;

namespace Mwi.SwissBowling.UnitTests;

public sealed class AdminApiTests
{
    [Fact]
    public async Task MutationObtainsCsrfTokenAndSendsTypedBody()
    {
        var requests = 0;
        using var handler = new StubHandler(async request =>
        {
            requests++;
            if (requests == 1)
            {
                Assert.Equal(HttpMethod.Get, request.Method);
                Assert.Equal("/bff/antiforgery", request.RequestUri!.AbsolutePath);
                return Json(new { requestToken = "csrf-test-token" });
            }
            Assert.Equal(HttpMethod.Post, request.Method);
            Assert.Equal("/bff/api/setup/seasons", request.RequestUri!.AbsolutePath);
            Assert.Equal("csrf-test-token", Assert.Single(request.Headers.GetValues("X-CSRF-TOKEN")));
            using var body = JsonDocument.Parse(await request.Content!.ReadAsStringAsync());
            Assert.Equal(2026, body.RootElement.GetProperty("year").GetInt32());
            return new HttpResponseMessage(HttpStatusCode.NoContent);
        });
        using var client = Client(handler);
        await new AdminApi(client).PostAsync("setup/seasons", new SeasonInput(2026));
        Assert.Equal(2, requests);
    }
    [Fact]
    public async Task FailedCsrfFetchNeverSendsMutation()
    {
        var requests = 0;
        using var handler = new StubHandler(_ =>
        {
            requests++;
            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.Unauthorized));
        });
        using var client = Client(handler);
        var error = await Assert.ThrowsAsync<ApiException>(() => new AdminApi(client).PostAsync("identity/register", new { }));
        Assert.Equal(HttpStatusCode.Unauthorized, error.StatusCode);
        Assert.Equal(1, requests);
    }
    [Fact]
    public async Task ReadsUseBffAndDeserializeContracts()
    {
        using var handler = new StubHandler(request =>
        {
            Assert.Equal("/bff/api/identity/me", request.RequestUri!.AbsolutePath);
            Assert.Equal(HttpMethod.Get, request.Method);
            return Task.FromResult(Json(new IdentityProfile(null, "Test", false, false, [])));
        });
        using var client = Client(handler);
        var result = await new AdminApi(client).GetAsync<IdentityProfile>("identity/me");
        Assert.False(result.IsApproved);
        Assert.Equal("Test", result.DisplayName);
    }
    [Fact]
    public async Task ValidationErrorsArePreservedForUser()
    {
        using var handler = new StubHandler(_ => Task.FromResult(Json(new { title = "Ungültige Eingabe", errors = new { score = new[] { "Bitte Holz prüfen." } } }, HttpStatusCode.BadRequest)));
        using var client = Client(handler);
        var error = await Assert.ThrowsAsync<ApiException>(() => new AdminApi(client).GetAsync<object>("setup"));
        Assert.Equal("Bitte Holz prüfen.", error.Message);
        Assert.Equal(HttpStatusCode.BadRequest, error.StatusCode);
    }
    [Fact]
    public async Task ConflictTitleIsPreservedForUser()
    {
        using var handler = new StubHandler(_ => Task.FromResult(Json(new { title = "Ein anderes Konto bearbeitet dieses Resultat." }, HttpStatusCode.Conflict)));
        using var client = Client(handler);
        var error = await Assert.ThrowsAsync<ApiException>(() => new AdminApi(client).GetAsync<object>("setup"));
        Assert.Equal(HttpStatusCode.Conflict, error.StatusCode);
        Assert.Equal("Ein anderes Konto bearbeitet dieses Resultat.", error.Message);
    }
    [Fact]
    public async Task HtmlFailureDoesNotLeakUpstreamBody()
    {
        using var handler = new StubHandler(_ => Task.FromResult(new HttpResponseMessage(HttpStatusCode.BadGateway) { Content = new StringContent("<html>upstream internal details</html>") }));
        using var client = Client(handler);
        var error = await Assert.ThrowsAsync<ApiException>(() => new AdminApi(client).GetAsync<object>("setup"));
        Assert.DoesNotContain("internal details", error.Message);
        Assert.Equal(HttpStatusCode.BadGateway, error.StatusCode);
    }
    private static HttpClient Client(HttpMessageHandler handler) => new(handler) { BaseAddress = new Uri("https://web.test/") };
    private static HttpResponseMessage Json<T>(T body, HttpStatusCode status = HttpStatusCode.OK) => new(status) { Content = JsonContent.Create(body) };
    private sealed class StubHandler(Func<HttpRequestMessage, Task<HttpResponseMessage>> respond) : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken) => respond(request);
    }
}
