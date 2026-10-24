using Mwi.SwissBowling.Contracts;

namespace Mwi.SwissBowling.Web.Client.Features.Administration;

public partial class ClubSetupForm : SetupFormBase
{
    private string _name = "", _email = "", _phone = "";
    private Task SaveAsync() => CreateAsync("clubs", new ClubInput(_name, Optional(_email), Optional(_phone)));
}
