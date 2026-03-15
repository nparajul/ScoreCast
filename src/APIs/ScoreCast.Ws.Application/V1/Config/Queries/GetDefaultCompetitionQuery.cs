using ScoreCast.Models.V1.Responses;
using ScoreCast.Models.V1.Responses.Football;
using ScoreCast.Ws.Application.V1.Interfaces;

namespace ScoreCast.Ws.Application.V1.Config.Queries;

public record GetDefaultCompetitionQuery : IQuery<ScoreCastResponse<CompetitionResult>>;
