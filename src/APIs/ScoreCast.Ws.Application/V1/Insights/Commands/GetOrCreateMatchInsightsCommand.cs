using FastEndpoints;
using ScoreCast.Models.V1.Requests.Insights;
using ScoreCast.Models.V1.Responses;
using ScoreCast.Models.V1.Responses.Football;

namespace ScoreCast.Ws.Application.V1.Insights.Commands;

public record GetOrCreateMatchInsightsCommand(GetMatchInsightsRequest Request)
    : ICommand<ScoreCastResponse<List<MatchInsightResult>>>;
