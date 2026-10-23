using System.Net;
using Mwi.SwissBowling.Contracts;
using Mwi.SwissBowling.Web.Client.Infrastructure;

namespace Mwi.SwissBowling.Web.Client.Features.Administration;

public sealed class EditingLeaseSession(IAdminApi api, Guid eventId, Guid playerId, TimeProvider clock) : IAsyncDisposable
{
    private readonly string _path = $"events/{eventId}/leases/{playerId}";
    private CancellationTokenSource? _renewal;
    private Task? _renewalTask;
    public LeaseDto? Lease { get; private set; }
    public bool IsValid { get; private set; }
    public Func<Task>? RenewalFailed { get; set; }

    public async Task AcquireAsync()
    {
        if (Lease is not null) throw new InvalidOperationException("A lease is already held.");
        Lease = await api.PostAsync<LeaseDto>(_path, new { });
        IsValid = true;
        _renewal = new CancellationTokenSource();
        _renewalTask = RenewAsync(_renewal.Token);
    }

    public void Invalidate() => IsValid = false;

    private async Task RenewAsync(CancellationToken cancellationToken)
    {
        try
        {
            while (Lease is not null)
            {
                await Task.Delay(TimeSpan.FromSeconds(Lease.RenewAfterSeconds), clock, cancellationToken);
                Lease = await api.PutAsync<LeaseDto>(_path, new LeaseTokenInput(Lease.Token), cancellationToken);
            }
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested) { }
        catch (Exception exception) when (exception is ApiException or HttpRequestException or OperationCanceledException)
        {
            IsValid = false;
            if (RenewalFailed is not null) await RenewalFailed();
        }
    }

    public async Task StopRenewalAsync()
    {
        if (_renewal is null) return;
        await _renewal.CancelAsync();
        if (_renewalTask is not null) await _renewalTask;
        _renewal.Dispose();
        _renewal = null;
        _renewalTask = null;
    }

    public async Task ReleaseAsync(CancellationToken cancellationToken = default)
    {
        await StopRenewalAsync();
        IsValid = false;
        if (Lease is null) return;
        try { await api.PostAsync(_path + "/release", new LeaseTokenInput(Lease.Token), cancellationToken); }
        catch (ApiException exception) when (exception.StatusCode == HttpStatusCode.Conflict) { }
        Lease = null;
    }

    public async ValueTask DisposeAsync()
    {
        await StopRenewalAsync();
        if (Lease is null) return;
        try
        {
            using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(3));
            await ReleaseAsync(timeout.Token);
        }
        catch (Exception exception) when (exception is ApiException or HttpRequestException or OperationCanceledException) { }
    }
}
