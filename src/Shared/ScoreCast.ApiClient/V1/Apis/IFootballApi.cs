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

    [Post("/api/v1/football/sync/teams")]
    Task<ScoreCastResponse> SyncTeamsAsync([Body] SyncCompetitionRequest request, CancellationToken ct);

    [Post("/api/v1/football/sync/matches")]
    Task<ScoreCastResponse> SyncMatchesAsync([Body] SyncCompetitionRequest request, CancellationToken ct);

    [Post("/api/v1/football/sync/fpl")]
    Task<ScoreCastResponse> SyncFplDataAsync([Body] SyncCompetitionRequest request, CancellationToken ct);

    [Post("/api/v1/football/sync/pulse-events")]
    Task<ScoreCastResponse<SyncPulseEventsResult>> SyncPulseEventsAsync([Body] SyncPulseEventsRequest request, CancellationToken ct);

    [Get("/api/v1/football/competitions/{competitionCode}/seasons")]
    Task<ScoreCastResponse<List<SeasonResult>>> GetSeasonsAsync(string competitionCode, CancellationToken ct);

    [Get("/api/v1/football/seasons/{seasonId}/table")]
    Task<ScoreCastResponse<List<LeagueTableRow>>> GetLeagueTableAsync(long seasonId, CancellationToken ct);

    [Get("/api/v1/football/competitions/{competitionCode}/zones")]
    Task<ScoreCastResponse<List<CompetitionZoneResult>>> GetCompetitionZonesAsync(string competitionCode, CancellationToken ct);

    [Get("/api/v1/football/seasons/{seasonId}/gameweek/{gameweekNumber}")]
    Task<ScoreCastResponse<GameweekMatchesResult>> GetGameweekMatchesAsync(long seasonId, int gameweekNumber, CancellationToken ct);
}
