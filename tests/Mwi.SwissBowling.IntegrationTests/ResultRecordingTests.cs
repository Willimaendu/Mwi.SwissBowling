using Microsoft.EntityFrameworkCore;
using Mwi.SwissBowling.Api.Features.Results;
using Mwi.SwissBowling.Contracts;
using Mwi.SwissBowling.Core.Http;

namespace Mwi.SwissBowling.IntegrationTests;

[Collection("PostgreSQL")]
public sealed class ResultRecordingTests(PostgresFixture fixture)
{
    [Fact]
    public async Task ConcurrentEditorsCannotAcquireTheSameResult()
    {
        var scenario = await fixture.CreateScenarioAsync();
        async Task<bool> Attempt(string actor)
        {
            try { await fixture.Recording().AcquireAsync(scenario.EventId, scenario.PlayerIds[0], PostgresFixture.Actor(actor), default); return true; }
            catch (RequestRejectedException exception) when (exception.StatusCode == 409) { return false; }
        }
        var outcomes = await Task.WhenAll(Attempt("editor-a"), Attempt("editor-b"));
        Assert.Single(outcomes, success => success);
    }
    [Fact]
    public async Task ExpiredEditorCannotOverwriteAReplacementLease()
    {
        var scenario = await fixture.CreateScenarioAsync();
        var oldLease = await fixture.Recording().AcquireAsync(scenario.EventId, scenario.PlayerIds[0], PostgresFixture.Actor("a"), default);
        fixture.Clock.Advance(TimeSpan.FromMinutes(6));
        var lease = await fixture.Recording().AcquireAsync(scenario.EventId, scenario.PlayerIds[0], PostgresFixture.Actor("b"), default);
        var exception = await Assert.ThrowsAsync<RequestRejectedException>(() => fixture.Recording().SaveAsync(scenario.EventId, scenario.PlayerIds[0], new ResultInput(782, 30, 24, false, 0, oldLease.Token), PostgresFixture.Actor("a"), default));
        Assert.Equal(409, exception.StatusCode);
        Assert.NotEqual(oldLease.Token, lease.Token);
    }
    [Fact]
    public async Task SavedResultsAreVersionedAndFreezeSeasonAssignments()
    {
        var scenario = await fixture.CreateScenarioAsync();
        var recording = fixture.Recording();
        var actor = PostgresFixture.Actor("a");
        var lease = await recording.AcquireAsync(scenario.EventId, scenario.PlayerIds[0], actor, default);
        Assert.Equal(1, await recording.SaveAsync(scenario.EventId, scenario.PlayerIds[0], new ResultInput(80, 0, 10, true, 0, lease.Token), actor, default));
        await Assert.ThrowsAsync<RequestRejectedException>(() => fixture.Recording().SaveAsync(scenario.EventId, scenario.PlayerIds[0], new ResultInput(90, 10, 0, true, 0, lease.Token), actor, default));
        await using var verification = fixture.CreateContext();
        Assert.True(await verification.Seasons.Where(row => row.Id == scenario.SeasonId).Select(row => row.RosterLocked).SingleAsync());
        var saved = await verification.Set<PlayerResult>().SingleAsync(row => row.EventId == scenario.EventId);
        Assert.True(saved.Interrupted);
        Assert.Equal(80, saved.Total);
        Assert.Equal(1, saved.Version);
        Assert.Single(await verification.AuditRecords.Where(row => row.Action == "ResultSaved" && row.Resource.StartsWith(scenario.EventId.ToString())).ToListAsync());
    }
    [Fact]
    public async Task AnotherActorCannotRenewOrReleaseALeaseEvenKnowingItsToken()
    {
        var scenario = await fixture.CreateScenarioAsync();
        var lease = await fixture.Recording().AcquireAsync(scenario.EventId, scenario.PlayerIds[0], PostgresFixture.Actor("a"), default);
        await Assert.ThrowsAsync<RequestRejectedException>(() => fixture.Recording().RenewAsync(scenario.EventId, scenario.PlayerIds[0], lease.Token, PostgresFixture.Actor("b"), default));
        await Assert.ThrowsAsync<RequestRejectedException>(() => fixture.Recording().ReleaseAsync(scenario.EventId, scenario.PlayerIds[0], lease.Token, PostgresFixture.Actor("b"), default));
    }
}
