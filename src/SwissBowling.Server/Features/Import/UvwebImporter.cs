using Microsoft.EntityFrameworkCore;
using SwissBowling.Server.Data;
using SwissBowling.Server.Features.Associations;
using SwissBowling.Server.Features.Championships;
using SwissBowling.Server.Features.Clubs;
using SwissBowling.Server.Features.Players;
using SwissBowling.Server.Features.Results;

namespace SwissBowling.Server.Features.Import;

/// <summary>
/// Übernimmt Resultate von UVweb (Disziplinen Meisterschaft und MS Damen). Die Stammdaten stammen von sfkv.ch;
/// Kegler werden über die Lizenznummer (HTML) bzw. den Namen (Archiv-PDF) zugeordnet. Fehlende Kegler/Klubs
/// (z.B. Gäste) werden minimal angelegt.
/// </summary>
public sealed class UvwebImporter(UvwebScraper scraper, AppDbContext db)
{
    public async Task<ImportSummary> ImportAsync(int msId, int uvId, int ranglisteId, CancellationToken ct)
    {
        var source = await scraper.GetChampionshipAsync(msId, uvId, ct);
        var results = await scraper.GetResultsAsync(msId, uvId, ranglisteId, ct);

        var association = await GetAssociationAsync(source, ct);
        var championship = await UpsertChampionshipAsync(source, association, ct);

        var licenses = results.Select(r => r.License).Distinct().ToList();
        var players = await db.Players.Include(p => p.Club).Where(p => p.LicenseNumber != null && licenses.Contains(p.LicenseNumber)).ToDictionaryAsync(p => p.LicenseNumber!, ct);
        var clubIds = results.Select(r => r.ClubId).Where(id => id > 0).Distinct().ToList();
        var clubs = await db.Clubs.Where(c => c.ExternalId != null && clubIds.Contains(c.ExternalId.Value)).ToDictionaryAsync(c => c.ExternalId!.Value, ct);
        var newPlayers = 0;

        foreach (var item in results)
        {
            if (!players.TryGetValue(item.License, out var player))
            {
                // Nicht in den sfkv.ch-Stammdaten (z.B. Gast): minimal anlegen.
                player = db.Players.Add(new Player
                {
                    Name = item.Name,
                    City = item.City,
                    LicenseNumber = item.License,
                    Category = item.Category,
                    Association = association,
                    Club = clubs.GetValueOrDefault(item.ClubId)
                }).Entity;
                players[item.License] = player;
                newPlayers++;
            }

            var result = GetOrAddResult(championship, player, item.Discipline);
            result.Pins = item.Pins;
            result.Nines = item.Nines;
            result.Award = item.Award;
        }

        await db.SaveChangesAsync(ct);
        return new ImportSummary(source.Name, 0, newPlayers, results.Count);
    }

    public async Task<ImportSummary> ImportArchiveAsync(int msId, int uvId, string pdfUrl, CancellationToken ct)
    {
        var source = await scraper.GetChampionshipAsync(msId, uvId, ct);
        await using var pdf = await scraper.GetPdfAsync(pdfUrl, ct);
        var results = UvwebPdfParser.Parse(pdf);

        var association = await GetAssociationAsync(source, ct);
        var championship = await UpsertChampionshipAsync(source, association, ct);

        var players = await db.Players.Include(p => p.Club).ToListAsync(ct);
        var clubs = await db.Clubs.ToListAsync(ct);
        var newClubs = 0;
        var newPlayers = 0;

        foreach (var item in results)
        {
            var player = MatchPlayer(players, item, association.Id);
            if (player is null)
            {
                var club = MatchClub(clubs, item.ClubName, association.Id);
                if (club is null)
                {
                    club = db.Clubs.Add(new Club { Name = item.ClubName, Association = association }).Entity;
                    clubs.Add(club);
                    newClubs++;
                }

                player = db.Players.Add(new Player { Name = item.Name, Club = club, Association = association, Category = item.Category, IsActive = false }).Entity;
                players.Add(player);
                newPlayers++;
            }

            var result = GetOrAddResult(championship, player, item.Discipline);
            result.Pins = item.Pins;
            result.Nines = item.Nines;
            result.Award = item.Award;
            result.Date ??= source.EndDate;
        }

        await db.SaveChangesAsync(ct);
        return new ImportSummary($"{source.Name} (Archiv)", newClubs, newPlayers, results.Count);
    }

    private async Task<Association> GetAssociationAsync(UvwebChampionship source, CancellationToken ct) =>
        await db.Associations.FirstOrDefaultAsync(a => a.ExternalId == source.UvId, ct)
        ?? db.Associations.Add(new Association { Name = source.Association, ExternalId = source.UvId }).Entity;

    private async Task<Championship> UpsertChampionshipAsync(UvwebChampionship source, Association association, CancellationToken ct)
    {
        var championship = await db.Championships.Include(c => c.Results).FirstOrDefaultAsync(c => c.ExternalId == source.MsId, ct)
            ?? db.Championships.Add(new Championship { Name = source.Name, Association = association, ExternalId = source.MsId }).Entity;

        championship.Name = source.Name;
        championship.Association = association;
        championship.Organizer = source.Organizer;
        championship.Venue = source.Venue;
        championship.City = source.City;
        championship.StartDate = source.StartDate;
        championship.EndDate = source.EndDate;
        championship.Throws = source.Throws;
        return championship;
    }

    /// <summary>Archiv: Kegler über Namen; bei Mehrdeutigkeit entscheidet der Klub, dann der Unterverband.</summary>
    private static Player? MatchPlayer(List<Player> players, UvwebArchiveResult item, Guid associationId)
    {
        var candidates = players.Where(p => p.Name.Equals(item.Name, StringComparison.OrdinalIgnoreCase)).ToList();
        return candidates.Count <= 1
            ? candidates.FirstOrDefault()
            : candidates.FirstOrDefault(p => p.Club is { } club && club.Name.Equals(item.ClubName, StringComparison.OrdinalIgnoreCase))
                ?? candidates.FirstOrDefault(p => p.AssociationId == associationId)
                ?? candidates[0];
    }

    /// <summary>Archiv: Klub über den SFKV-Kurznamen, bevorzugt im eigenen Unterverband.</summary>
    private static Club? MatchClub(List<Club> clubs, string name, Guid associationId)
    {
        var candidates = clubs.Where(c => c.Name.Equals(name, StringComparison.OrdinalIgnoreCase)).ToList();
        return candidates.FirstOrDefault(c => c.AssociationId == associationId) ?? candidates.FirstOrDefault();
    }

    private static Result GetOrAddResult(Championship championship, Player player, Core.Results.Discipline discipline)
    {
        var result = championship.Results.FirstOrDefault(r => r.Discipline == discipline && (r.PlayerId == player.Id || ReferenceEquals(r.Player, player)));
        if (result is null)
        {
            result = new Result { Championship = championship, Player = player, Discipline = discipline };
            championship.Results.Add(result);
        }

        return result;
    }
}
