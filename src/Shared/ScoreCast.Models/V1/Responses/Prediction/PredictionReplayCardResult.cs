namespace ScoreCast.Models.V1.Responses.Prediction;

public record PredictionReplayCardResult(
    string DisplayName,
    string HomeTeam,
    string AwayTeam,
    int HomeScore,
    int AwayScore,
    int PredictedHome,
    int PredictedAway,
    string OutcomeLabel,
    string OutcomeColor,
    int Points);
