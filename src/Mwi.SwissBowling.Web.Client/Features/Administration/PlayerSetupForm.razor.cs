using Mwi.SwissBowling.Contracts;

namespace Mwi.SwissBowling.Web.Client.Features.Administration;

public partial class PlayerSetupForm : SetupFormBase
{
    private Guid _id;
    private string _firstName = "", _lastName = "", _sfkv = "", _email = "", _phone = "";
    private DateOnly? _birthDate;

    private Task SaveAsync() => Execute(async () =>
    {
        var input = new PlayerInput(_firstName, _lastName, Optional(_sfkv), _birthDate, Optional(_email), Optional(_phone));
        if (_id == Guid.Empty) await Api.PostAsync("setup/players", input);
        else await Api.PutAsync($"setup/players/{_id}", input);
    });

    private void ChoosePlayer(Guid id)
    {
        _id = id;
        var player = Data.Players.SingleOrDefault(row => row.Id == _id);
        _firstName = player?.FirstName ?? "";
        _lastName = player?.LastName ?? "";
        _sfkv = player?.SfkvNumber ?? "";
        _birthDate = player?.DateOfBirth;
        _email = player?.Email ?? "";
        _phone = player?.Phone ?? "";
    }
}
