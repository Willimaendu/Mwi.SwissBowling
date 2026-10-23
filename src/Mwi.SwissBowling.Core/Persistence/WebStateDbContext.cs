using Microsoft.AspNetCore.DataProtection.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace Mwi.SwissBowling.Core.Persistence;

public sealed class WebStateDbContext(DbContextOptions<WebStateDbContext> options) : DbContext(options), IDataProtectionKeyContext
{
    public DbSet<DataProtectionKey> DataProtectionKeys { get; set; } = null!;
    public DbSet<AuthenticationSession> AuthenticationSessions => Set<AuthenticationSession>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<DataProtectionKey>().ToTable("DataProtectionKeys");
        modelBuilder.Entity<AuthenticationSession>(entity =>
        {
            entity.ToTable("AuthenticationSessions");
            entity.HasKey(session => session.Id);
            entity.Property(session => session.Id).HasMaxLength(64);
            entity.HasIndex(session => session.ExpiresAt);
        });
    }
}

public sealed class AuthenticationSession
{
    public string Id { get; set; } = "";
    public byte[] Payload { get; set; } = [];
    public DateTimeOffset ExpiresAt { get; set; }
}
