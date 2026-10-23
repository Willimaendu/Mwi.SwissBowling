using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Mwi.SwissBowling.Contracts;

namespace Mwi.SwissBowling.Api.Features.Setup;

public sealed class Season
{
    public Guid Id { get; set; }
    public int Year { get; set; }
    public bool RosterLocked { get; set; }
}
public sealed class Club
{
    public Guid Id { get; set; }
    public string Name { get; set; } = "";
    public string? Email { get; set; }
    public string? Phone { get; set; }
}
public sealed class SeasonMember
{
    public Guid SeasonId { get; set; }
    public Guid PlayerId { get; set; }
    public Guid? ClubId { get; set; }
    public PlayerCategory Category { get; set; }
}
public sealed class Venue
{
    public Guid Id { get; set; }
    public string Name { get; set; } = "";
    public string Address { get; set; } = "";
    public List<Lane> Lanes { get; set; } = [];
}
public sealed class Lane
{
    public Guid VenueId { get; set; }
    public int Number { get; set; }
}
public sealed class AnnualChampionship
{
    public Guid Id { get; set; }
    public Guid SeasonId { get; set; }
    public string Name { get; set; } = "";
}
public sealed class ChampionshipEvent
{
    public Guid Id { get; set; }
    public Guid AnnualChampionshipId { get; set; }
    public string Name { get; set; } = "";
    public Guid VenueId { get; set; }
    public DateOnly StartDate { get; set; }
    public DateOnly EndDate { get; set; }
    public int? MaximumThrows { get; set; }
    public EventState State { get; set; }
    public int PublishedRevision { get; set; }
}

public sealed class SetupConfiguration : IEntityTypeConfiguration<Season>, IEntityTypeConfiguration<Club>, IEntityTypeConfiguration<SeasonMember>, IEntityTypeConfiguration<Venue>, IEntityTypeConfiguration<Lane>, IEntityTypeConfiguration<AnnualChampionship>, IEntityTypeConfiguration<ChampionshipEvent>
{
    public void Configure(EntityTypeBuilder<Season> builder)
    {
        builder.ToTable("Seasons"); builder.HasKey(row => row.Id); builder.HasIndex(row => row.Year).IsUnique();
    }
    public void Configure(EntityTypeBuilder<Club> builder)
    {
        builder.ToTable("Clubs"); builder.HasKey(row => row.Id);
        builder.Property(row => row.Name).HasMaxLength(160);
        builder.Property(row => row.Email).HasMaxLength(254);
        builder.Property(row => row.Phone).HasMaxLength(50);
    }
    public void Configure(EntityTypeBuilder<SeasonMember> builder)
    {
        builder.ToTable("SeasonMembers"); builder.HasKey(row => new { row.SeasonId, row.PlayerId });
        builder.HasOne<Season>().WithMany().HasForeignKey(row => row.SeasonId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<Player>().WithMany().HasForeignKey(row => row.PlayerId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<Club>().WithMany().HasForeignKey(row => row.ClubId).OnDelete(DeleteBehavior.Restrict);
    }
    public void Configure(EntityTypeBuilder<Venue> builder)
    {
        builder.ToTable("Venues"); builder.HasKey(row => row.Id);
        builder.Property(row => row.Name).HasMaxLength(160); builder.Property(row => row.Address).HasMaxLength(300);
        builder.HasMany(row => row.Lanes).WithOne().HasForeignKey(row => row.VenueId).OnDelete(DeleteBehavior.Cascade);
    }
    public void Configure(EntityTypeBuilder<Lane> builder)
    {
        builder.ToTable("Lanes"); builder.HasKey(row => new { row.VenueId, row.Number });
    }
    public void Configure(EntityTypeBuilder<AnnualChampionship> builder)
    {
        builder.ToTable("AnnualChampionships"); builder.HasKey(row => row.Id); builder.Property(row => row.Name).HasMaxLength(160);
        builder.HasOne<Season>().WithMany().HasForeignKey(row => row.SeasonId).OnDelete(DeleteBehavior.Restrict);
    }
    public void Configure(EntityTypeBuilder<ChampionshipEvent> builder)
    {
        builder.ToTable("Events"); builder.HasKey(row => row.Id); builder.Property(row => row.Name).HasMaxLength(160);
        builder.HasOne<AnnualChampionship>().WithMany().HasForeignKey(row => row.AnnualChampionshipId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<Venue>().WithMany().HasForeignKey(row => row.VenueId).OnDelete(DeleteBehavior.Restrict);
    }
}
