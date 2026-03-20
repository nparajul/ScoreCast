using ScoreCast.Ws.Application.V1.Interfaces;
using ScoreCast.Models.V1.Responses;
using ScoreCast.Models.V1.Responses.Prediction;

namespace ScoreCast.Ws.Application.V1.PredictionGame.Queries;

public record GetPlayerProfileQuery(long TargetUserId, long PredictionLeagueId, string RequestingUserId)
    : IQuery<ScoreCastResponse<PlayerProfileResult>>;
