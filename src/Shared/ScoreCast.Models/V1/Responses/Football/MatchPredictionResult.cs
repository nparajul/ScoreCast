namespace ScoreCast.Models.V1.Responses.Football;

public record MatchPredictionResult(
    double HomeExpectedGoals,
    double AwayExpectedGoals,
    int HomeWinPct,
    int DrawPct,
    int AwayWinPct,
    List<ScorelineProbability> TopScorelines);

public record ScorelineProbability(int Home, int Away, int Pct);
