namespace ScoreCast.Models.V1.Responses.Prediction;

public record LeagueStandingsResult(
    string LeagueName, long SeasonId, List<LeagueStandingRow> Standings);

public record LeagueStandingRow(
    long UserId, string DisplayName, string? AvatarUrl,
    int TotalPoints, int ExactScores, int CorrectResults, int PredictionCount);
