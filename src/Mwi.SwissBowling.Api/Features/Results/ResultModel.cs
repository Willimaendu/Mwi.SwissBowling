using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Mwi.SwissBowling.Api.Features.Setup;

namespace Mwi.SwissBowling.Api.Features.Results;

public sealed class PlayerResult
{
    public Guid EventId { get; set; }
    public Guid PlayerId { get; set; }
    public Guid SeasonId { get; set; }
    public int Total { get; set; }
    public int Nines { get; set; }
    public int Eights { get; set; }
    public bool Interrupted { get; set; }
    public long Version { get; set; }
}
public sealed class EditingLease
{
    public Guid EventId { get; set; }
    public Guid PlayerId { get; set; }
    public Guid Token { get; set; }
    public string Issuer { get; set; } = "";
    public string Subject { get; set; } = "";
    public DateTimeOffset ExpiresAt { get; set; }
}
public sealed class EditingLeaseOptions
{
    public int DurationSeconds { get; set; } = 300;
    public int RenewalSeconds { get; set; } = 30;
}
public sealed class ResultConfiguration : IEntityTypeConfiguration<PlayerResult>, IEntityTypeConfiguration<EditingLease>
{
    public void Configure(EntityTypeBuilder<PlayerResult> builder)
    {
        builder.ToTable("Results"); builder.HasKey(row => new { row.EventId, row.PlayerId });
        builder.Property(row => row.Version).IsConcurrencyToken();
        builder.HasOne<ChampionshipEvent>().WithMany().HasForeignKey(row => row.EventId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<SeasonMember>().WithMany().HasForeignKey(row => new { row.SeasonId, row.PlayerId }).OnDelete(DeleteBehavior.Restrict);
    }
    public void Configure(EntityTypeBuilder<EditingLease> builder)
    {
        builder.ToTable("EditingLeases"); builder.HasKey(row => new { row.EventId, row.PlayerId });
        builder.Property(row => row.Issuer).HasMaxLength(500); builder.Property(row => row.Subject).HasMaxLength(200);
        builder.HasOne<ChampionshipEvent>().WithMany().HasForeignKey(row => row.EventId).OnDelete(DeleteBehavior.Cascade);
        builder.HasOne<Player>().WithMany().HasForeignKey(row => row.PlayerId).OnDelete(DeleteBehavior.Restrict);
        builder.HasIndex(row => row.ExpiresAt);
    }
}
