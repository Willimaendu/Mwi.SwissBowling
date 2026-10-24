using System.Globalization;
using System.Text.RegularExpressions;
using AngleSharp.Dom;
using AngleSharp.Html.Parser;
using SwissBowling.Core.Import;
using SwissBowling.Core.Players;

namespace SwissBowling.Server.Features.Import;

/// <summary>Rohdaten einer Meisterschaft von UVweb (noch ohne Bezug zu unserer Datenbank).</summary>
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
    string? City,
    IReadOnlyList<UvwebResult> Results,
    IReadOnlyList<UvwebClub> Clubs);

public sealed record UvwebResult(string License, string Name, string? City, PlayerCategory Category, int Pins, int Nines, bool Award, int ClubId);

public sealed record UvwebClub(int ClubId, string Name, string? City, IReadOnlyList<UvwebMember> Members);

public sealed record UvwebMember(string License, string Name, string? City, PlayerCategory Category, DateOnly? MatchDate);

/// <summary>Liest die öffentlichen HTML-Seiten von www.uvweb.kegler.ch.</summary>
public sealed partial class UvwebScraper(HttpClient http)
{
    private static readonly HtmlParser Parser = new();
    private static readonly CultureInfo Swiss = CultureInfo.GetCultureInfo("de-CH");

    /// <summary>Startseite: Tabelle «Aktuelle Ranglisten» aller Unterverbände.</summary>
    public async Task<IReadOnlyList<UvwebChampionshipDto>> GetCurrentAsync(CancellationToken ct)
    {
        var document = await LoadAsync("rangliste/AktuelleRanglisten.aspx", ct);
        var items = new List<UvwebChampionshipDto>();

        foreach (var row in document.QuerySelectorAll("tr"))
        {
            var cells = row.QuerySelectorAll("td");
            var link = row.QuerySelector("a[href*='msID=']");
            if (cells.Length < 4 || link is null)
            {
                continue;
            }

            var query = ParseQuery(link.GetAttribute("href"));
            items.Add(new UvwebChampionshipDto(
                int.Parse(query["msID"], CultureInfo.InvariantCulture),
                int.Parse(query["uvID"], CultureInfo.InvariantCulture),
                int.Parse(query["RanglisteID"], CultureInfo.InvariantCulture),
                Text(cells[1]),
                Text(link),
                Text(cells[3]),
                IsImported: false));
        }

        return items;
    }

    /// <summary>Startseite: Navigation «Verbandsübersichten» mit allen Unterverbänden.</summary>
    public async Task<IReadOnlyList<UvwebAssociationDto>> GetAssociationsAsync(CancellationToken ct)
    {
        var document = await LoadAsync("default.aspx", ct);
        return document.QuerySelectorAll("a[href*='uv/default.aspx?uvID=']")
            .Select(a => new UvwebAssociationDto(int.Parse(ParseQuery(a.GetAttribute("href"))["uvID"], CultureInfo.InvariantCulture), Text(a)))
            .Where(a => a.Name.Length > 0)
            .DistinctBy(a => a.UvId)
            .ToList();
    }

    /// <summary>Ranglisten-Archiv eines Unterverbands: PDF-Links auf ftp.sfkv.net.</summary>
    public async Task<IReadOnlyList<UvwebArchiveEntryDto>> GetArchiveAsync(int uvId, CancellationToken ct)
    {
        var document = await LoadAsync($"archiv/ranglistenarchiv_uv.aspx?uvID={uvId}", ct);
        var entries = new List<UvwebArchiveEntryDto>();

        foreach (var row in document.QuerySelectorAll("tr"))
        {
            var cells = row.QuerySelectorAll("td");
            var link = row.QuerySelector("a[href$='.pdf']");
            if (cells.Length < 4 || link is null || !int.TryParse(Text(cells[0]), out var msId))
            {
                continue;
            }

            entries.Add(new UvwebArchiveEntryDto(msId, uvId, Text(link), link.GetAttribute("href") ?? string.Empty, Text(cells[3]), IsImported: false));
        }

        // Gleiche MS-Nr. kann mehrfach vorkommen (Korrekturen) – die zuletzt eingefügte PDF gilt.
        return entries.GroupBy(e => e.MsId).Select(g => g.Last()).ToList();
    }

