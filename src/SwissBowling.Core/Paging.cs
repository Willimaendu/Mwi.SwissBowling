namespace SwissBowling.Core;

/// <summary>Eine Seite einer serverseitig gefilterten Liste.</summary>
public sealed record PagedResult<T>(IReadOnlyList<T> Items, int Total);
