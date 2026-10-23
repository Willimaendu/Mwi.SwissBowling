using Mwi.SwissBowling.Contracts;

namespace Mwi.SwissBowling.Api.Features.Results;

public static class ScoreRules
{
    public static string? Validate(ResultInput input, int? maximumThrows)
    {
        if (input.Total < 0 || input.Nines < 0 || input.Eights < 0 || input.ExpectedVersion < 0)
            return "Holz, Trefferzahlen und Version dürfen nicht negativ sein.";
        var countedThrows = (long)input.Nines + input.Eights;
        var countedScore = 9L * input.Nines + 8L * input.Eights;
        if (countedScore > input.Total) return "Die Trefferzahlen ergeben mehr Holz als das Gesamtresultat.";
        if (maximumThrows is { } limit && (countedThrows > limit || input.Total > countedScore + 7L * (limit - countedThrows)))
            return "Das Resultat widerspricht der festgelegten Wurfzahl oder den Trefferzahlen.";
        return null;
    }
}
