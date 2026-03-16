using ScoreCast.Ws.Application.V1.Interfaces;
using ScoreCast.Models.V1.Responses;
using ScoreCast.Models.V1.Responses.Football;

namespace ScoreCast.Ws.Application.V1.Football.Queries;

public record GetTeamPlayerStatsQuery(long TeamId, long? SeasonId = null) : IQuery<ScoreCastResponse<PlayerStatsResult>>;
