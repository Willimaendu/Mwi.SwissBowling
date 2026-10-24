using System.Globalization;
using System.Text.RegularExpressions;
using AngleSharp.Dom;
using AngleSharp.Html.Parser;
using SwissBowling.Core.Players;

namespace SwissBowling.Server.Features.Import;

public sealed record SfkvAssociation(
    string Name,
    string ShortCode,
    int? UvId,
    string? Website,
    IReadOnlyList<SfkvBoardMember> Board,
    IReadOnlyList<SfkvMember> Members,
    IReadOnlyList<SfkvClub> Clubs,
    IReadOnlyList<SfkvVenue> Venues);

public sealed record SfkvBoardMember(string Function, string Name, string? City, DateOnly? Since, string? Email, string? Phone);

public sealed record SfkvMember(string License, string Name, string? City, DateOnly? BirthDate, string? ClubName, PlayerCategory Category);

public sealed record SfkvClub(string Name, string? HomeVenue, PlayerCategory? Category, int? ClubId, string? Email, string? Website);

public sealed record SfkvVenue(string Name, string? Zip, string? City, string? Phone, int? Lanes, string? RestDay, string? Website);

/// <summary>
/// Liest die Verbandsübersichten auf www.sfkv.ch/unterverbaende/uvXX – der Master für Unterverbände, Vorstand,
/// Mitglieder (ID = Lizenz), Klubs (Klub-ID) und Kegelbahnen.
/// </summary>
public sealed partial class SfkvScraper(HttpClient http)
{
    private static readonly HtmlParser Parser = new();
    private static readonly CultureInfo Swiss = CultureInfo.GetCultureInfo("de-CH");

    /// <summary>Kürzel aller Unterverbände (uvbo, uvbe, …) aus der Übersichtsseite.</summary>
    public async Task<IReadOnlyList<string>> GetAssociationCodesAsync(CancellationToken ct)
    {
        var document = await LoadAsync("unterverbaende", ct);
        return document.QuerySelectorAll("a[href*='/unterverbaende/uv']")
            .Select(a => AssociationCodePattern().Match(a.GetAttribute("href") ?? string.Empty))
            .Where(m => m.Success)
            .Select(m => m.Groups[1].Value.ToLowerInvariant())
            .Distinct()
            .ToList();
    }

    public async Task<SfkvAssociation> GetAssociationAsync(string shortCode, CancellationToken ct)
    {
        var document = await LoadAsync($"unterverbaende/{shortCode}", ct);
        var tables = document.QuerySelectorAll("table").ToList();

        var info = ParseInfo(tables);
        var name = document.QuerySelector("h1")?.TextContent.Trim() ?? shortCode;

        return new SfkvAssociation(
            name,
            info.GetValueOrDefault("Kürzel") ?? shortCode,
            int.TryParse(info.GetValueOrDefault("UV-Nr."), out var uvId) ? uvId : null,
            NullIfEmpty(info.GetValueOrDefault("Homepage")),
            Rows(tables, "Funktion").Select(ParseBoardMember).ToList(),
            Rows(tables, "Geburtsdatum").Select(ParseMember).ToList(),
            Rows(tables, "Klub-ID").Select(ParseClub).ToList(),
            Rows(tables, "Bahnen").Select(ParseVenue).ToList());
    }

    /// <summary>Kopftabelle: Zellen wie «Kürzel: uvem», «UV-Nr.: 15», «Homepage: http://…».</summary>
    private static Dictionary<string, string> ParseInfo(IEnumerable<IElement> tables)
    {
        var info = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        var table = tables.FirstOrDefault(t => t.TextContent.Contains("UV-Nr.", StringComparison.Ordinal));
        foreach (var cell in table?.QuerySelectorAll("td, th") ?? Enumerable.Empty<IElement>())
        {
            var parts = Text(cell).Split(':', 2);
            if (parts.Length == 2)
            {
                info[parts[0].Trim()] = parts[1].Trim();
            }
        }

        return info;
    }

