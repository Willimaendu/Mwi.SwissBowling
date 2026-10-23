using System.Net;
using Mwi.SwissBowling.Contracts;
using Mwi.SwissBowling.Web.Client.Features.Administration;
using Mwi.SwissBowling.Web.Client.Infrastructure;

namespace Mwi.SwissBowling.UnitTests;

public sealed class EditingLeaseSessionTests
{
    [Fact]
    public async Task ReleaseCancelsRenewalAndClearsOwnership()
    {
        var api = new LeaseApi();
        await using var session = new EditingLeaseSession(api, Guid.NewGuid(), Guid.NewGuid(), TimeProvider.System);
        await session.AcquireAsync();
        Assert.True(session.IsValid);
        await Assert.ThrowsAsync<InvalidOperationException>(() => session.AcquireAsync());
        await session.ReleaseAsync();
        Assert.Null(session.Lease);
        Assert.False(session.IsValid);
        Assert.Equal(1, api.Releases);
        Assert.Equal(0, api.Renewals);
    }

    [Fact]
    public async Task ConflictOnReleaseMeansOwnershipHasEnded()
    {
        var api = new LeaseApi { ReleaseFailure = new ApiException("expired", HttpStatusCode.Conflict) };
        await using var session = new EditingLeaseSession(api, Guid.NewGuid(), Guid.NewGuid(), TimeProvider.System);
        await session.AcquireAsync();
        await session.ReleaseAsync();
        Assert.Null(session.Lease);
        Assert.False(session.IsValid);
    }

    [Fact]
    public async Task UnconfirmedReleaseRetainsTokenButDisablesSaving()
    {
        var api = new LeaseApi { ReleaseFailure = new HttpRequestException() };
        await using var session = new EditingLeaseSession(api, Guid.NewGuid(), Guid.NewGuid(), TimeProvider.System);
        await session.AcquireAsync();
        await Assert.ThrowsAsync<HttpRequestException>(() => session.ReleaseAsync());
        Assert.Equal(api.Lease.Token, session.Lease?.Token);
        Assert.False(session.IsValid);
    }

    [Fact]
    public async Task RenewalFailureRetainsLeaseAndNotifiesOnceWithoutRetrying()
    {
        var api = new LeaseApi { Lease = new LeaseDto(Guid.NewGuid(), DateTimeOffset.MaxValue, 0) };
        var notifications = 0;
        await using var session = new EditingLeaseSession(api, Guid.NewGuid(), Guid.NewGuid(), TimeProvider.System)
        {
            RenewalFailed = () => { notifications++; return Task.CompletedTask; }
        };
        await session.AcquireAsync();
        Assert.False(session.IsValid);
        Assert.NotNull(session.Lease);
        Assert.Equal(1, notifications);
        Assert.Equal(1, api.Renewals);
    }

    private sealed class LeaseApi : IAdminApi
    {
        public LeaseDto Lease { get; init; } = new(Guid.NewGuid(), DateTimeOffset.MaxValue, 3600);
        public Exception? ReleaseFailure { get; init; }
        public int Releases { get; private set; }
        public int Renewals { get; private set; }
        public Task<T> GetAsync<T>(string path, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task<T> PostAsync<T>(string path, object body, CancellationToken cancellationToken = default) => Task.FromResult((T)(object)Lease);
        public Task PostAsync(string path, object body, CancellationToken cancellationToken = default)
        {
            Releases++;
            return ReleaseFailure is null ? Task.CompletedTask : Task.FromException(ReleaseFailure);
        }
        public Task PutAsync(string path, object body, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task<T> PutAsync<T>(string path, object body, CancellationToken cancellationToken = default)
        {
            Renewals++;
            return Task.FromException<T>(new HttpRequestException());
        }
    }
}
