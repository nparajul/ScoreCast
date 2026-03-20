namespace ScoreCast.Models.V1.Responses.Prediction;

public record PlayerGameweekResult(
    List<MyPredictionResult> Predictions,
    List<RiskPlayResult> RiskPlays,
    bool PredictionsVisible,
    bool RiskPlaysVisible);
