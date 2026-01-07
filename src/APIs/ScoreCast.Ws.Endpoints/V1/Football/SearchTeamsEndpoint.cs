using ScoreCast.Models.V1.Requests.Football;
using ScoreCast.Models.V1.Responses;
using ScoreCast.Models.V1.Responses.Football;
using ScoreCast.Ws.Application.V1.Football.Queries;

namespace ScoreCast.Ws.Endpoints.V1.Football;

public sealed class SearchTeamsEndpoint : Endpoint<SearchTeamsRequest, ScoreCastResponse<TeamSearchResult>>
{
    public override void Configure()
    {
        Get("/teams/search");
        Group<FootballGroup>();
    }

    public override async Task HandleAsync(SearchTeamsRequest request, CancellationToken ct)
    {
        var result = await new SearchTeamsQuery(request.Q, request.Skip, request.Take).ExecuteAsync(ct);
        await Send.OkAsync(result, ct);
    }
}
