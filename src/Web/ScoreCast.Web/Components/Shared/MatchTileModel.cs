using ScoreCast.Models.V1.Responses.Football;
using ScoreCast.Shared.Types;

namespace ScoreCast.Web.Components.Shared;

public record MatchTileModel(
    long MatchId, DateTime? KickoffTime, string Status,
    long HomeTeamId, string HomeTeamName, string? HomeTeamLogo, string HomeTeamShortName,
    long AwayTeamId, string AwayTeamName, string? AwayTeamLogo, string AwayTeamShortName,
    int? HomeScore, int? AwayScore,
    string? Venue, string? Referee, string? Minute,
    string? CompetitionName, string? CompetitionLogo,
    List<MatchEventDetail> Events)
{
    public static MatchTileModel From(MatchDetail m) => new(
        m.MatchId, m.KickoffTime, m.Status,
        m.HomeTeamId, m.HomeTeamName, m.HomeTeamLogo, m.HomeTeamShortName,
        m.AwayTeamId, m.AwayTeamName, m.AwayTeamLogo, m.AwayTeamShortName,
        m.HomeScore, m.AwayScore, m.Venue, m.Referee, m.Minute,
        null, null, m.Events);

    public static MatchTileModel From(TeamMatchDetail m) => new(
        m.MatchId, m.KickoffTime?.Value, m.Status,
        m.HomeTeamId, m.HomeTeamName, m.HomeTeamLogo, m.HomeTeamShortName,
        m.AwayTeamId, m.AwayTeamName, m.AwayTeamLogo, m.AwayTeamShortName,
        m.HomeScore, m.AwayScore, m.Venue, m.Referee, m.Minute,
        m.CompetitionName, m.CompetitionLogo, m.Events);
}
