namespace ScoreCast.Models.V1.Responses.Football;

public record GameweekMatchesResult(
    long GameweekId, int GameweekNumber, DateOnly? StartDate, DateOnly? EndDate,
    int TotalGameweeks, int CurrentGameweek, List<MatchDetail> Matches);

public record MatchDetail(
    long MatchId, DateTime? KickoffTime, string Status,
    string HomeTeamName, string? HomeTeamLogo, string HomeTeamShortName,
    string AwayTeamName, string? AwayTeamLogo, string AwayTeamShortName,
    int? HomeScore, int? AwayScore,
    string? Venue, string? Referee, string? Minute,
    List<MatchEventDetail> Events);

public record MatchEventDetail(
    string PlayerName, string EventType, int Value, bool IsHome, string? Minute);
