using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Mwi.SwissBowling.Contracts;
using Mwi.SwissBowling.Web.Features.Standings;

namespace Mwi.SwissBowling.Web.Components.Pages;

public partial class PublicStandings : ComponentBase
{
    [Inject] private PublicEventsClient Events { get; set; } = default!;
    [Inject] private IHttpContextAccessor Context { get; set; } = default!;
    [Inject] private IConfiguration Configuration { get; set; } = default!;
    [Parameter] public IComponentRenderMode? LiveRenderMode { get; set; } = RenderMode.InteractiveWebAssembly;
    [Parameter] public Guid EventId { get; set; }

    private PublicEventDto? _event;
    private bool _notFound;
    private bool _error;
    private string Origin => (Configuration["Site:PublicOrigin"] ?? "https://localhost:7443").TrimEnd('/');

    protected override async Task OnParametersSetAsync()
    {
        _error = false;
        _notFound = false;
        _event = null;
        try
        {
            _event = await Events.ReadAsync(EventId);
            if (_event is null)
            {
                _notFound = true;
                if (Context.HttpContext?.Response.HasStarted == false) Context.HttpContext.Response.StatusCode = 404;
                return;
            }
        }
        catch (Exception exception) when (exception is HttpRequestException or TaskCanceledException)
        {
            _error = true;
            if (Context.HttpContext?.Response.HasStarted == false) Context.HttpContext.Response.StatusCode = 503;
        }
    }
}
