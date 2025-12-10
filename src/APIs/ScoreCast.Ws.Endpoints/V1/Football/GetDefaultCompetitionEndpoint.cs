using ScoreCast.Models.V1.Responses;
using ScoreCast.Models.V1.Responses.Football;
using ScoreCast.Ws.Application.V1.Config.Queries;

namespace ScoreCast.Ws.Endpoints.V1.Football;

public sealed class GetDefaultCompetitionEndpoint : EndpointWithoutRequest<ScoreCastResponse<CompetitionResult>>
{
    public override void Configure()
    {
        Get("/competitions/default");
        Group<FootballGroup>();
        Summary(s =>
        {
            s.Summary = "Get Default Competition";
            s.Description = "Returns the current default competition from app config";
        });
    }

    public override async Task HandleAsync(CancellationToken ct)
    {
        var result = await new GetDefaultCompetitionQuery().ExecuteAsync(ct);
        await Send.OkAsync(result, ct);
    }
}
