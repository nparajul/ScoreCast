namespace ScoreCast.Models.V1.Responses.Prediction;

public record MyPredictionStatsResult(
    int CurrentStreak,
    string StreakType,
    int BestStreak,
    int TotalPredictions,
    int TotalCorrectResults,
    int TotalExactScores,
    List<string> Achievements,
    GameweekComparison? LastGameweek);

public record GameweekComparison(
    int GameweekNumber,
    int UserCorrect,
    int UserTotal,
    double CommunityAvgCorrect,
    double CommunityAvgTotal,
    double BeatPct);
