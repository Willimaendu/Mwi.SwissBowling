using System.ComponentModel.DataAnnotations;
using SwissBowling.Core.Import;

namespace SwissBowling.Server.Features.Import;

/// <summary>Ein Import-/Aktualisierungslauf mit Protokoll.</summary>
public class ImportRun
{
    public Guid Id { get; set; } = Guid.CreateVersion7();

    public DateTimeOffset StartedAt { get; set; } = DateTimeOffset.UtcNow;

    public DateTimeOffset? FinishedAt { get; set; }

    public ImportRunStatus Status { get; set; } = ImportRunStatus.Running;

    public bool IncludeArchive { get; set; }

    [MaxLength(300)]
    public string? CurrentStep { get; set; }

    public List<ImportLogEntry> Entries { get; set; } = [];

    public ImportRunDto ToDto() => new(
        Id, StartedAt, FinishedAt, Status, IncludeArchive, CurrentStep,
        Entries.Sum(e => e.Clubs), Entries.Sum(e => e.Players), Entries.Sum(e => e.Results), Entries.Count(e => e.IsError),
        Entries.OrderBy(e => e.Time).Select(e => e.ToDto()).ToList());
}

public class ImportLogEntry
{
    public Guid Id { get; set; } = Guid.CreateVersion7();

    public Guid RunId { get; set; }

    public ImportRun Run { get; set; } = null!;

    public DateTimeOffset Time { get; set; } = DateTimeOffset.UtcNow;

    [MaxLength(300)]
    public required string Source { get; set; }

    public int Clubs { get; set; }

    public int Players { get; set; }

    public int Results { get; set; }

    [MaxLength(2000)]
    public string? Message { get; set; }

    public bool IsError { get; set; }

    public ImportLogEntryDto ToDto() => new(Time, Source, Clubs, Players, Results, Message, IsError);
}

/// <summary>Ergebnis eines einzelnen Importschritts.</summary>
public sealed record ImportSummary(string Source, int Clubs, int Players, int Results);
