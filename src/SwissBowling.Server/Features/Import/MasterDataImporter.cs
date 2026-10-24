using Microsoft.EntityFrameworkCore;
using SwissBowling.Server.Data;
using SwissBowling.Server.Features.Associations;
using SwissBowling.Server.Features.Clubs;
using SwissBowling.Server.Features.Players;
using SwissBowling.Server.Features.Venues;

namespace SwissBowling.Server.Features.Import;

/// <summary>
/// Übernimmt die Stammdaten von www.sfkv.ch (Master): Unterverband über UV-Nr., Klubs über Klub-ID, Kegler über
/// Mitglieder-ID (Lizenz), Kegelbahnen über Name, Vorstand wird ersetzt. Kegler, die nicht mehr gelistet sind, werden deaktiviert.
/// </summary>
public sealed class MasterDataImporter(SfkvScraper scraper, AppDbContext db)
{
    public Task<IReadOnlyList<string>> GetAssociationCodesAsync(CancellationToken ct) => scraper.GetAssociationCodesAsync(ct);

    public async Task<ImportSummary> ImportAssociationAsync(string shortCode, CancellationToken ct)
    {
        var source = await scraper.GetAssociationAsync(shortCode, ct);

        var association = await db.Associations
                .Include(a => a.Board).Include(a => a.Venues).Include(a => a.Clubs).Include(a => a.Players)
                .FirstOrDefaultAsync(a => (source.UvId != null && a.ExternalId == source.UvId) || a.ShortCode == source.ShortCode, ct)
            ?? db.Associations.Add(new Association { Name = source.Name }).Entity;

        association.Name = source.Name;
        association.ShortCode = source.ShortCode;
        association.ExternalId = source.UvId ?? association.ExternalId;
        association.Website = source.Website;

        association.Board.Clear();
        association.Board.AddRange(source.Board.Select(b => new BoardMember
        {
            Association = association, Function = b.Function, Name = b.Name, City = b.City, Since = b.Since, Email = b.Email, Phone = b.Phone
        }));

        foreach (var item in source.Venues)
        {
            var venue = association.Venues.FirstOrDefault(v => v.Name.Equals(item.Name, StringComparison.OrdinalIgnoreCase) && v.City == item.City)
                ?? AddTo(association.Venues, new Venue { Association = association, Name = item.Name });
            (venue.Zip, venue.City, venue.Phone, venue.Lanes, venue.RestDay, venue.Website) = (item.Zip, item.City, item.Phone, item.Lanes, item.RestDay, item.Website);
        }

        var clubs = await UpsertClubsAsync(source, association, ct);
        var players = await UpsertPlayersAsync(source, association, ct);

        await db.SaveChangesAsync(ct);
        return new ImportSummary($"Stammdaten UV {source.Name}", clubs, players, 0);
    }

    private async Task<int> UpsertClubsAsync(SfkvAssociation source, Association association, CancellationToken ct)
    {
        var count = 0;
        foreach (var item in source.Clubs)
        {
            var club = association.Clubs.FirstOrDefault(c => item.ClubId != null && c.ExternalId == item.ClubId)
                ?? association.Clubs.FirstOrDefault(c => c.Name.Equals(item.Name, StringComparison.OrdinalIgnoreCase))
                ?? (item.ClubId is { } id ? await db.Clubs.FirstOrDefaultAsync(c => c.ExternalId == id, ct) : null)
                ?? AddTo(association.Clubs, new Club { Association = association, Name = item.Name });

            club.Name = item.Name;
            club.Association = association;
            club.ExternalId = item.ClubId ?? club.ExternalId;
            club.Category = item.Category;
            club.HomeVenue = item.HomeVenue;
            club.Email = item.Email;
            club.Website = item.Website;
            count++;
        }

        return count;
    }

    private async Task<int> UpsertPlayersAsync(SfkvAssociation source, Association association, CancellationToken ct)
    {
        var licenses = source.Members.Select(m => m.License).ToList();
        var existing = await db.Players.Where(p => p.LicenseNumber != null && licenses.Contains(p.LicenseNumber)).ToDictionaryAsync(p => p.LicenseNumber!, ct);
        var clubsByName = association.Clubs.GroupBy(c => c.Name, StringComparer.OrdinalIgnoreCase).ToDictionary(g => g.Key, g => g.First(), StringComparer.OrdinalIgnoreCase);

        foreach (var member in source.Members)
        {
            if (!existing.TryGetValue(member.License, out var player))
            {
                player = AddTo(association.Players, new Player { Association = association, Name = member.Name, LicenseNumber = member.License });
                existing[member.License] = player;
            }

            player.Name = member.Name;
            player.City = member.City;
            player.BirthDate = member.BirthDate;
            player.Category = member.Category;
            player.Association = association;
            player.Club = member.ClubName is { } clubName ? clubsByName.GetValueOrDefault(clubName) : null;
            player.ClubId = player.Club?.Id;
            player.IsActive = true;
        }

        // Nicht mehr gelistete Mitglieder dieses Unterverbands sind ausgetreten.
        foreach (var former in association.Players.Where(p => p.LicenseNumber is not null && !existing.ContainsKey(p.LicenseNumber)))
        {
            former.IsActive = false;
        }

        return source.Members.Count;
    }

    private static T AddTo<T>(List<T> list, T item)
    {
        list.Add(item);
        return item;
    }
}
