using System.ComponentModel.DataAnnotations;

namespace SwissBowling.Core.Import;

public static class ImportRoutes
{
    public const string Base = "api/import";

    /// <summary>Stammdaten (Unterverbände, Vorstand, Mitglieder, Klubs, Kegelbahnen) von www.sfkv.ch – der Master.</summary>
    public const string MasterData = Base + "/sfkv";

    /// <summary>Auf UVweb aktuell publizierte Ranglisten (HTML).</summary>
    public const string Current = Base + "/uvweb";

    public static string Championship(int msId, int uvId, int ranglisteId) => $"{Current}/{msId}?uvId={uvId}&ranglisteId={ranglisteId}";

    /// <summary>Ranglisten-Archiv (PDF) eines Unterverbands.</summary>
    public const string Archive = Current + "/archive";

    public static string ArchiveOf(int uvId) => $"{Archive}?uvId={uvId}";
}

/// <summary>Eine aktuell publizierte Rangliste (Kennung: msID, uvID und Ranglisten-Slot).</summary>
public sealed record UvwebChampionshipDto(int MsId, int UvId, int RanglisteId, string Association, string Name, string UpdatedAt, bool IsImported);

/// <summary>Eine archivierte Rangliste als PDF.</summary>
public sealed record UvwebArchiveEntryDto(int MsId, int UvId, string Name, string PdfUrl, string InsertedAt, bool IsImported);

public sealed record ImportArchiveRequest(int MsId, int UvId, [property: Required, Url] string PdfUrl);

/// <summary>Zusammenfassung eines Importlaufs (Anzahl angelegter bzw. aktualisierter Einträge).</summary>
public sealed record ImportSummaryDto(string Source, int Clubs, int Players, int Results);
