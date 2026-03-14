using ScoreCast.Models.V1.Requests.Football;
using ScoreCast.Models.V1.Responses;
using ScoreCast.Models.V1.Responses.Football;
using ScoreCast.Shared.Constants;
using ScoreCast.Ws.Application.V1.Football.Queries;

namespace ScoreCast.Ws.Endpoints.V1.Football;

public sealed class GetGameweekMatchesEndpoint : Endpoint<GetGameweekMatchesRequest, ScoreCastResponse<GameweekMatchesResult>>
{
    public override void Configure()
    {
        Get("/seasons/{SeasonId}/gameweek/{GameweekNumber}");
        Group<FootballGroup>();
        AllowAnonymous();
    }

    public override async Task HandleAsync(GetGameweekMatchesRequest request, CancellationToken ct)
    {
        var result = await new GetGameweekMatchesQuery(request.SeasonId, request.GameweekNumber == SharedConstants.CurrentGameweek ? null : request.GameweekNumber).ExecuteAsync(ct);
        await Send.OkAsync(result, ct);
    }
}
