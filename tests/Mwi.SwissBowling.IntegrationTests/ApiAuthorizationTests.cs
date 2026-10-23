using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using Mwi.SwissBowling.Api.Features.Identity;
using Mwi.SwissBowling.Contracts;

namespace Mwi.SwissBowling.IntegrationTests;

[Collection("PostgreSQL")]
public sealed class ApiAuthorizationTests(PostgresFixture fixture)
{
    [Fact]
    public async Task AnonymousCannotReadSetupAndDraftIsNotPublic()
    {
        var scenario = await fixture.CreateScenarioAsync();
        await using var factory = new ApiFactory(fixture);
        using var client = factory.CreateClient();
        using var setup = await client.GetAsync("/api/setup");
        using var draft = await client.GetAsync($"/api/public/events/{scenario.EventId}");
        Assert.Equal(HttpStatusCode.Unauthorized, setup.StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, draft.StatusCode);
    }
    [Fact]
    public async Task AdministratorRoleAloneDoesNotBypassApproval()
    {
        await using var factory = new ApiFactory(fixture);
        using var client = factory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", factory.Token(Guid.NewGuid().ToString(), ["SystemAdministrator"]));
        using var response = await client.GetAsync("/api/setup");
        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }
    [Fact]
    public async Task ApprovedAdministratorCanApproveButMustVerifyIdentity()
    {
        var subject = Guid.NewGuid().ToString();
        var pending = new ApplicationAccount { Id = Guid.NewGuid(), Issuer = ApiFactory.Issuer, Subject = Guid.NewGuid().ToString(), DisplayName = "Pending", RegisteredAt = fixture.Clock.GetUtcNow() };
        await using (var database = fixture.CreateContext())
        {
            database.Accounts.Add(new ApplicationAccount { Id = Guid.NewGuid(), Issuer = ApiFactory.Issuer, Subject = subject, DisplayName = "Administration", RegisteredAt = fixture.Clock.GetUtcNow(), IsApproved = true });
            database.Accounts.Add(pending);
            await database.SaveChangesAsync();
        }
        await using var factory = new ApiFactory(fixture);
        using var client = factory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", factory.Token(subject, ["SportAdministrator"]));
        using var setup = await client.GetAsync("/api/setup");
        Assert.Equal(HttpStatusCode.OK, setup.StatusCode);
        Assert.Equal("no-store", setup.Headers.CacheControl?.ToString());
        using var unverified = await client.PostAsJsonAsync($"/api/identity/{pending.Id}/approve", new ApproveAccountRequest(false, null));
        Assert.Equal(HttpStatusCode.BadRequest, unverified.StatusCode);
        using var verified = await client.PostAsJsonAsync($"/api/identity/{pending.Id}/approve", new ApproveAccountRequest(true, null));
        Assert.Equal(HttpStatusCode.NoContent, verified.StatusCode);
    }
    [Fact]
    public async Task WrongAudienceAndTamperedSignatureAreRejected()
    {
        await using var factory = new ApiFactory(fixture);
        using var client = factory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", factory.Token("a", ["SystemAdministrator"], "other-api"));
        using var wrongAudience = await client.GetAsync("/api/identity/me");
        Assert.Equal(HttpStatusCode.Unauthorized, wrongAudience.StatusCode);
        var parts = factory.Token("a", ["SystemAdministrator"]).Split('.');
        parts[2] = (parts[2][0] == 'A' ? "B" : "A") + parts[2][1..];
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", string.Join('.', parts));
        using var tampered = await client.GetAsync("/api/identity/me");
        Assert.Equal(HttpStatusCode.Unauthorized, tampered.StatusCode);
    }
}
