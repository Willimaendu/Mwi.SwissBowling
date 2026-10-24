using System.ComponentModel.DataAnnotations;

namespace SwissBowling.Core.Import;

public static class ImportRoutes
{
    /// <summary>Auf UVweb aktuell publizierte Ranglisten (HTML).</summary>
    public const string Available = "api/import/uvweb";

    public static string Championship(int msId, int uvId, int ranglisteId) => $"{Available}/{msId}?uvId={uvId}&ranglisteId={ranglisteId}";

    /// <summary>Unterverbände auf UVweb.</summary>
    public const string Associations = Available + "/associations";

    /// <summary>Ranglisten-Archiv (PDF) eines Unterverbands.</summary>
    public const string Archive = Available + "/archive";

    public static string ArchiveOf(int uvId) => $"{Archive}?uvId={uvId}";
}

public sealed record UvwebAssociationDto(int UvId, string Name);

/// <summary>Eine aktuell publizierte Rangliste (Kennung: msID, uvID und Ranglisten-Slot).</summary>
public sealed record UvwebChampionshipDto(int MsId, int UvId, int RanglisteId, string Association, string Name, string UpdatedAt, bool IsImported);

/// <summary>Eine archivierte Rangliste als PDF.</summary>
public sealed record UvwebArchiveEntryDto(int MsId, int UvId, string Name, string PdfUrl, string InsertedAt, bool IsImported);

public sealed record ImportArchiveRequest(int MsId, int UvId, [property: Required, Url] string PdfUrl);

public sealed record ImportSummaryDto(string Championship, int Clubs, int Players, int Results);
