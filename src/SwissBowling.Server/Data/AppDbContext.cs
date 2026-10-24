using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using SwissBowling.Server.Auth;
using SwissBowling.Server.Features.Associations;
using SwissBowling.Server.Features.Championships;
using SwissBowling.Server.Features.Clubs;
using SwissBowling.Server.Features.Import;
using SwissBowling.Server.Features.Players;
using SwissBowling.Server.Features.Results;
using SwissBowling.Server.Features.Venues;

namespace SwissBowling.Server.Data;

public class AppDbContext(DbContextOptions<AppDbContext> options) : IdentityDbContext<AppUser, IdentityRole<Guid>, Guid>(options)
{
    public DbSet<Association> Associations => Set<Association>();

    public DbSet<BoardMember> BoardMembers => Set<BoardMember>();

    public DbSet<Venue> Venues => Set<Venue>();

    public DbSet<Club> Clubs => Set<Club>();

    public DbSet<Player> Players => Set<Player>();

    public DbSet<Championship> Championships => Set<Championship>();

    public DbSet<Result> Results => Set<Result>();

    public DbSet<ImportRun> ImportRuns => Set<ImportRun>();

    public DbSet<ImportLogEntry> ImportLogEntries => Set<ImportLogEntry>();

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        builder.Entity<ImportRun>().HasMany(r => r.Entries).WithOne(e => e.Run).OnDelete(DeleteBehavior.Cascade);

        builder.Entity<Association>().HasIndex(a => a.ExternalId).IsUnique();
        builder.Entity<Association>().HasMany(a => a.Clubs).WithOne(c => c.Association).OnDelete(DeleteBehavior.Restrict);
        builder.Entity<Association>().HasMany(a => a.Players).WithOne(p => p.Association).OnDelete(DeleteBehavior.Restrict);
        builder.Entity<Association>().HasMany(a => a.Venues).WithOne(v => v.Association).OnDelete(DeleteBehavior.Cascade);
        builder.Entity<Association>().HasMany(a => a.Board).WithOne(b => b.Association).OnDelete(DeleteBehavior.Cascade);

        builder.Entity<Club>().HasIndex(c => c.ExternalId).IsUnique();
        builder.Entity<Club>().HasMany(c => c.Players).WithOne(p => p.Club).OnDelete(DeleteBehavior.SetNull);

        builder.Entity<Player>().HasIndex(p => p.LicenseNumber).IsUnique();
        builder.Entity<Player>().HasMany(p => p.Results).WithOne(r => r.Player).OnDelete(DeleteBehavior.Restrict);

        builder.Entity<Championship>().HasIndex(c => c.ExternalId).IsUnique();
        builder.Entity<Championship>().Ignore(c => c.MaxPins);
        builder.Entity<Championship>().HasOne(c => c.Association).WithMany().OnDelete(DeleteBehavior.Restrict);
        builder.Entity<Championship>().HasMany(c => c.Results).WithOne(r => r.Championship).OnDelete(DeleteBehavior.Cascade);

        builder.Entity<Result>().HasIndex(r => new { r.ChampionshipId, r.PlayerId, r.Discipline }).IsUnique();
    }
}
