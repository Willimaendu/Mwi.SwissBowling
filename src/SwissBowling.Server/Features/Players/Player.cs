using System.ComponentModel.DataAnnotations;
using SwissBowling.Core.Players;
using SwissBowling.Server.Features.Clubs;
using SwissBowling.Server.Features.Results;

namespace SwissBowling.Server.Features.Players;

public class Player
{
    public Guid Id { get; set; } = Guid.CreateVersion7();

    [MaxLength(100)]
    public required string FirstName { get; set; }

    [MaxLength(100)]
    public required string LastName { get; set; }

    public int BirthYear { get; set; }

    public PlayerCategory Category { get; set; }

    [MaxLength(50)]
    public string? LicenseNumber { get; set; }

    public bool IsActive { get; set; } = true;

    public Guid ClubId { get; set; }

    public Club Club { get; set; } = null!;

    public List<Result> Results { get; set; } = [];

    public PlayerDto ToDto() =>
        new(Id, FirstName, LastName, BirthYear, Category, LicenseNumber, IsActive, ClubId, Club.Name, Club.ShortName);

    public void Apply(SavePlayerRequest request)
    {
        FirstName = request.FirstName.Trim();
        LastName = request.LastName.Trim();
        BirthYear = request.BirthYear;
        Category = request.Category;
        LicenseNumber = string.IsNullOrWhiteSpace(request.LicenseNumber) ? null : request.LicenseNumber.Trim();
        ClubId = request.ClubId;
        IsActive = request.IsActive;
    }
}
