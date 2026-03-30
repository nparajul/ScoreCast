namespace ScoreCast.Models.V1.Responses.Prediction;

public record GameweekReplayResult(
    string DisplayName,
    int GameweekNumber,
    string CompetitionName,
    string? CompetitionLogo,
    int TotalPoints,
    int MatchesPredicted,
    int CorrectResults,
    int ExactScores,
    List<GameweekReplayMatch> Matches);

public record GameweekReplayMatch(
    long MatchId,
    string HomeTeam,
    string AwayTeam,
    string? HomeLogo,
    string? AwayLogo,
    int HomeScore,
    int AwayScore,
    int PredictedHome,
    int PredictedAway,
    string? Outcome,
    int Points,
    string? DeathMinute);
