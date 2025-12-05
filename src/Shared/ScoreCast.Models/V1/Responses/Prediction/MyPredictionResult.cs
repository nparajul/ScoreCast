using ScoreCast.Shared.Enums;

namespace ScoreCast.Models.V1.Responses.Prediction;

public record MyPredictionResult(long MatchId, int PredictedHomeScore, int PredictedAwayScore, PredictionOutcome? Outcome);
