using FastEndpoints;
using ScoreCast.Models.V1.Requests.Prediction;
using ScoreCast.Models.V1.Responses;

namespace ScoreCast.Ws.Application.V1.PredictionGame.Commands;

public record CalculatePredictionPointsCommand(CalculatePredictionPointsRequest Request)
    : ICommand<ScoreCastResponse>;
