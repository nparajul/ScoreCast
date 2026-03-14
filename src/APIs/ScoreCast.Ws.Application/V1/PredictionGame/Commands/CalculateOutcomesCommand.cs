using FastEndpoints;
using ScoreCast.Models.V1.Responses;

namespace ScoreCast.Ws.Application.V1.PredictionGame.Commands;

public record CalculateOutcomesCommand(long SeasonId)
    : ICommand<ScoreCastResponse>;
