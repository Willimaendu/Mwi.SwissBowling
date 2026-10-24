using Mwi.SwissBowling.Contracts;

namespace Mwi.SwissBowling.Web.Client.Features.Administration;

public partial class VenueSetupForm : SetupFormBase
{
    private string _name = "", _address = "", _lanes = "1,2";
    private string? _error;

    private async Task SaveAsync()
    {
        _error = null;
        var parts = _lanes.Split(',', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);
        if (parts.Length == 0 || parts.Any(part => !int.TryParse(part, out var number) || number <= 0))
        {
            _error = "Bahnnummern müssen positive ganze Zahlen sein.";
            return;
        }
        await CreateAsync("venues", new VenueInput(_name, _address, parts.Select(int.Parse).ToArray()));
    }
}