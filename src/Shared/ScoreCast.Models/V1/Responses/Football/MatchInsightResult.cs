namespace ScoreCast.Models.V1.Responses.Football;

public record MatchInsightResult(
    long MatchId,
    string HomeTeamName,
    string? HomeTeamLogo,
    string AwayTeamName,
    string? AwayTeamLogo,
    DateTime? KickoffTime,
    int HomeWinPct,
    int DrawPct,
    int AwayWinPct,
    string? AiSummary);
