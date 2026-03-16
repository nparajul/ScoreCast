using ScoreCast.Models.V1.Responses;
using ScoreCast.Models.V1.Responses.Football;
using ScoreCast.Ws.Application.V1.Interfaces;

namespace ScoreCast.Ws.Application.V1.Insights.Queries;

public record GetMatchInsightsQuery(long SeasonId, int GameweekNumber)
    : IQuery<ScoreCastResponse<List<MatchInsightResult>>>;
