using FastEndpoints;
using ScoreCast.Models.V1.Requests.Prediction;
using ScoreCast.Models.V1.Responses;
using ScoreCast.Models.V1.Responses.Prediction;

namespace ScoreCast.Ws.Application.V1.PredictionGame.Commands;

public record EnrollUserSeasonCommand(EnrollUserSeasonRequest Request)
    : ICommand<ScoreCastResponse<UserSeasonResult>>;
