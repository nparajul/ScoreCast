namespace ScoreCast.Models.V1.Responses.Football;

public record GlobalDashboardResult(
    GameweekCountdown Countdown,
    List<MatchPredictionSummary> UpcomingPredictions,
    List<GlobalLeaderboardEntry> TopPredictors,
    CommunityStats Community,
    GameweekRecap? LastGameweekRecap = null);

public record GameweekCountdown(
    int GameweekNumber,
    DateTime Deadline,
    int TotalPredictions,
    int TotalUsers,
    bool IsComplete = false);

public record MatchPredictionSummary(
    long MatchId,
    string HomeTeam,
    string AwayTeam,
    string? HomeTeamCrest,
    string? AwayTeamCrest,
    DateTime KickoffTime,
    int PredictionCount,
    string MostPredictedScore,
    double MostPredictedPct,
    double HomePct,
    double DrawPct,
    double AwayPct,
    string? HomeTeamShortName = null,
    string? AwayTeamShortName = null);

public record GlobalLeaderboardEntry(
    int Rank,
    string Username,
    int TotalPoints,
    int ExactScores,
    int TotalPredictions);

public record CommunityStats(
    int TotalPredictors,
    int TotalPredictions,
    int ExactScores,
    double ExactScorePct,
    string HardestMatch,
    double HardestMatchAccuracy,
    string MostPredictableTeam,
    double MostPredictableTeamPct);

public record GameweekRecap(
    int GameweekNumber,
    string BestPredictor,
    int BestPredictorPoints,
    int TotalExactScores,
    int TotalPredictors,
    string? BiggestUpset,
    string? BoldestCorrectPrediction);