    /// <summary>Archiv: Stammdaten aus msangaben.aspx (funktioniert auch für alte msIDs), Resultate aus dem PDF.</summary>
    public async Task<(UvwebChampionship Championship, IReadOnlyList<UvwebArchiveResult> Results)> GetArchiveChampionshipAsync(int msId, int uvId, string pdfUrl, CancellationToken ct)
    {
        var details = await LoadDetailsAsync(msId, ct);
        await using var pdf = await http.GetStreamAsync(new Uri(pdfUrl), ct);
        var results = UvwebPdfParser.Parse(pdf);
        return (ToChampionship(msId, uvId, details, [], []), results);
    }

    public async Task<UvwebChampionship> GetChampionshipAsync(int msId, int uvId, int ranglisteId, CancellationToken ct)
    {
        var details = await LoadDetailsAsync(msId, ct);
        var categories = await LoadCategoriesAsync(msId, uvId, ranglisteId, ct);

        var results = new List<UvwebResult>();
        foreach (var (katId, category) in categories)
        {
            results.AddRange(await LoadRankingAsync(katId, category, uvId, ranglisteId, ct));
        }

        var clubs = new List<UvwebClub>();
        foreach (var clubId in results.Select(r => r.ClubId).Where(id => id > 0).Distinct())
        {
            clubs.Add(await LoadClubAsync(clubId, msId, ct));
        }

        return ToChampionship(msId, uvId, details, results, clubs);
    }

    private static UvwebChampionship ToChampionship(int msId, int uvId, Dictionary<string, string> details, IReadOnlyList<UvwebResult> results, IReadOnlyList<UvwebClub> clubs) =>
        new(
            msId, uvId, details["Unterverband"], details["Bezeichnung"],
            ParseDate(details.GetValueOrDefault("Beginn")) ?? DateOnly.FromDateTime(DateTime.Today),
            ParseDate(details.GetValueOrDefault("Ende")) ?? DateOnly.FromDateTime(DateTime.Today),
            int.TryParse(details.GetValueOrDefault("Anzahl Würfe"), out var throws) ? throws : 100,
            NullIfEmpty(details.GetValueOrDefault("Durchführer") ?? string.Empty),
            NullIfEmpty(details.GetValueOrDefault("Lokal") ?? string.Empty),
            NullIfEmpty(details.GetValueOrDefault("Ort") ?? string.Empty),
            results, clubs);

    private async Task<Dictionary<string, string>> LoadDetailsAsync(int msId, CancellationToken ct)
    {
        var document = await LoadAsync($"rangliste/msangaben.aspx?msID={msId}", ct);
        return document.QuerySelectorAll("tr")
            .Select(r => r.QuerySelectorAll("td"))
            .Where(c => c.Length >= 2)
            .GroupBy(c => Text(c[0]))
            .ToDictionary(g => g.Key, g => Text(g.First()[1]), StringComparer.OrdinalIgnoreCase);
    }

    /// <summary>Navigation (treeview.aspx) enthält die Links «Kat. A» … mit der jeweiligen KatID.</summary>
    private async Task<List<(int KatId, PlayerCategory Category)>> LoadCategoriesAsync(int msId, int uvId, int ranglisteId, CancellationToken ct)
    {
        var document = await LoadAsync($"rangliste/treeview.aspx?uvID={uvId}&RanglisteID={ranglisteId}&msID={msId}", ct);
        var categories = new List<(int, PlayerCategory)>();

        foreach (var link in document.QuerySelectorAll("a[href*='meisterschaft.aspx']"))
        {
            var match = CategoryPattern().Match(Text(link));
            if (match.Success && Enum.TryParse<PlayerCategory>(match.Groups[1].Value, out var category))
            {
                categories.Add((int.Parse(ParseQuery(link.GetAttribute("href"))["KatID"], CultureInfo.InvariantCulture), category));
            }
        }

        return categories;
    }

