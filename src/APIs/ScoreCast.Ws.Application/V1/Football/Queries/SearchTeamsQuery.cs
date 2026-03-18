using ScoreCast.Ws.Application.V1.Interfaces;
using ScoreCast.Models.V1.Responses;
using ScoreCast.Models.V1.Responses.Football;

namespace ScoreCast.Ws.Application.V1.Football.Queries;

public record SearchTeamsQuery(string? SearchTerm = null, int Skip = 0, int Take = 50) : IQuery<ScoreCastResponse<TeamSearchResult>>;
