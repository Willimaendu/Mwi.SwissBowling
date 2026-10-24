using Mwi.SwissBowling.Contracts;

namespace Mwi.SwissBowling.Web.Client.Features.Administration;

public partial class EventSetupForm : SetupFormBase
{
    private Guid _annual, _venue;
    private string _name = "";
    private DateOnly _start = DateOnly.FromDateTime(DateTime.Today), _end = DateOnly.FromDateTime(DateTime.Today);
    private int? _throws;
    private Task SaveAsync() => CreateAsync("events", new EventInput(_annual, _name, _venue, _start, _end, _throws));
}
