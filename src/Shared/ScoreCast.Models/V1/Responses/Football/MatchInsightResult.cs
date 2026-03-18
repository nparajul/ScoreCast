namespace ScoreCast.Models.V1.Responses.Football;

public record MatchInsightResult(
    long MatchId,
    long HomeTeamId,
    string HomeTeamName,
    string? HomeTeamShortName,
    string? HomeTeamLogo,
    long AwayTeamId,
    string AwayTeamName,
    string? AwayTeamShortName,
    string? AwayTeamLogo,
    DateTime? KickoffTime,
    int HomeWinPct,
    int DrawPct,
    int AwayWinPct,
    string? AiSummary,
    double? HomeXg = null,
    double? AwayXg = null,
    string? TopScoreline = null,
    int? TopScorelinePct = null);
