namespace ScoreCast.Models.V1.Responses.Prediction;

public record PredictionReplayResult(
    long MatchId,
    string DisplayName,
    string HomeTeam,
    string AwayTeam,
    string? HomeLogo,
    string? AwayLogo,
    int HomeScore,
    int AwayScore,
    int PredictedHome,
    int PredictedAway,
    string? Outcome,
    int PointsEarned,
    List<ReplayGoalEvent> GoalTimeline,
    string? DeathMinute,
    List<LeagueRivalComparison> LeagueRivals,
    ReplaySeasonAccuracy SeasonAccuracy,
    string? AiCommentary);

public record ReplayGoalEvent(string Minute, string Team, string Scorer, int RunningHome, int RunningAway, string PredictionStatus);

public record LeagueRivalComparison(string DisplayName, string? AvatarUrl, int PredictedHome, int PredictedAway, string? Outcome, int Points);

public record ReplaySeasonAccuracy(int TotalPredictions, int ExactScores, int CorrectResults, int AccuracyPct, string Trend);
