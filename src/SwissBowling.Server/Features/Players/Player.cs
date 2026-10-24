using System.ComponentModel.DataAnnotations;
using SwissBowling.Core.Players;
using SwissBowling.Server.Features.Associations;
using SwissBowling.Server.Features.Clubs;
using SwissBowling.Server.Features.Results;

namespace SwissBowling.Server.Features.Players;

public class Player
{
    public Guid Id { get; set; } = Guid.CreateVersion7();

    /// <summary>«Nachname Vorname» wie bei der SFKV üblich.</summary>
    [MaxLength(200)]
    public required string Name { get; set; }

    [MaxLength(100)]
    public string? City { get; set; }

    /// <summary>SFKV-Mitglieder-ID (Lizenz) – eindeutiger Schlüssel für den Import.</summary>
    [MaxLength(20)]
    public string? LicenseNumber { get; set; }

    public DateOnly? BirthDate { get; set; }

    public PlayerCategory Category { get; set; }

    public bool IsActive { get; set; } = true;

    /// <summary>Null = Einzelmitglied des Unterverbands.</summary>
    public Guid? ClubId { get; set; }

    public Club? Club { get; set; }

    public Guid AssociationId { get; set; }

    public Association Association { get; set; } = null!;

    public List<Result> Results { get; set; } = [];

    public PlayerDto ToDto() => new(Id, Name, City, LicenseNumber, BirthDate, Category, IsActive, ClubId, Club?.Name, AssociationId, Association.Name);

    public void Apply(SavePlayerRequest request)
    {
        Name = request.Name.Trim();
        City = string.IsNullOrWhiteSpace(request.City) ? null : request.City.Trim();
        LicenseNumber = string.IsNullOrWhiteSpace(request.LicenseNumber) ? null : request.LicenseNumber.Trim();
        BirthDate = request.BirthDate;
        Category = request.Category;
        IsActive = request.IsActive;
    }
}
