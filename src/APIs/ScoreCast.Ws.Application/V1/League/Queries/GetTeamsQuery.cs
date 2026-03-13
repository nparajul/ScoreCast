using FastEndpoints;
using ScoreCast.Models.V1.Responses;
using ScoreCast.Models.V1.Responses.League;

namespace ScoreCast.Ws.Application.V1.League.Queries;

public record GetTeamsQuery(string LeagueName) : ICommand<ScoreCastResponse<List<TeamResult>>>;
