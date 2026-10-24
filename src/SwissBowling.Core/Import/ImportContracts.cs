namespace SwissBowling.Core.Import;

public static class ImportRoutes
{
    /// <summary>Importläufe: POST startet einen Lauf, GET listet die bisherigen.</summary>
    public const string Runs = "api/import/runs";

    public static string Run(Guid id) => $"{Runs}/{id}";
}

public enum ImportRunStatus
{
    Running,
    Completed,
    Failed
}

/// <summary>Startet einen Lauf. Das Archiv (PDFs) wird nur für noch nicht importierte Meisterschaften geladen.</summary>
public sealed record StartImportRequest(bool IncludeArchive = true);

/// <summary>Ein Protokolleintrag: was wurde aus welcher Quelle übernommen (angelegt oder aktualisiert).</summary>
public sealed record ImportLogEntryDto(DateTimeOffset Time, string Source, int Clubs, int Players, int Results, string? Message, bool IsError);

public sealed record ImportRunDto(
    Guid Id,
    DateTimeOffset StartedAt,
    DateTimeOffset? FinishedAt,
    ImportRunStatus Status,
    bool IncludeArchive,
    string? CurrentStep,
    int Clubs,
    int Players,
    int Results,
    int Errors,
    IReadOnlyList<ImportLogEntryDto> Entries);
