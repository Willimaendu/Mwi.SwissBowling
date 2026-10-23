using System.Security.Claims;
using HttpResults = Microsoft.AspNetCore.Http.Results;
using Microsoft.EntityFrameworkCore;
using Mwi.SwissBowling.Api.Features.Identity;
using Mwi.SwissBowling.Api.Persistence;
using Mwi.SwissBowling.Contracts;

namespace Mwi.SwissBowling.Api.Features.Setup;

public static class SetupEndpoints
{
    public static void MapSetupEndpoints(this IEndpointRouteBuilder endpoints)
    {
        var group = endpoints.MapGroup("/api/setup").RequireAuthorization(ApiIdentity.Administrator);
        group.MapGet("", ReadAsync);
        group.MapPost("/seasons", async (SeasonInput input, ApplicationDbContext database, ClaimsPrincipal user, TimeProvider clock, CancellationToken cancellationToken) =>
        {
            if (input.Year is < 1 or > 9999) return Invalid("Bitte ein gültiges Jahr angeben.");
            var entity = new Season { Id = Guid.NewGuid(), Year = input.Year };
            return await CreateAsync(database, entity, entity.Id, "SeasonCreated", user, clock, cancellationToken);
        });
        group.MapPost("/clubs", async (ClubInput input, ApplicationDbContext database, ClaimsPrincipal user, TimeProvider clock, CancellationToken cancellationToken) =>
        {
            if (!SetupValidation.Text(input.Name, 160) || !SetupValidation.Contact(input.Email, input.Phone)) return Invalid("Bitte Klubname und Kontaktdaten prüfen.");
            var entity = new Club { Id = Guid.NewGuid(), Name = input.Name.Trim(), Email = SetupValidation.Optional(input.Email), Phone = SetupValidation.Optional(input.Phone) };
            return await CreateAsync(database, entity, entity.Id, "ClubCreated", user, clock, cancellationToken);
        });
        group.MapPost("/players", CreatePlayerAsync);
        group.MapPut("/players/{id:guid}", UpdatePlayerAsync);
        group.MapPost("/members", AssignMemberAsync);
        group.MapPost("/venues", CreateVenueAsync);
        group.MapPost("/annual-championships", async (AnnualChampionshipInput input, ApplicationDbContext database, ClaimsPrincipal user, TimeProvider clock, CancellationToken cancellationToken) =>
        {
            if (!SetupValidation.Text(input.Name, 160) || !await database.Seasons.AnyAsync(row => row.Id == input.SeasonId, cancellationToken)) return Invalid("Eine bestehende Saison und ein Name sind erforderlich.");
            var entity = new AnnualChampionship { Id = Guid.NewGuid(), SeasonId = input.SeasonId, Name = input.Name.Trim() };
            return await CreateAsync(database, entity, entity.Id, "AnnualChampionshipCreated", user, clock, cancellationToken);
        });
        group.MapPost("/events", async (EventInput input, ApplicationDbContext database, ClaimsPrincipal user, TimeProvider clock, CancellationToken cancellationToken) =>
        {
            var error = SetupValidation.Event(input);
            if (error is not null) return Invalid(error);
            if (!await database.AnnualChampionships.AnyAsync(row => row.Id == input.AnnualChampionshipId, cancellationToken) || !await database.Venues.AnyAsync(row => row.Id == input.VenueId, cancellationToken)) return Invalid("Jahresmeisterschaft und Anlage müssen vorhanden sein.");
            var entity = new ChampionshipEvent { Id = Guid.NewGuid(), AnnualChampionshipId = input.AnnualChampionshipId, Name = input.Name.Trim(), VenueId = input.VenueId, StartDate = input.StartDate, EndDate = input.EndDate, MaximumThrows = input.MaximumThrows };
            return await CreateAsync(database, entity, entity.Id, "EventCreated", user, clock, cancellationToken);
        });
    }

    private static async Task<IResult> CreateVenueAsync(VenueInput input, ApplicationDbContext database, ClaimsPrincipal user, TimeProvider clock, CancellationToken cancellationToken)
    {
        if (!SetupValidation.Text(input.Name, 160) || !SetupValidation.Text(input.Address, 300) || input.Lanes is not { Length: > 0 } || input.Lanes.Any(number => number <= 0) || input.Lanes.Distinct().Count() != input.Lanes.Length) return Invalid("Bitte Anlage, Adresse und eindeutige positive Bahnnummern angeben.");
        var entity = new Venue { Id = Guid.NewGuid(), Name = input.Name.Trim(), Address = input.Address.Trim() };
        entity.Lanes = input.Lanes.Select(number => new Lane { VenueId = entity.Id, Number = number }).ToList();
        return await CreateAsync(database, entity, entity.Id, "VenueCreated", user, clock, cancellationToken);
    }

