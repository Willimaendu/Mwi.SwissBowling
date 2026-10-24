using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using SwissBowling.Server.Auth;
using SwissBowling.Server.Features.Championships;
using SwissBowling.Server.Features.Clubs;
using SwissBowling.Server.Features.Players;
using SwissBowling.Server.Features.Results;
using SwissBowling.Server.Features.Seasons;

namespace SwissBowling.Server.Data;

public class AppDbContext(DbContextOptions<AppDbContext> options) : IdentityDbContext<AppUser, IdentityRole<Guid>, Guid>(options)
{
    public DbSet<Club> Clubs => Set<Club>();

    public DbSet<Player> Players => Set<Player>();

    public DbSet<Season> Seasons => Set<Season>();

    public DbSet<Championship> Championships => Set<Championship>();

    public DbSet<Round> Rounds => Set<Round>();

    public DbSet<Result> Results => Set<Result>();

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        builder.Entity<Club>().HasIndex(c => c.ShortName).IsUnique();
        builder.Entity<Club>().HasMany(c => c.Players).WithOne(p => p.Club).OnDelete(DeleteBehavior.Restrict);

        builder.Entity<Player>().HasIndex(p => p.LicenseNumber).IsUnique();
        builder.Entity<Player>().HasMany(p => p.Results).WithOne(r => r.Player).OnDelete(DeleteBehavior.Restrict);

        builder.Entity<Season>().HasIndex(s => s.Name).IsUnique();
        builder.Entity<Season>().HasMany(s => s.Championships).WithOne(c => c.Season).OnDelete(DeleteBehavior.Restrict);

        builder.Entity<Championship>().Ignore(c => c.CountedRounds).Ignore(c => c.MaxPinsPerRound);
        builder.Entity<Championship>().HasMany(c => c.Rounds).WithOne(r => r.Championship).OnDelete(DeleteBehavior.Cascade);

        builder.Entity<Round>().HasIndex(r => new { r.ChampionshipId, r.Number }).IsUnique();
        builder.Entity<Round>().HasMany(r => r.Results).WithOne(x => x.Round).OnDelete(DeleteBehavior.Cascade);

        builder.Entity<Result>().HasIndex(r => new { r.RoundId, r.PlayerId }).IsUnique();
    }
}
