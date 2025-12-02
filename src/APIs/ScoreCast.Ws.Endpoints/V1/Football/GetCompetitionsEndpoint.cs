using ScoreCast.Models.V1.Responses;
using ScoreCast.Models.V1.Responses.Football;
using ScoreCast.Ws.Application.V1.Football.Queries;

namespace ScoreCast.Ws.Endpoints.V1.Football;

public sealed class GetCompetitionsEndpoint : EndpointWithoutRequest<ScoreCastResponse<List<CompetitionResult>>>
{
    public override void Configure()
    {
        Get("/competitions");
        Group<FootballGroup>();
        AllowAnonymous();
        Summary(s =>
        {
            s.Summary = "Get Competitions";
            s.Description = "Returns all active competitions";
        });
    }

    public override async Task HandleAsync(CancellationToken ct)
    {
        var result = await new GetCompetitionsQuery().ExecuteAsync(ct);
        await Send.OkAsync(result, ct);
    }
}
