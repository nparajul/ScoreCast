using ScoreCast.Models.V1.Responses;
using ScoreCast.Models.V1.Responses.Prediction;
using ScoreCast.Ws.Application.V1.Interfaces;

namespace ScoreCast.Ws.Application.V1.PredictionGame.Queries;

public record GetGameweekReplayQuery(long SeasonId, int GameweekNumber, long UserId)
    : IQuery<ScoreCastResponse<GameweekReplayResult>>;
