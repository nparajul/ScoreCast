namespace ScoreCast.Models.V1.Responses.Prediction;

public record PlayerProfileResult(
    long UserId, string DisplayName, string? AvatarUrl, string? FavoriteTeam,
    int TotalPoints, int BestGameweek, int MatchweeksPlayed, int ExactScores, int CorrectResults,
    decimal AveragePointsPerGameweek);
