using Refit;
using ScoreCast.Models.V1.Requests.Football;
using ScoreCast.Models.V1.Responses;
using ScoreCast.Models.V1.Responses.Football;

namespace ScoreCast.ApiClient.V1.Apis;

public partial interface IScoreCastApiClient
{
    [Get("/api/v1/football/competitions")]
    Task<ScoreCastResponse<List<CompetitionResult>>> GetCompetitionsAsync(CancellationToken ct);

    [Get("/api/v1/football/competitions/{competitionName}/teams")]
    Task<ScoreCastResponse<List<TeamResult>>> GetTeamsAsync(string competitionName, CancellationToken ct);

    [Post("/api/v1/football/sync/competition")]
    Task<ScoreCastResponse> SyncCompetitionAsync([Body] SyncCompetitionRequest request, CancellationToken ct);
}
