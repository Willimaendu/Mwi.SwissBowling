namespace SwissBowling.Server.Infrastructure;

/// <summary>Einheitliche ProblemDetails-Antworten mit deutschen Meldungen.</summary>
public static class Problems
{
    public static IResult NotFound(string what) =>
        Results.Problem(statusCode: StatusCodes.Status404NotFound, title: "Nicht gefunden", detail: $"{what} wurde nicht gefunden.");

    public static IResult Conflict(string detail) =>
        Results.Problem(statusCode: StatusCodes.Status409Conflict, title: "Konflikt", detail: detail);

    public static IResult Invalid(string detail) =>
        Results.Problem(statusCode: StatusCodes.Status400BadRequest, title: "Ungültige Eingabe", detail: detail);

    public static IResult Forbidden(string detail) =>
        Results.Problem(statusCode: StatusCodes.Status403Forbidden, title: "Nicht erlaubt", detail: detail);
}
