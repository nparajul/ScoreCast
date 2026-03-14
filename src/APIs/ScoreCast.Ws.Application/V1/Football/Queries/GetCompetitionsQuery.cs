using FastEndpoints;
using ScoreCast.Models.V1.Responses;
using ScoreCast.Models.V1.Responses.Football;

namespace ScoreCast.Ws.Application.V1.Football.Queries;

public record GetCompetitionsQuery : ICommand<ScoreCastResponse<List<CompetitionResult>>>;
