using System.Globalization;
using System.Text.RegularExpressions;
using AngleSharp.Dom;
using AngleSharp.Html.Parser;
using SwissBowling.Core.Players;
using SwissBowling.Core.Results;

namespace SwissBowling.Server.Features.Import;

/// <summary>Stammdaten einer Meisterschaft von UVweb (msangaben.aspx).</summary>
public sealed record UvwebChampionship(
    int MsId,
    int UvId,
    string Association,
    string Name,
    DateOnly StartDate,
    DateOnly EndDate,
    int Throws,
    string? Organizer,
    string? Venue,
    string? City);

/// <summary>Resultat aus einer aktuellen HTML-Rangliste – mit Lizenznummer und klubID.</summary>
public sealed record UvwebResult(string License, string Name, string? City, PlayerCategory Category, Discipline Discipline, int Pins, int Nines, bool Award, int ClubId);

/// <summary>Eine aktuell publizierte Rangliste (Kennung: msID, uvID und Ranglisten-Slot).</summary>
public sealed record UvwebCurrentEntry(int MsId, int UvId, int RanglisteId, string Association, string Name);

/// <summary>Eine archivierte Rangliste als PDF.</summary>
public sealed record UvwebArchiveEntry(int MsId, int UvId, string Name, string PdfUrl);

/// <summary>Liest die öffentlichen HTML-Seiten von www.uvweb.kegler.ch.</summary>
public sealed partial class UvwebScraper(HttpClient http)
{
    /// <summary>UVweb-Disziplinen, die übernommen werden (DiszID → Disziplin). Sport, Americaine, Sport Spezial, Kombination und Klub nicht.</summary>
    private static readonly Dictionary<int, Discipline> Disciplines = new() { [1] = Discipline.Championship, [2] = Discipline.Women };

    private static readonly HtmlParser Parser = new();
    private static readonly CultureInfo Swiss = CultureInfo.GetCultureInfo("de-CH");

    /// <summary>Übersicht «Aktuelle Ranglisten» aller Unterverbände.</summary>
    public async Task<IReadOnlyList<UvwebCurrentEntry>> GetCurrentAsync(CancellationToken ct)
    {
        var document = await LoadAsync("rangliste/AktuelleRanglisten.aspx", ct);
        var items = new List<UvwebCurrentEntry>();

        foreach (var row in document.QuerySelectorAll("tr"))
        {
            var cells = row.QuerySelectorAll("td");
            var link = row.QuerySelector("a[href*='msID=']");
            if (cells.Length < 4 || link is null)
            {
                continue;
            }

            var query = ParseQuery(link.GetAttribute("href"));
            items.Add(new UvwebCurrentEntry(
                int.Parse(query["msID"], CultureInfo.InvariantCulture),
                int.Parse(query["uvID"], CultureInfo.InvariantCulture),
                int.Parse(query["RanglisteID"], CultureInfo.InvariantCulture),
                Text(cells[1]),
                Text(link)));
        }

        return items;
    }

    /// <summary>Ranglisten-Archiv eines Unterverbands: PDF-Links auf ftp.sfkv.net (bei Korrekturen gilt die zuletzt eingefügte PDF).</summary>
    public async Task<IReadOnlyList<UvwebArchiveEntry>> GetArchiveAsync(int uvId, CancellationToken ct)
    {
        var document = await LoadAsync($"archiv/ranglistenarchiv_uv.aspx?uvID={uvId}", ct);
        var entries = new List<UvwebArchiveEntry>();

        foreach (var row in document.QuerySelectorAll("tr"))
        {
            var cells = row.QuerySelectorAll("td");
            var link = row.QuerySelector("a[href$='.pdf']");
            if (cells.Length >= 4 && link is not null && int.TryParse(Text(cells[0]), out var msId))
            {
                entries.Add(new UvwebArchiveEntry(msId, uvId, Text(link), link.GetAttribute("href") ?? string.Empty));
            }
        }

        return entries.GroupBy(e => e.MsId).Select(g => g.Last()).ToList();
    }

    /// <summary>Stammdaten aus msangaben.aspx – funktioniert auch für archivierte msIDs.</summary>
    public async Task<UvwebChampionship> GetChampionshipAsync(int msId, int uvId, CancellationToken ct)
    {
        var document = await LoadAsync($"rangliste/msangaben.aspx?msID={msId}", ct);
        var details = document.QuerySelectorAll("tr")
            .Select(r => r.QuerySelectorAll("td"))
            .Where(c => c.Length >= 2)
            .GroupBy(c => Text(c[0]))
            .ToDictionary(g => g.Key, g => Text(g.First()[1]), StringComparer.OrdinalIgnoreCase);

        return new UvwebChampionship(
            msId, uvId, details["Unterverband"], details["Bezeichnung"],
            ParseDate(details.GetValueOrDefault("Beginn")) ?? DateOnly.FromDateTime(DateTime.Today),
            ParseDate(details.GetValueOrDefault("Ende")) ?? DateOnly.FromDateTime(DateTime.Today),
            int.TryParse(details.GetValueOrDefault("Anzahl Würfe"), out var throws) ? throws : 100,
            NullIfEmpty(details.GetValueOrDefault("Durchführer")),
            NullIfEmpty(details.GetValueOrDefault("Lokal")),
            NullIfEmpty(details.GetValueOrDefault("Ort")));
    }

