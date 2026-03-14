using ScoreCast.Ws.Application.V1.Interfaces;
using ScoreCast.Models.V1.Requests;
using ScoreCast.Models.V1.Responses;
using ScoreCast.Models.V1.Responses.Prediction;

namespace ScoreCast.Ws.Application.V1.PredictionGame.Queries;

public record GetMyLeaguesQuery(ScoreCastRequest Request)
    : IQuery<ScoreCastResponse<List<PredictionLeagueResult>>>;