    private static async Task<SetupDto> ReadAsync(ApplicationDbContext database, CancellationToken cancellationToken) => new(
        await database.Seasons.AsNoTracking().OrderByDescending(row => row.Year).Select(row => new SeasonDto(row.Id, row.Year)).ToArrayAsync(cancellationToken),
        await database.Clubs.AsNoTracking().OrderBy(row => row.Name).Select(row => new ClubDto(row.Id, row.Name, row.Email, row.Phone)).ToArrayAsync(cancellationToken),
        await database.Players.AsNoTracking().OrderBy(row => row.LastName).ThenBy(row => row.FirstName).Select(row => new PlayerDto(row.Id, row.FirstName, row.LastName, row.SfkvNumber, row.DateOfBirth, row.Email, row.Phone)).ToArrayAsync(cancellationToken),
        await database.SeasonMembers.AsNoTracking().Select(row => new SeasonMemberDto(row.SeasonId, row.PlayerId, row.ClubId, row.Category)).ToArrayAsync(cancellationToken),
        await database.Venues.AsNoTracking().OrderBy(row => row.Name).Select(row => new VenueDto(row.Id, row.Name, row.Address, row.Lanes.OrderBy(lane => lane.Number).Select(lane => lane.Number).ToArray())).ToArrayAsync(cancellationToken),
        await database.AnnualChampionships.AsNoTracking().Select(row => new AnnualChampionshipDto(row.Id, row.SeasonId, row.Name)).ToArrayAsync(cancellationToken),
        await database.Events.AsNoTracking().OrderByDescending(row => row.StartDate).Select(row => new EventDto(row.Id, row.AnnualChampionshipId, row.Name, row.VenueId, row.StartDate, row.EndDate, row.MaximumThrows, row.State, row.PublishedRevision)).ToArrayAsync(cancellationToken));

    private static async Task<IResult> CreatePlayerAsync(PlayerInput input, ApplicationDbContext database, ClaimsPrincipal user, TimeProvider clock, CancellationToken cancellationToken)
    {
        var error = SetupValidation.Player(input, DateOnly.FromDateTime(clock.GetUtcNow().UtcDateTime));
        if (error is not null) return Invalid(error);
        var entity = new Player { Id = Guid.NewGuid() };
        Apply(input, entity);
        return await CreateAsync(database, entity, entity.Id, "PlayerCreated", user, clock, cancellationToken);
    }

    private static async Task<IResult> UpdatePlayerAsync(Guid id, PlayerInput input, ApplicationDbContext database, ClaimsPrincipal user, TimeProvider clock, CancellationToken cancellationToken)
    {
        var error = SetupValidation.Player(input, DateOnly.FromDateTime(clock.GetUtcNow().UtcDateTime));
        if (error is not null) return Invalid(error);
        var entity = await database.Players.FindAsync([id], cancellationToken);
        if (entity is null) return HttpResults.NotFound();
        if (entity.SfkvNumber != SetupValidation.Optional(input.SfkvNumber) && await database.SeasonMembers.AnyAsync(row => row.PlayerId == id, cancellationToken)) return Invalid("Eine zugeordnete SFKV-Identität darf nicht direkt geändert werden.");
        Apply(input, entity);
        database.Audit("PlayerUpdated", id.ToString(), user.Issuer(), user.Subject(), clock.GetUtcNow());
        await database.SaveChangesAsync(cancellationToken);
        return HttpResults.NoContent();
    }

    private static async Task<IResult> AssignMemberAsync(SeasonMemberInput input, ApplicationDbContext database, ClaimsPrincipal user, TimeProvider clock, CancellationToken cancellationToken)
    {
        if (!Enum.IsDefined(input.Category)) return Invalid("Unbekannte Kategorie.");
        await using var transaction = await database.Database.BeginTransactionAsync(cancellationToken);
        var seasons = await database.Seasons.FromSqlInterpolated($"SELECT * FROM \"Seasons\" WHERE \"Id\" = {input.SeasonId} FOR UPDATE").ToListAsync(cancellationToken);
        var season = seasons.SingleOrDefault();
        if (season is null) return HttpResults.NotFound();
        if (season.RosterLocked) return HttpResults.Problem(statusCode: 409, title: "Die Saisonzuordnung ist nach dem ersten Resultat gesperrt. Nachmeldungen benötigen den späteren Freigabeworkflow.");
        var player = await database.Players.FindAsync([input.PlayerId], cancellationToken);
        if (player is null || input.Category != PlayerCategory.Guest && player.SfkvNumber is null) return Invalid("Ein Spieler und für A/B/C eine SFKV-Nummer sind erforderlich.");
        if (input.ClubId is not null && !await database.Clubs.AnyAsync(row => row.Id == input.ClubId, cancellationToken)) return Invalid("Der Klub ist nicht vorhanden.");
        var member = await database.SeasonMembers.FindAsync([input.SeasonId, input.PlayerId], cancellationToken);
        if (member is null) database.SeasonMembers.Add(new SeasonMember { SeasonId = input.SeasonId, PlayerId = input.PlayerId, ClubId = input.ClubId, Category = input.Category });
        else { member.ClubId = input.ClubId; member.Category = input.Category; }
        database.Audit("SeasonMemberAssigned", $"{input.SeasonId}/{input.PlayerId}", user.Issuer(), user.Subject(), clock.GetUtcNow());
        await database.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
        return HttpResults.NoContent();
    }

    private static void Apply(PlayerInput input, Player entity)
    {
        entity.FirstName = input.FirstName.Trim(); entity.LastName = input.LastName.Trim();
        entity.SfkvNumber = SetupValidation.Optional(input.SfkvNumber); entity.DateOfBirth = input.DateOfBirth;
        entity.Email = SetupValidation.Optional(input.Email); entity.Phone = SetupValidation.Optional(input.Phone);
    }

    private static async Task<IResult> CreateAsync<T>(ApplicationDbContext database, T entity, Guid id, string action, ClaimsPrincipal user, TimeProvider clock, CancellationToken cancellationToken) where T : class
    {
        database.Add(entity);
        database.Audit(action, id.ToString(), user.Issuer(), user.Subject(), clock.GetUtcNow());
        await database.SaveChangesAsync(cancellationToken);
        return HttpResults.Ok(new CreatedId(id));
    }
    private static IResult Invalid(string message) => HttpResults.ValidationProblem(new Dictionary<string, string[]> { ["input"] = [message] });
}
