using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Mwi.SwissBowling.Api.Features.Identity;

public sealed class ApplicationAccount
{
    public Guid Id { get; set; }
    public string Issuer { get; set; } = "";
    public string Subject { get; set; } = "";
    public string DisplayName { get; set; } = "";
    public bool IsApproved { get; set; }
    public Guid? PlayerId { get; set; }
    public DateTimeOffset RegisteredAt { get; set; }
}

public sealed class AuditRecord
{
    public Guid Id { get; set; }
    public string Action { get; set; } = "";
    public string Resource { get; set; } = "";
    public string Issuer { get; set; } = "";
    public string Subject { get; set; } = "";
    public DateTimeOffset OccurredAt { get; set; }
    public string Details { get; set; } = "{}";
}

public sealed class AccountConfiguration : IEntityTypeConfiguration<ApplicationAccount>
{
    public void Configure(EntityTypeBuilder<ApplicationAccount> builder)
    {
        builder.ToTable("Accounts");
        builder.HasKey(account => account.Id);
        builder.Property(account => account.Issuer).HasMaxLength(500);
        builder.Property(account => account.Subject).HasMaxLength(200);
        builder.Property(account => account.DisplayName).HasMaxLength(200);
        builder.HasIndex(account => new { account.Issuer, account.Subject }).IsUnique();
        builder.HasIndex(account => account.PlayerId).IsUnique();
    }
}

public sealed class AuditConfiguration : IEntityTypeConfiguration<AuditRecord>
{
    public void Configure(EntityTypeBuilder<AuditRecord> builder)
    {
        builder.ToTable("AuditRecords");
        builder.HasKey(audit => audit.Id);
        builder.Property(audit => audit.Details).HasColumnType("jsonb");
        builder.HasIndex(audit => new { audit.Resource, audit.OccurredAt });
    }
}
