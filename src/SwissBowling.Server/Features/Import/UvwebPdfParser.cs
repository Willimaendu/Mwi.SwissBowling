using System.Globalization;
using System.Text.RegularExpressions;
using SwissBowling.Core.Players;
using SwissBowling.Core.Results;
using UglyToad.PdfPig;
using UglyToad.PdfPig.DocumentLayoutAnalysis.TextExtractor;

namespace SwissBowling.Server.Features.Import;

/// <summary>Resultat aus einem Archiv-PDF: ohne Lizenznummer, Klub nur als SFKV-Kurzname.</summary>
public sealed record UvwebArchiveResult(string Name, string ClubName, PlayerCategory Category, Discipline Discipline, int Pins, int Nines, bool Award);

/// <summary>
/// Liest die Ranglisten-PDFs von ftp.sfkv.net. Jede Seite beginnt mit einem Disziplin-Code («MS», «MSD», «AME», «SP», …),
/// danach folgen Kategorie-Blöcke «Kategorie A (66 Teilnehmer)» mit Zeilen «1. Wicki Fridolin, River Boys 795 / 38»,
/// optional getrennt durch «ohne Auszeichnung». Nur Meisterschaft (MS) und MS Damen (MSD) werden übernommen.
/// </summary>
public static partial class UvwebPdfParser
{
    private static readonly Dictionary<string, Discipline> Sections = new(StringComparer.OrdinalIgnoreCase)
    {
        ["MS"] = Discipline.Championship,
        ["MSD"] = Discipline.Women
    };

    public static IReadOnlyList<UvwebArchiveResult> Parse(Stream pdf)
    {
        using var document = PdfDocument.Open(pdf);
        return ParseLines(document.GetPages().SelectMany(page => ContentOrderTextExtractor.GetText(page).Split('\n')));
    }

    public static IReadOnlyList<UvwebArchiveResult> ParseLines(IEnumerable<string> rawLines)
    {
        var results = new List<UvwebArchiveResult>();
        Discipline? discipline = null;
        PlayerCategory? category = null;
        var award = true;

        foreach (var line in rawLines.Select(l => l.Trim()).Where(l => l.Length > 0))
        {
            if (SectionPattern().IsMatch(line))
            {
                discipline = Sections.GetValueOrDefault(line);
                category = null;
                continue;
            }

            if (CategoryPattern().Match(line) is { Success: true } categoryMatch)
            {
                category = Enum.Parse<PlayerCategory>(categoryMatch.Groups[1].Value);
                award = true;
                continue;
            }

            if (line.StartsWith("ohne Auszeichnung", StringComparison.OrdinalIgnoreCase))
            {
                award = false;
                continue;
            }

            if (discipline is null || category is null || EntryPattern().Match(line) is not { Success: true } entry)
            {
                continue;
            }

            results.Add(new UvwebArchiveResult(
                entry.Groups["name"].Value.Trim(),
                entry.Groups["club"].Value.Trim(),
                category.Value,
                discipline.Value,
                int.Parse(entry.Groups["pins"].Value, CultureInfo.InvariantCulture),
                int.Parse(entry.Groups["nines"].Value, CultureInfo.InvariantCulture),
                award));
        }

        return results;
    }

    /// <summary>Disziplin-Code allein auf einer Zeile, z.B. «MS», «MSD», «AME», «SP», «KOMB».</summary>
    [GeneratedRegex(@"^[A-Z]{2,6}$")]
    private static partial Regex SectionPattern();

    [GeneratedRegex(@"^Kategorie\s+([ABCG])\b")]
    private static partial Regex CategoryPattern();

    [GeneratedRegex(@"^\d+\.\s+(?<name>[^,]+),\s+(?<club>.+?)\s+(?<pins>\d+)\s*/\s*(?<nines>\d+)$")]
    private static partial Regex EntryPattern();
}
