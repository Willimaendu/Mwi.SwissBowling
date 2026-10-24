using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Http.Connections;
using Microsoft.AspNetCore.SignalR.Client;

namespace Mwi.SwissBowling.Web.Client.Components.Controls;

public partial class LiveRefresh : ComponentBase, IAsyncDisposable
{
    [Inject] private NavigationManager Navigation { get; set; } = default!;
    [Parameter] public bool Private { get; set; }
    [Parameter] public EventCallback Changed { get; set; }

    private readonly CancellationTokenSource _stop = new();
    private HubConnection? _hub;
    private Task? _connectionTask;
    private bool _online, _reloading, _reloadRequested;

    protected override Task OnAfterRenderAsync(bool firstRender)
    {
        if (!firstRender) return Task.CompletedTask;
        _hub = new HubConnectionBuilder().WithUrl(Navigation.ToAbsoluteUri(Private ? "live/administration" : "live/public"), transport =>
        {
            transport.Transports = HttpTransportType.WebSockets;
            transport.SkipNegotiation = true;
        }).WithAutomaticReconnect().Build();
        _hub.On("Changed", (Action)QueueReload);
        _hub.Reconnecting += _ => UpdateConnectionAsync(false);
        _hub.Reconnected += async _ => { await UpdateConnectionAsync(true); QueueReload(); };
        _hub.Closed += _ => UpdateConnectionAsync(false);
        _connectionTask = KeepConnectedAsync();
        return Task.CompletedTask;
    }

    private async Task KeepConnectedAsync()
    {
        while (!_stop.IsCancellationRequested)
        {
            try
            {
                if (_hub!.State == HubConnectionState.Disconnected)
                {
                    await _hub.StartAsync(_stop.Token);
                    await UpdateConnectionAsync(true);
                }
                if (_hub.State == HubConnectionState.Connected) QueueReload();
                await Task.Delay(TimeSpan.FromSeconds(30), _stop.Token);
            }
            catch (OperationCanceledException) when (_stop.IsCancellationRequested) { return; }
            catch (Exception failure) when (failure is HttpRequestException or IOException or TimeoutException or System.Net.WebSockets.WebSocketException or OperationCanceledException)
            {
                await UpdateConnectionAsync(false);
                try { await Task.Delay(TimeSpan.FromSeconds(5), _stop.Token); }
                catch (OperationCanceledException) when (_stop.IsCancellationRequested) { return; }
            }
        }
    }

    private Task UpdateConnectionAsync(bool connected) => InvokeAsync(() =>
    {
        if (_stop.IsCancellationRequested) return;
        _online = connected;
        StateHasChanged();
    });

    // Reload may remove this component when authentication expires. Never await
    // it from the connection task or hub callback that DisposeAsync must stop.
    private void QueueReload() => _ = ReloadAndObserveAsync();

    private async Task ReloadAndObserveAsync()
    {
        try
        {
            await InvokeAsync(async () =>
            {
                if (_stop.IsCancellationRequested) return;
                _reloadRequested = true;
                if (_reloading) return;
                _reloading = true;
                try
                {
                    while (_reloadRequested && !_stop.IsCancellationRequested)
                    {
                        _reloadRequested = false;
                        await Changed.InvokeAsync();
                    }
                }
                finally { _reloading = false; }
            });
        }
        catch (OperationCanceledException) when (_stop.IsCancellationRequested) { }
        catch (Exception failure)
        {
            if (!_stop.IsCancellationRequested) await DispatchExceptionAsync(failure);
        }
    }

    public async ValueTask DisposeAsync()
    {
        await _stop.CancelAsync();
        if (_connectionTask is not null) await _connectionTask;
        if (_hub is not null) await _hub.DisposeAsync();
        _stop.Dispose();
    }
}
