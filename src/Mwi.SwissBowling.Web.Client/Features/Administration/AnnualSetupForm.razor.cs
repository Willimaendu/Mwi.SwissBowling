using Mwi.SwissBowling.Contracts;

namespace Mwi.SwissBowling.Web.Client.Features.Administration;

public partial class AnnualSetupForm : SetupFormBase
{
    private Guid _season;
    private string _name = "";
    private Task SaveAsync() => CreateAsync("annual-championships", new AnnualChampionshipInput(_season, _name));
}
