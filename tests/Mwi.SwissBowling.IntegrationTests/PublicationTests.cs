using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Mwi.SwissBowling.Api.Features.Publication;
using Mwi.SwissBowling.Contracts;
using Mwi.SwissBowling.Core.Http;

namespace Mwi.SwissBowling.IntegrationTests;

[Collection("PostgreSQL")]
public sealed class PublicationTests(PostgresFixture fixture)
{
    private async Task SaveAsync(Scenario scenario, int total, int nines, int eights, long version)
    {
        var service = fixture.Recording();
        var actor = PostgresFixture.Actor("editor");
        var lease = await service.AcquireAsync(scenario.EventId, scenario.PlayerIds[0], actor, default);
        await service.SaveAsync(scenario.EventId, scenario.PlayerIds[0], new ResultInput(total, nines, eights, false, version, lease.Token), actor, default);
        await service.ReleaseAsync(scenario.EventId, scenario.PlayerIds[0], lease.Token, actor, default);
    }
    [Fact]
    public async Task DraftIsPrivateAndPublishedProjectionExcludesPersonalFields()
    {
        var scenario = await fixture.CreateScenarioAsync();
        var service = new PublicationService(fixture, fixture.Clock);
        Assert.Null(await new PublicationQueries(fixture).ReadPublicAsync(scenario.EventId, default));
        await SaveAsync(scenario, 782, 30, 24, 0);
        var snapshot = await service.PublishAsync(scenario.EventId, PostgresFixture.Actor("editor"), default);
        var json = JsonSerializer.Serialize(snapshot);
        Assert.DoesNotContain("private@example.invalid", json);
        Assert.DoesNotContain("DateOfBirth", json);
        Assert.DoesNotContain("SfkvNumber", json);
        Assert.DoesNotContain("PlayerId", json);
        Assert.Equal(782, Assert.Single(snapshot.Standings).Total);
    }
    [Fact]
    public async Task CorrectionsRequireAnotherActorAndKeepOldSnapshotUntilRepublication()
    {
        var scenario = await fixture.CreateScenarioAsync();
        await SaveAsync(scenario, 782, 30, 24, 0);
        await new PublicationService(fixture, fixture.Clock).PublishAsync(scenario.EventId, PostgresFixture.Actor("editor"), default);
        Guid request;
        request = await new PublicationService(fixture, fixture.Clock).RequestCorrectionAsync(scenario.EventId, "Trefferzahl anhand Standblatt korrigieren", PostgresFixture.Actor("editor"), default);
        await Assert.ThrowsAsync<RequestRejectedException>(() => new PublicationService(fixture, fixture.Clock).ApproveCorrectionAsync(scenario.EventId, request, PostgresFixture.Actor("editor"), default));
        await new PublicationService(fixture, fixture.Clock).ApproveCorrectionAsync(scenario.EventId, request, PostgresFixture.Actor("approver"), default);
        await SaveAsync(scenario, 764, 27, 22, 1);
        await using var verification = fixture.CreateContext();
        var service = new PublicationService(fixture, fixture.Clock);
        var previous = await new PublicationQueries(fixture).ReadPublicAsync(scenario.EventId, default);
        Assert.NotNull(previous);
        Assert.True(previous.UnderCorrection);
        Assert.Equal(782, Assert.Single(previous.Standings).Total);
        var current = await service.PublishAsync(scenario.EventId, PostgresFixture.Actor("editor"), default);
        Assert.Equal(2, current.Revision);
        Assert.False(current.UnderCorrection);
        Assert.Equal(764, Assert.Single(current.Standings).Total);
        Assert.Equal(2, await verification.Set<PublishedSnapshot>().CountAsync(row => row.EventId == scenario.EventId));
    }
    [Fact]
    public async Task PublishedEventsRejectEditsAndEmptyOrActiveEventsCannotPublish()
    {
        var scenario = await fixture.CreateScenarioAsync();
        await Assert.ThrowsAsync<RequestRejectedException>(() => new PublicationService(fixture, fixture.Clock).PublishAsync(scenario.EventId, PostgresFixture.Actor("a"), default));
        await SaveAsync(scenario, 782, 30, 24, 0);
        var lease = await fixture.Recording().AcquireAsync(scenario.EventId, scenario.PlayerIds[1], PostgresFixture.Actor("b"), default);
        await Assert.ThrowsAsync<RequestRejectedException>(() => new PublicationService(fixture, fixture.Clock).PublishAsync(scenario.EventId, PostgresFixture.Actor("a"), default));
        await fixture.Recording().ReleaseAsync(scenario.EventId, scenario.PlayerIds[1], lease.Token, PostgresFixture.Actor("b"), default);
        await new PublicationService(fixture, fixture.Clock).PublishAsync(scenario.EventId, PostgresFixture.Actor("a"), default);
        await Assert.ThrowsAsync<RequestRejectedException>(() => fixture.Recording().AcquireAsync(scenario.EventId, scenario.PlayerIds[0], PostgresFixture.Actor("a"), default));
    }
}
