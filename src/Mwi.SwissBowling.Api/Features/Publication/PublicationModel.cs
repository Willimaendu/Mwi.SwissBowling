using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Mwi.SwissBowling.Api.Features.Setup;

namespace Mwi.SwissBowling.Api.Features.Publication;

public sealed class PublishedSnapshot
{
    public Guid EventId { get; set; }
    public int Revision { get; set; }
    public string Name { get; set; } = "";
    public int Season { get; set; }
    public DateTimeOffset PublishedAt { get; set; }
    public string Payload { get; set; } = "";
}
public sealed class CorrectionRequest
{
    public Guid Id { get; set; }
    public Guid EventId { get; set; }
    public string Reason { get; set; } = "";
    public string RequestedIssuer { get; set; } = "";
    public string RequestedSubject { get; set; } = "";
    public DateTimeOffset RequestedAt { get; set; }
    public DateTimeOffset? ApprovedAt { get; set; }
    public string? ApprovedIssuer { get; set; }
    public string? ApprovedSubject { get; set; }
}
public sealed class PublicationConfiguration : IEntityTypeConfiguration<PublishedSnapshot>, IEntityTypeConfiguration<CorrectionRequest>
{
    public void Configure(EntityTypeBuilder<PublishedSnapshot> builder)
    {
        builder.ToTable("PublishedSnapshots"); builder.HasKey(row => new { row.EventId, row.Revision });
        builder.Property(row => row.Name).HasMaxLength(160); builder.Property(row => row.Payload).HasColumnType("jsonb");
        builder.HasOne<ChampionshipEvent>().WithMany().HasForeignKey(row => row.EventId).OnDelete(DeleteBehavior.Restrict);
    }
    public void Configure(EntityTypeBuilder<CorrectionRequest> builder)
    {
        builder.ToTable("CorrectionRequests"); builder.HasKey(row => row.Id);
        builder.Property(row => row.Reason).HasMaxLength(1000);
        builder.Property(row => row.RequestedIssuer).HasMaxLength(500); builder.Property(row => row.RequestedSubject).HasMaxLength(200);
        builder.Property(row => row.ApprovedIssuer).HasMaxLength(500); builder.Property(row => row.ApprovedSubject).HasMaxLength(200);
        builder.HasOne<ChampionshipEvent>().WithMany().HasForeignKey(row => row.EventId).OnDelete(DeleteBehavior.Restrict);
        builder.HasIndex(row => row.EventId).IsUnique().HasFilter("\"ApprovedAt\" IS NULL");
    }
}
