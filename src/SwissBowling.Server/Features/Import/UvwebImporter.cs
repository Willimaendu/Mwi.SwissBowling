using Microsoft.EntityFrameworkCore;
using SwissBowling.Core.Import;
using SwissBowling.Server.Data;
using SwissBowling.Server.Features.Associations;
using SwissBowling.Server.Features.Championships;
using SwissBowling.Server.Features.Clubs;
using SwissBowling.Server.Features.Players;
using SwissBowling.Server.Features.Results;

namespace SwissBowling.Server.Features.Import;

/// <summary>
/// Übernimmt UVweb-Meisterschaften idempotent. Aktuelle Ranglisten (HTML) liefern Lizenznummern und klubIDs als Schlüssel;
/// Archiv-PDFs enthalten nur Namen und Klub-Kurznamen und werden per Namensabgleich zugeordnet.
/// </summary>
public sealed class UvwebImporter(UvwebScraper scraper, AppDbContext db)
{
    private const string UnknownClubName = "Ohne Klubzugehörigkeit";

    public async Task<ImportSummaryDto> ImportAsync(int msId, int uvId, int ranglisteId, CancellationToken ct)
    {
        var source = await scraper.GetChampionshipAsync(msId, uvId, ranglisteId, ct);
        var association = await UpsertAssociationAsync(source, ct);
        var clubs = await UpsertClubsAsync(source, association, ct);
        var players = await UpsertPlayersAsync(source, clubs, ct);
        var championship = await UpsertChampionshipAsync(source, association, ct);

        var matchDates = source.Clubs.SelectMany(c => c.Members).Where(m => m.MatchDate is not null)
            .GroupBy(m => m.License).ToDictionary(g => g.Key, g => g.First().MatchDate);

        foreach (var item in source.Results)
        {
            var result = GetOrAddResult(championship, players[item.License]);
            result.Pins = item.Pins;
            result.Nines = item.Nines;
            result.Award = item.Award;
            result.Date = matchDates.GetValueOrDefault(item.License);
        }

        await db.SaveChangesAsync(ct);
        return new ImportSummaryDto(source.Name, clubs.Count, players.Count, source.Results.Count);
    }

    public async Task<ImportSummaryDto> ImportArchiveAsync(int msId, int uvId, string pdfUrl, CancellationToken ct)
    {
        var (source, results) = await scraper.GetArchiveChampionshipAsync(msId, uvId, pdfUrl, ct);
        var association = await UpsertAssociationAsync(source, ct);
        var championship = await UpsertChampionshipAsync(source, association, ct);

        var players = await db.Players.Include(p => p.Club).ToListAsync(ct);
        var clubs = await db.Clubs.Where(c => c.AssociationId == association.Id).ToListAsync(ct);
        var newClubs = 0;
        var newPlayers = 0;

        foreach (var item in results)
        {
            var player = MatchPlayer(players, item);
            if (player is null)
            {
                var club = MatchClub(clubs, item.ClubName);
                if (club is null)
                {
                    club = db.Clubs.Add(new Club { Name = item.ClubName, Association = association }).Entity;
                    clubs.Add(club);
                    newClubs++;
                }

                player = db.Players.Add(new Player { Name = item.Name, Club = club, Category = item.Category }).Entity;
                players.Add(player);
                newPlayers++;
            }

            var result = GetOrAddResult(championship, player);
            result.Pins = item.Pins;
            result.Nines = item.Nines;
            result.Award = item.Award;
            result.Date ??= source.EndDate;
        }

        await db.SaveChangesAsync(ct);
        return new ImportSummaryDto(source.Name, newClubs, newPlayers, results.Count);
    }

    private async Task<Association> UpsertAssociationAsync(UvwebChampionship source, CancellationToken ct)
    {
        var association = await db.Associations.FirstOrDefaultAsync(a => a.ExternalId == source.UvId, ct)
            ?? db.Associations.Add(new Association { Name = source.Association, ExternalId = source.UvId }).Entity;
        association.Name = source.Association;
        return association;
    }

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