    private async Task<List<UvwebResult>> LoadRankingAsync(int katId, PlayerCategory category, int uvId, int ranglisteId, CancellationToken ct)
    {
        var document = await LoadAsync($"rangliste/meisterschaft.aspx?KatID={katId}&DiszID=1&uvID={uvId}&RanglisteID={ranglisteId}", ct);
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
                ParsePins(Text(cells[3])),
                int.TryParse(Text(cells[4]), out var nines) ? nines : 0,
                Text(cells[5]).Equals("Ja", StringComparison.OrdinalIgnoreCase),
                int.Parse(ParseQuery(link.GetAttribute("href"))["klubID"], CultureInfo.InvariantCulture)));
        }

        return results;
    }

    private async Task<UvwebClub> LoadClubAsync(int clubId, int msId, CancellationToken ct)
    {
        var document = await LoadAsync($"rangliste/klubresultate.aspx?klubID={clubId}&msID={msId}", ct);

        // Titel: «Klubresultate KK Lüderenblick, Zollbrück»
        var titleMatch = ClubTitlePattern().Match(document.Body is { } body ? Text(body) : string.Empty);
        var nameAndCity = titleMatch.Success ? titleMatch.Groups[1].Value.Trim() : string.Empty;
        var comma = nameAndCity.LastIndexOf(',');
        var clubName = comma > 0 ? nameAndCity[..comma].Trim() : nameAndCity;
        var clubCity = comma > 0 ? nameAndCity[(comma + 1)..].Trim() : null;

        var members = new List<UvwebMember>();
        var tables = document.QuerySelectorAll("table");

        // Tabelle 1: Liz.-Nr. | Rang | Name, Ort | Kat. | Einzel | Resultat | Ausz. | Matchdatum
        // Tabelle 2 (Mitglieder ohne Resultat): Liz.-Nr. | Name | Ort | Kategorie
        foreach (var table in tables)
        {
            foreach (var row in table.QuerySelectorAll("tr"))
            {
                var cells = row.QuerySelectorAll("td");
                if (cells.Length == 0 || !int.TryParse(Text(cells[0]), out _))
                {
                    continue;
                }

                if (cells.Length >= 8)
                {
                    var (name, city) = SplitNameCity(Text(cells[2]));
                    members.Add(new UvwebMember(Text(cells[0]), name, city, ParseCategory(Text(cells[3])), ParseDate(Text(cells[7]))));
                }
                else if (cells.Length == 4)
                {
                    members.Add(new UvwebMember(Text(cells[0]), Text(cells[1]), NullIfEmpty(Text(cells[2])), ParseCategory(Text(cells[3])), null));
                }
            }
        }

        return new UvwebClub(clubId, string.IsNullOrEmpty(clubName) ? $"Klub {clubId}" : clubName, clubCity, members);
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

    private static string? NullIfEmpty(string value) => string.IsNullOrWhiteSpace(value) ? null : value;

    private static (string Name, string? City) SplitNameCity(string value)
    {
        var comma = value.LastIndexOf(',');
        return comma > 0 ? (value[..comma].Trim(), value[(comma + 1)..].Trim()) : (value, null);
    }

    /// <summary>«770.000» → 770 (UVweb zeigt Resultate mit drei Nachkommastellen).</summary>
    private static int ParsePins(string value) =>
        (int)Math.Round(decimal.Parse(value.Replace("'", string.Empty, StringComparison.Ordinal), NumberStyles.Any, CultureInfo.InvariantCulture));

    private static PlayerCategory ParseCategory(string value) =>
        Enum.TryParse<PlayerCategory>(value.Trim(), out var category) ? category : PlayerCategory.G;

    private static DateOnly? ParseDate(string? value) =>
        DateOnly.TryParseExact(value?.Trim(), "dd.MM.yyyy", Swiss, DateTimeStyles.None, out var date) ? date : null;

    [GeneratedRegex(@"Kat\.\s*([ABCG])")]
    private static partial Regex CategoryPattern();

    [GeneratedRegex(@"\s+")]
    private static partial Regex WhitespacePattern();

    [GeneratedRegex(@"Klubresultate\s+(.*?)\s*Liz\.-Nr\.")]
    private static partial Regex ClubTitlePattern();
}
