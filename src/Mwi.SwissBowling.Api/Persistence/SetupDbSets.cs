using Microsoft.EntityFrameworkCore;
using Mwi.SwissBowling.Api.Features.Setup;

namespace Mwi.SwissBowling.Api.Persistence;

public sealed partial class ApplicationDbContext
{
    public DbSet<Player> Players => Set<Player>();
    public DbSet<Club> Clubs => Set<Club>();
    public DbSet<Season> Seasons => Set<Season>();
    public DbSet<SeasonMember> SeasonMembers => Set<SeasonMember>();
    public DbSet<Venue> Venues => Set<Venue>();
    public DbSet<AnnualChampionship> AnnualChampionships => Set<AnnualChampionship>();
    public DbSet<ChampionshipEvent> Events => Set<ChampionshipEvent>();
}
