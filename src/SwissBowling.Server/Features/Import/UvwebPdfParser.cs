using System.Globalization;
using System.Text.RegularExpressions;
using SwissBowling.Core.Players;
using UglyToad.PdfPig;
using UglyToad.PdfPig.DocumentLayoutAnalysis.TextExtractor;

namespace SwissBowling.Server.Features.Import;

/// <summary>Resultat aus einem Archiv-PDF: ohne Lizenznummer, Klub nur als (Kurz-)Name.</summary>
public sealed record UvwebArchiveResult(string Name, string ClubName, PlayerCategory Category, int Pins, int Nines, bool Award);

/// <summary>
/// Liest die Ranglisten-PDFs von ftp.sfkv.net. Aufbau pro Kategorie:
/// «Kategorie A (66 Teilnehmer)», dann Zeilen «1. Wicki Fridolin, River Boys 795 / 38»,
/// optional getrennt durch «ohne Auszeichnung».
/// </summary>
public static partial class UvwebPdfParser
{
    public static IReadOnlyList<UvwebArchiveResult> Parse(Stream pdf)
    {
        using var document = PdfDocument.Open(pdf);
        return ParseLines(document.GetPages().SelectMany(page => ContentOrderTextExtractor.GetText(page).Split('\n')));
    }

    public static IReadOnlyList<UvwebArchiveResult> ParseLines(IEnumerable<string> rawLines)
    {
        var lines = rawLines.Select(l => l.Trim()).Where(l => l.Length > 0);
        var results = new List<UvwebArchiveResult>();
        PlayerCategory? category = null;
        var award = true;

        foreach (var line in lines)
        {
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

            if (category is null || EntryPattern().Match(line) is not { Success: true } entry)
            {
                continue;
            }

            results.Add(new UvwebArchiveResult(
                entry.Groups["name"].Value.Trim(),
                entry.Groups["club"].Value.Trim(),
                category.Value,
                int.Parse(entry.Groups["pins"].Value, CultureInfo.InvariantCulture),
                int.Parse(entry.Groups["nines"].Value, CultureInfo.InvariantCulture),
                award));
        }

        return results;
    }

    [GeneratedRegex(@"^Kategorie\s+([ABCG])\b")]
    private static partial Regex CategoryPattern();

    [GeneratedRegex(@"^\d+\.\s+(?<name>[^,]+),\s+(?<club>.+?)\s+(?<pins>\d+)\s*/\s*(?<nines>\d+)$")]
    private static partial Regex EntryPattern();
}
