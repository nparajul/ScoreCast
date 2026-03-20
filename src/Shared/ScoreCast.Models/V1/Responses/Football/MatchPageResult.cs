namespace ScoreCast.Models.V1.Responses.Football;

public record MatchPageResult(
    long MatchId, DateTime? KickoffTime, string Status, string? Minute,
    int? ClockSeconds, string? Phase, long? SecondHalfStartMillis,
    long HomeTeamId, string HomeTeamName, string? HomeTeamLogo, string HomeTeamShortName,
    long AwayTeamId, string AwayTeamName, string? AwayTeamLogo, string AwayTeamShortName,
    int? HomeScore, int? AwayScore,
    string? Venue, string? Referee,
    int? HalfTimeHomeScore, int? HalfTimeAwayScore,
    string CompetitionName, string? CompetitionLogo,
    string? HomeFormation, string? AwayFormation,
    string? HomeCoach, string? AwayCoach,
    List<MatchPageLineupPlayer> HomeLineup, List<MatchPageLineupPlayer> HomeSubs,
    List<MatchPageLineupPlayer> AwayLineup, List<MatchPageLineupPlayer> AwaySubs,
    List<MatchPageEvent> Events);

public record MatchPageLineupPlayer(
    long PlayerId, string Name, string? PhotoUrl,
    int? ShirtNumber, string? Position, bool IsCaptain,
    List<string> Icons, string? SubMinute);

public record MatchPageEvent(
    string EventType, string PlayerName, string? AssistName,
    string? Minute, bool IsHome, double SortKey,
    string? PlayerOff, string? RunningScore);
