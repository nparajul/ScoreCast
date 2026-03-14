using FastEndpoints;
using ScoreCast.Models.V1.Responses;
using ScoreCast.Models.V1.Responses.Prediction;

namespace ScoreCast.Ws.Application.V1.PredictionGame.Queries;

public record GetMyPredictionsQuery(long PredictionLeagueId, long GameweekId, string UserId)
    : ICommand<ScoreCastResponse<List<MyPredictionResult>>>;
