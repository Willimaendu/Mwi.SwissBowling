using Mwi.SwissBowling.Contracts;

namespace Mwi.SwissBowling.Web.Client.Features.Administration;

public partial class MembershipSetupForm : SetupFormBase
{
    private Guid _season, _player, _club;
    private PlayerCategory _category = PlayerCategory.Guest;
    private Task SaveAsync() => CreateAsync("members", new SeasonMemberInput(_season, _player, _club == Guid.Empty ? null : _club, _category));
}
