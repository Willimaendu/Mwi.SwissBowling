using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Http.Connections;
using Microsoft.AspNetCore.SignalR.Client;

namespace Mwi.SwissBowling.Web.Client.Infrastructure;

public partial class LiveUpdates : ComponentBase, IAsyncDisposable
{
    [Inject] private NavigationManager Navigation { get; set; } = default!;
    [Parameter] public bool Administration { get; set; }
    [Parameter] public EventCallback Changed { get; set; }

    private readonly CancellationTokenSource _lifetime = new();
    private HubConnection? _connection;
    private Task? _runner;
    private bool _connected, _refreshing, _pending;

    protected override Task OnAfterRenderAsync(bool firstRender)
    {
        if (!firstRender) return Task.CompletedTask;
        _connection = new HubConnectionBuilder()
            .WithUrl(Navigation.ToAbsoluteUri(Administration ? "live/administration" : "live/public"), options =>
            {
                // Direct WebSockets route every replica independently, without negotiation affinity.
                options.Transports = HttpTransportType.WebSockets;
                options.SkipNegotiation = true;
            })
            .WithAutomaticReconnect()
            .Build();
        _connection.On("Changed", RequestRefresh);
        _connection.Reconnecting += _ => SetConnectedAsync(false);
        _connection.Reconnected += async _ => { await SetConnectedAsync(true); RequestRefresh(); };
        _connection.Closed += _ => SetConnectedAsync(false);
        _runner = ConnectAsync();
        return Task.CompletedTask;
    }

    private async Task ConnectAsync()
    {
        var synchronizationTicks = 0;
        while (!_lifetime.IsCancellationRequested)
        {
            try
            {
                if (_connection!.State == HubConnectionState.Disconnected)
                {
                    await _connection.StartAsync(_lifetime.Token);
                    await SetConnectedAsync(true);
                    RequestRefresh(); // Includes changes missed before connecting or during an outage.
                    synchronizationTicks = 0;
                }
                // Recover failed HTTP reloads even when no further database write occurs.
                if (_connection.State == HubConnectionState.Connected && ++synchronizationTicks >= 12)
                {
                    synchronizationTicks = 0;
                    RequestRefresh();
                }
                await Task.Delay(TimeSpan.FromSeconds(5), _lifetime.Token);
            }
            catch (OperationCanceledException) when (_lifetime.IsCancellationRequested) { break; }
            catch (Exception exception) when (exception is HttpRequestException or IOException or InvalidOperationException or System.Net.WebSockets.WebSocketException or TimeoutException)
            {
                await SetConnectedAsync(false);
                try { await Task.Delay(TimeSpan.FromSeconds(5), _lifetime.Token); }
                catch (OperationCanceledException) when (_lifetime.IsCancellationRequested) { break; }
            }
        }
    }

    private Task SetConnectedAsync(bool value) => InvokeAsync(() =>
    {
        if (_lifetime.IsCancellationRequested) return;
        _connected = value;
        StateHasChanged();
    });

    // Refresh can remove this component. Do not await it from the connection loop
    // or hub handler, because disposal awaits both and would form a wait cycle.
    private void RequestRefresh() => _ = RefreshAndObserveAsync();

    private async Task RefreshAndObserveAsync()
    {
        try { await RefreshAsync(); }
        catch (OperationCanceledException) when (_lifetime.IsCancellationRequested) { }
        catch (Exception exception)
        {
            if (!_lifetime.IsCancellationRequested) await DispatchExceptionAsync(exception);
        }
    }

    private Task RefreshAsync() => InvokeAsync(async () =>
    {
        if (_lifetime.IsCancellationRequested) return;
        _pending = true;
        if (_refreshing) return;
        _refreshing = true;
        try
        {
            while (_pending && !_lifetime.IsCancellationRequested)
            {
                _pending = false;
                await Changed.InvokeAsync();
            }
        }
        finally { _refreshing = false; }
    });

    public async ValueTask DisposeAsync()
    {
        await _lifetime.CancelAsync();
        if (_runner is not null) await _runner;
        if (_connection is not null) await _connection.DisposeAsync();
        _lifetime.Dispose();
    }
}
