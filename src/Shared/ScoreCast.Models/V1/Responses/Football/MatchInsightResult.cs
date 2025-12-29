namespace ScoreCast.Models.V1.Responses.Football;

public record MatchInsightResult(
    long MatchId,
    string HomeTeamName,
    string? HomeTeamShortName,
    string? HomeTeamLogo,
    string AwayTeamName,
    string? AwayTeamShortName,
    string? AwayTeamLogo,
    DateTime? KickoffTime,
    int HomeWinPct,
    int DrawPct,
    int AwayWinPct,
    string? AiSummary);
