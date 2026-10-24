using Mwi.SwissBowling.Contracts;

namespace Mwi.SwissBowling.Web.Client.Features.Administration;

public partial class SeasonSetupForm : SetupFormBase
{
    private int _year = DateTime.Today.Year;
    private Task SaveAsync() => CreateAsync("seasons", new SeasonInput(_year));
}
