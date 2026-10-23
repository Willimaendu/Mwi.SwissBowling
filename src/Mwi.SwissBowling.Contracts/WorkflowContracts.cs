namespace Mwi.SwissBowling.Contracts;

public enum PlayerCategory { A, B, C, Guest }
public enum EventState { Draft, Published, Correcting }
public sealed record IdentityProfile(Guid? AccountId, string DisplayName, bool IsApproved, bool IsAdministrator, string[] Roles);
public sealed record PendingAccount(Guid Id, string DisplayName, DateTimeOffset RegisteredAt);
public sealed record ApproveAccountRequest(bool IdentityVerified, Guid? PlayerId);
public sealed record SeasonInput(int Year);
public sealed record ClubInput(string Name, string? Email, string? Phone);
public sealed record PlayerInput(string FirstName, string LastName, string? SfkvNumber, DateOnly? DateOfBirth, string? Email, string? Phone);
public sealed record SeasonMemberInput(Guid SeasonId, Guid PlayerId, Guid? ClubId, PlayerCategory Category);
public sealed record VenueInput(string Name, string Address, int[] Lanes);
public sealed record AnnualChampionshipInput(Guid SeasonId, string Name);
public sealed record EventInput(Guid AnnualChampionshipId, string Name, Guid VenueId, DateOnly StartDate, DateOnly EndDate, int? MaximumThrows);
public sealed record SeasonDto(Guid Id, int Year);
public sealed record ClubDto(Guid Id, string Name, string? Email, string? Phone);
public sealed record PlayerDto(Guid Id, string FirstName, string LastName, string? SfkvNumber, DateOnly? DateOfBirth, string? Email, string? Phone);
public sealed record SeasonMemberDto(Guid SeasonId, Guid PlayerId, Guid? ClubId, PlayerCategory Category);
public sealed record VenueDto(Guid Id, string Name, string Address, int[] Lanes);
public sealed record AnnualChampionshipDto(Guid Id, Guid SeasonId, string Name);
public sealed record EventDto(Guid Id, Guid AnnualChampionshipId, string Name, Guid VenueId, DateOnly StartDate, DateOnly EndDate, int? MaximumThrows, EventState State, int PublishedRevision);
public sealed record SetupDto(SeasonDto[] Seasons, ClubDto[] Clubs, PlayerDto[] Players, SeasonMemberDto[] Members, VenueDto[] Venues, AnnualChampionshipDto[] AnnualChampionships, EventDto[] Events);
public sealed record LeaseDto(Guid Token, DateTimeOffset ExpiresAt, int RenewAfterSeconds);
public sealed record LeaseTokenInput(Guid Token);
public sealed record ResultInput(int Total, int Nines, int Eights, bool Interrupted, long ExpectedVersion, Guid LeaseToken);
public sealed record ResultDto(Guid PlayerId, string Name, string? Club, PlayerCategory Category, int Total, int Nines, int Eights, bool Interrupted, long Version);
public sealed record RankedRow(int Rank, string Name, string? Club, PlayerCategory Category, int Total, int Nines, int Eights, bool Interrupted);
public sealed record PublicEventDto(Guid Id, string Name, string Venue, int Season, DateOnly StartDate, DateOnly EndDate, DateTimeOffset PublishedAt, int Revision, bool UnderCorrection, RankedRow[] Standings);
public sealed record PublicEventSummary(Guid Id, string Name, int Season, DateTimeOffset PublishedAt, int Revision, bool UnderCorrection);
public sealed record CorrectionInput(string Reason);
public sealed record CorrectionDto(Guid Id, Guid EventId, string EventName, string Reason, string RequestedBy, DateTimeOffset RequestedAt);
public sealed record CreatedId(Guid Id);
