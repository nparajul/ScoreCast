using ScoreCast.Models.V1.Responses;
using ScoreCast.Models.V1.Responses.Football;
using ScoreCast.Ws.Application.V1.Interfaces;

namespace ScoreCast.Ws.Application.V1.Football.Queries;

public record GetBracketQuery(long SeasonId) : IQuery<ScoreCastResponse<BracketResult>>;
