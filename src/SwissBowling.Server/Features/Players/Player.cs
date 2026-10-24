using System.ComponentModel.DataAnnotations;
using SwissBowling.Core.Players;
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

    /// <summary>SFKV-Lizenznummer – eindeutiger Schlüssel für den Import.</summary>
    [MaxLength(20)]
    public string? LicenseNumber { get; set; }

    public PlayerCategory Category { get; set; }

    public bool IsActive { get; set; } = true;

    public Guid ClubId { get; set; }

    public Club Club { get; set; } = null!;

    public List<Result> Results { get; set; } = [];

    public PlayerDto ToDto() => new(Id, Name, City, LicenseNumber, Category, IsActive, ClubId, Club.Name);

    public void Apply(SavePlayerRequest request)
    {
        Name = request.Name.Trim();
        City = string.IsNullOrWhiteSpace(request.City) ? null : request.City.Trim();
        LicenseNumber = string.IsNullOrWhiteSpace(request.LicenseNumber) ? null : request.LicenseNumber.Trim();
        Category = request.Category;
        ClubId = request.ClubId;
        IsActive = request.IsActive;
    }
}
