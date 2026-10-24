using System.ComponentModel.DataAnnotations;
using SwissBowling.Server.Features.Championships;

namespace SwissBowling.Server.Features.Seasons;

public class Season
{
    public Guid Id { get; set; } = Guid.CreateVersion7();

    [MaxLength(50)]
    public required string Name { get; set; }

    public DateOnly StartDate { get; set; }

    public DateOnly EndDate { get; set; }

    public List<Championship> Championships { get; set; } = [];
}
