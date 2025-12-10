using ScoreCast.Shared.Enums;

namespace ScoreCast.Models.V1.Responses.Football;

public record PointsTableResult(
    CompetitionFormat Format,
    List<PointsTableGroup> Groups,
    List<PointsTableRow> BestThirdPlaced,
    List<KnockoutRound> KnockoutRounds);

public record PointsTableGroup(
    string? GroupName,
    List<PointsTableRow> Rows);

public record KnockoutRound(string Name, int SortOrder, List<KnockoutMatch> Matches);

public record KnockoutMatch(
    long MatchId,
    string? HomeTeam, string? HomeTeamLogo,
    string? AwayTeam, string? AwayTeamLogo,
    int? HomeScore, int? AwayScore,
    string Status, DateTime? KickoffTime);
