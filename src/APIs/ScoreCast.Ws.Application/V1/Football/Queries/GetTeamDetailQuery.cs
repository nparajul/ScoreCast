using ScoreCast.Ws.Application.V1.Interfaces;
using ScoreCast.Models.V1.Responses;
using ScoreCast.Models.V1.Responses.Football;

namespace ScoreCast.Ws.Application.V1.Football.Queries;

public record GetTeamDetailQuery(long TeamId) : IQuery<ScoreCastResponse<TeamDetailResult>>;
