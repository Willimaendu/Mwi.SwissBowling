using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Mwi.SwissBowling.Api.Features.Setup;

public sealed class Player
{
    public Guid Id { get; set; }
    public string FirstName { get; set; } = "";
    public string LastName { get; set; } = "";
    public string? SfkvNumber { get; set; }
    public DateOnly? DateOfBirth { get; set; }
    public string? Email { get; set; }
    public string? Phone { get; set; }
}

public sealed class PlayerConfiguration : IEntityTypeConfiguration<Player>
{
    public void Configure(EntityTypeBuilder<Player> builder)
    {
        builder.ToTable("Players");
        builder.HasKey(player => player.Id);
        builder.Property(player => player.FirstName).HasMaxLength(100);
        builder.Property(player => player.LastName).HasMaxLength(100);
        builder.Property(player => player.SfkvNumber).HasMaxLength(30);
        builder.Property(player => player.Email).HasMaxLength(254);
        builder.Property(player => player.Phone).HasMaxLength(50);
        builder.HasIndex(player => player.SfkvNumber).IsUnique();
    }
}
