using Refit;
using ScoreCast.Models.V1.Requests.MasterData;
using ScoreCast.Models.V1.Responses;
using ScoreCast.Models.V1.Responses.MasterData;

namespace ScoreCast.ApiClient.V1.Apis;

public partial interface IScoreCastApiClient
{
    [Post("/api/v1/master-data/sync/competition")]
    Task<ScoreCastResponse> SyncCompetitionAsync([Body] SyncCompetitionRequest request, CancellationToken ct);

    [Post("/api/v1/master-data/sync/teams")]
    Task<ScoreCastResponse> SyncTeamsAsync([Body] SyncCompetitionRequest request, CancellationToken ct);

    [Post("/api/v1/master-data/sync/matches")]
    Task<ScoreCastResponse> SyncMatchesAsync([Body] SyncCompetitionRequest request, CancellationToken ct);

    [Post("/api/v1/master-data/sync/fpl")]
    Task<ScoreCastResponse> SyncFplDataAsync([Body] SyncCompetitionRequest request, CancellationToken ct);

    [Post("/api/v1/master-data/sync/pulse-events")]
    Task<ScoreCastResponse<SyncPulseEventsResult>> SyncPulseEventsAsync([Body] SyncPulseEventsRequest request, CancellationToken ct);
}
