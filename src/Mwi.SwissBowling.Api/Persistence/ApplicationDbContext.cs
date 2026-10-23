using Microsoft.EntityFrameworkCore;
using Mwi.SwissBowling.Api.Features.Identity;

namespace Mwi.SwissBowling.Api.Persistence;

public sealed partial class ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : DbContext(options)
{
    public DbSet<ApplicationAccount> Accounts => Set<ApplicationAccount>();
    public DbSet<AuditRecord> AuditRecords => Set<AuditRecord>();
    protected override void OnModelCreating(ModelBuilder modelBuilder) => modelBuilder.ApplyConfigurationsFromAssembly(typeof(ApplicationDbContext).Assembly);

    public void Audit(string action, string resource, string issuer, string subject, DateTimeOffset now, string details = "{}") => AuditRecords.Add(new AuditRecord
    {
        Id = Guid.NewGuid(), Action = action, Resource = resource, Issuer = issuer, Subject = subject, OccurredAt = now, Details = details
    });
}