    /// <summary>Datenzeilen der Tabelle, deren Kopfzeile <paramref name="headerKeyword"/> enthält.</summary>
    private static IEnumerable<List<IElement>> Rows(IEnumerable<IElement> tables, string headerKeyword)
    {
        var table = tables.FirstOrDefault(t => t.QuerySelector("tr") is { } header && header.TextContent.Contains(headerKeyword, StringComparison.Ordinal));
        return table is null
            ? []
            : table.QuerySelectorAll("tr").Skip(1)
                .Select(r => r.Children.Where(c => c.LocalName is "td" or "th").ToList())
                .Where(c => c.Count >= 6);
    }

    private static SfkvBoardMember ParseBoardMember(List<IElement> c) =>
        new(Text(c[0]), Text(c[1]), CityFromAddress(Lines(c[2]).LastOrDefault()), ParseDate(Text(c[3])), NullIfEmpty(Text(c[4])), NullIfEmpty(Lines(c[5]).FirstOrDefault()));

    private static SfkvMember ParseMember(List<IElement> c) =>
        new(Text(c[0]), Text(c[1]), CityFromAddress(Text(c[2]).Split(',').Last()), ParseDate(Text(c[3])), NullIfEmpty(Text(c[4])), ParseCategory(Text(c[5])) ?? PlayerCategory.C);

    private static SfkvClub ParseClub(List<IElement> c) =>
        new(Text(c[0]), NullIfEmpty(Text(c[1])), ParseCategory(Text(c[2])), int.TryParse(Text(c[3]), out var id) ? id : null, NullIfEmpty(Text(c[4])), NullIfEmpty(c[5].QuerySelector("a")?.GetAttribute("href")));

    private static SfkvVenue ParseVenue(List<IElement> c)
    {
        var (zip, city) = SplitZipCity(Text(c[1]));
        return new SfkvVenue(Text(c[0]), zip, city, NullIfEmpty(Text(c[2])), int.TryParse(Text(c[3]), out var lanes) ? lanes : null, NullIfEmpty(Text(c[4])), NullIfEmpty(c[5].QuerySelector("a")?.GetAttribute("href")));
    }

    private async Task<IDocument> LoadAsync(string relativeUrl, CancellationToken ct)
    {
        var html = await http.GetStringAsync(relativeUrl, ct);
        return await Parser.ParseDocumentAsync(html, ct);
    }

    private static string Text(INode node) => WhitespacePattern().Replace(node.TextContent, " ").Trim();

    /// <summary>Zellen mit <br> in einzelne Zeilen zerlegen.</summary>
    private static List<string> Lines(IElement cell)
    {
        var text = Parser.ParseDocument($"<body>{BrPattern().Replace(cell.InnerHtml, "\n")}</body>").Body!.TextContent;
        return text.Split('\n').Select(l => WhitespacePattern().Replace(l, " ").Trim()).Where(l => l.Length > 0).ToList();
    }

    private static string? NullIfEmpty(string? value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim();

    /// <summary>«3110 Münsingen» → Münsingen</summary>
    private static string? CityFromAddress(string? value) => NullIfEmpty(ZipPattern().Replace(value ?? string.Empty, string.Empty));

    private static (string? Zip, string? City) SplitZipCity(string value)
    {
        var match = ZipCityPattern().Match(value);
        return match.Success ? (match.Groups[1].Value, match.Groups[2].Value.Trim()) : (null, NullIfEmpty(value));
    }

    private static PlayerCategory? ParseCategory(string value) => Enum.TryParse<PlayerCategory>(value.Trim(), out var category) ? category : null;

    private static DateOnly? ParseDate(string? value) =>
        DateOnly.TryParseExact(value?.Trim(), "dd.MM.yyyy", Swiss, DateTimeStyles.None, out var date) ? date : null;

    [GeneratedRegex(@"/unterverbaende/(uv[a-z]{2})\b", RegexOptions.IgnoreCase)]
    private static partial Regex AssociationCodePattern();

    [GeneratedRegex(@"^\s*(\d{4})\s+(.+)$")]
    private static partial Regex ZipCityPattern();

    [GeneratedRegex(@"^\s*\d{4}\s+")]
    private static partial Regex ZipPattern();

    [GeneratedRegex(@"\s+")]
    private static partial Regex WhitespacePattern();

    [GeneratedRegex(@"<br\s*/?>", RegexOptions.IgnoreCase)]
    private static partial Regex BrPattern();
}