    private async Task<Dictionary<int, Club>> UpsertClubsAsync(UvwebChampionship source, Association association, CancellationToken ct)
    {
        var externalIds = source.Clubs.Select(c => c.ClubId).ToList();
        var existing = await db.Clubs.Where(c => c.ExternalId != null && externalIds.Contains(c.ExternalId.Value)).ToDictionaryAsync(c => c.ExternalId!.Value, ct);

        foreach (var item in source.Clubs)
        {
            if (!existing.TryGetValue(item.ClubId, out var club))
            {
                // Vielleicht wurde der Klub bereits aus dem Archiv (ohne klubID) angelegt.
                club = await db.Clubs.FirstOrDefaultAsync(c => c.ExternalId == null && c.Name == item.Name, ct)
                    ?? db.Clubs.Add(new Club { Name = item.Name, Association = association }).Entity;
                club.ExternalId = item.ClubId;
                existing[item.ClubId] = club;
            }

            club.Name = item.Name;
            club.City = item.City;
        }

        // Kegler ohne Klub (klubID 0) landen in einem Sammelklub des Unterverbands.
        if (source.Results.Any(r => r.ClubId == 0))
        {
            existing[0] = await db.Clubs.FirstOrDefaultAsync(c => c.AssociationId == association.Id && c.Name == UnknownClubName, ct)
                ?? db.Clubs.Add(new Club { Name = UnknownClubName, Association = association }).Entity;
        }

        return existing;
    }

    private async Task<Dictionary<string, Player>> UpsertPlayersAsync(UvwebChampionship source, Dictionary<int, Club> clubs, CancellationToken ct)
    {
        var members = source.Clubs.SelectMany(c => c.Members.Select(m => (Club: clubs[c.ClubId], Member: m)))
            .GroupBy(x => x.Member.License).ToDictionary(g => g.Key, g => g.First());

        foreach (var item in source.Results.Where(r => !members.ContainsKey(r.License)))
        {
            members[item.License] = (clubs[item.ClubId], new UvwebMember(item.License, item.Name, item.City, item.Category, null));
        }

        var licenses = members.Keys.ToList();
        var existing = await db.Players.Where(p => p.LicenseNumber != null && licenses.Contains(p.LicenseNumber)).ToDictionaryAsync(p => p.LicenseNumber!, ct);
        var unlicensed = await db.Players.Include(p => p.Club).Where(p => p.LicenseNumber == null).ToListAsync(ct);

        foreach (var (license, (club, member)) in members)
        {
            if (!existing.TryGetValue(license, out var player))
            {
                // Vielleicht wurde der Kegler bereits aus dem Archiv (ohne Lizenz) angelegt.
                player = unlicensed.FirstOrDefault(p => p.Name.Equals(member.Name, StringComparison.OrdinalIgnoreCase) && SameClub(p.Club, club))
                    ?? db.Players.Add(new Player { Name = member.Name, Club = club }).Entity;
                player.LicenseNumber = license;
                existing[license] = player;
            }

            player.Name = member.Name;
            player.City = member.City ?? player.City;
            player.Category = member.Category;
            player.Club = club;
            player.IsActive = true;
        }

        return existing;
    }

    /// <summary>Archiv: Kegler über Namen finden; bei Namensgleichheit entscheidet der Klub.</summary>
    private static Player? MatchPlayer(List<Player> players, UvwebArchiveResult item)
    {
        var candidates = players.Where(p => p.Name.Equals(item.Name, StringComparison.OrdinalIgnoreCase)).ToList();
        return candidates.Count <= 1
            ? candidates.FirstOrDefault()
            : candidates.FirstOrDefault(p => ClubMatches(p.Club.Name, item.ClubName)) ?? candidates[0];
    }

    private static Club? MatchClub(List<Club> clubs, string shortName) =>
        clubs.FirstOrDefault(c => c.Name.Equals(shortName, StringComparison.OrdinalIgnoreCase))
        ?? clubs.FirstOrDefault(c => ClubMatches(c.Name, shortName));

    /// <summary>«KK Lüderenblick» passt zu «Lüderenblick».</summary>
    private static bool ClubMatches(string fullName, string shortName) =>
        fullName.Contains(shortName, StringComparison.OrdinalIgnoreCase) || shortName.Contains(fullName, StringComparison.OrdinalIgnoreCase);

    private static bool SameClub(Club a, Club b) => ReferenceEquals(a, b) || a.Id == b.Id || ClubMatches(a.Name, b.Name);

    private static Result GetOrAddResult(Championship championship, Player player)
    {
        var result = championship.Results.FirstOrDefault(r => r.PlayerId == player.Id || ReferenceEquals(r.Player, player));
        if (result is null)
        {
            result = new Result { Championship = championship, Player = player };
            championship.Results.Add(result);
        }

        return result;
    }
}