    /// <summary>Resultate der aktuellen HTML-Ranglisten für Meisterschaft und MS Damen (alle Kategorien).</summary>
    public async Task<IReadOnlyList<UvwebResult>> GetResultsAsync(int msId, int uvId, int ranglisteId, CancellationToken ct)
    {
        var navigation = await LoadAsync($"rangliste/treeview.aspx?uvID={uvId}&RanglisteID={ranglisteId}&msID={msId}", ct);
        var results = new List<UvwebResult>();

        foreach (var link in navigation.QuerySelectorAll("a[href*='meisterschaft.aspx']"))
        {
            var query = ParseQuery(link.GetAttribute("href"));
            var categoryMatch = CategoryPattern().Match(Text(link));
            if (!categoryMatch.Success
                || !Enum.TryParse<PlayerCategory>(categoryMatch.Groups[1].Value, out var category)
                || !Disciplines.TryGetValue(int.Parse(query["DiszID"], CultureInfo.InvariantCulture), out var discipline))
            {
                continue;
            }

            results.AddRange(await LoadRankingAsync(query["KatID"], query["DiszID"], uvId, ranglisteId, category, discipline, ct));
        }

        return results;
    }

    /// <summary>PDF einer archivierten Rangliste herunterladen.</summary>
    public Task<Stream> GetPdfAsync(string pdfUrl, CancellationToken ct) => http.GetStreamAsync(new Uri(pdfUrl), ct);

    private async Task<List<UvwebResult>> LoadRankingAsync(string katId, string diszId, int uvId, int ranglisteId, PlayerCategory category, Discipline discipline, CancellationToken ct)
    {
        var document = await LoadAsync($"rangliste/meisterschaft.aspx?KatID={katId}&DiszID={diszId}&uvID={uvId}&RanglisteID={ranglisteId}", ct);
        var results = new List<UvwebResult>();

        // Spalten: Rang | Name | Ort | Resultat | Neuner | Auszeichnung | Lizenz-Nr. | Details-Link (klubID)
        foreach (var row in document.QuerySelectorAll("tr"))
        {
            var cells = row.QuerySelectorAll("td");
            var link = row.QuerySelector("a[href*='klubID=']");
            if (cells.Length < 7 || link is null || !int.TryParse(Text(cells[0]), out _))
            {
                continue;
            }

            results.Add(new UvwebResult(
                Text(cells[6]),
                Text(cells[1]),
                NullIfEmpty(Text(cells[2])),
                category,
                discipline,
                ParsePins(Text(cells[3])),
                int.TryParse(Text(cells[4]), out var nines) ? nines : 0,
                Text(cells[5]).Equals("Ja", StringComparison.OrdinalIgnoreCase),
                int.Parse(ParseQuery(link.GetAttribute("href"))["klubID"], CultureInfo.InvariantCulture)));
        }

        return results;
    }

    private async Task<IDocument> LoadAsync(string relativeUrl, CancellationToken ct)
    {
        var html = await http.GetStringAsync(relativeUrl, ct);
        return await Parser.ParseDocumentAsync(html, ct);
    }

    private static Dictionary<string, string> ParseQuery(string? href)
    {
        var query = href?.Split('?').ElementAtOrDefault(1) ?? string.Empty;
        return query.Split('&', StringSplitOptions.RemoveEmptyEntries)
            .Select(p => p.Split('=', 2))
            .ToDictionary(p => p[0], p => p.ElementAtOrDefault(1) ?? string.Empty, StringComparer.OrdinalIgnoreCase);
    }

    private static string Text(INode node) => WhitespacePattern().Replace(node.TextContent, " ").Trim();

    private static string? NullIfEmpty(string? value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim();

    /// <summary>«770.000» → 770 (UVweb zeigt Resultate mit drei Nachkommastellen).</summary>
    private static int ParsePins(string value) =>
        (int)Math.Round(decimal.Parse(value.Replace("'", string.Empty, StringComparison.Ordinal), NumberStyles.Any, CultureInfo.InvariantCulture));

    private static DateOnly? ParseDate(string? value) =>
        DateOnly.TryParseExact(value?.Trim(), "dd.MM.yyyy", Swiss, DateTimeStyles.None, out var date) ? date : null;

    [GeneratedRegex(@"Kat\.\s*([ABCG])")]
    private static partial Regex CategoryPattern();

    [GeneratedRegex(@"\s+")]
    private static partial Regex WhitespacePattern();
}
