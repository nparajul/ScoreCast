using ScoreCast.Shared.Types;

namespace ScoreCast.Models.V1.Responses.Football;

public record TeamMatchesResult(List<TeamMatchDetail> Matches);

public record TeamMatchDetail(
    long MatchId, ScoreCastDateTime? KickoffTime, string Status,
    long HomeTeamId, string HomeTeamName, string? HomeTeamLogo, string HomeTeamShortName,
    long AwayTeamId, string AwayTeamName, string? AwayTeamLogo, string AwayTeamShortName,
    int? HomeScore, int? AwayScore,
    string? Venue, string? Referee, string? Minute,
    string CompetitionName, string? CompetitionLogo,
    List<MatchEventDetail> Events);
