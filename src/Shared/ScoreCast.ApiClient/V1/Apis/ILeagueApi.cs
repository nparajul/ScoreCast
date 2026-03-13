using Refit;
using ScoreCast.Models.V1.Responses;
using ScoreCast.Models.V1.Responses.League;

namespace ScoreCast.ApiClient.V1.Apis;

public interface ILeagueApi
{
    [Get("/api/v1/leagues/{leagueName}/teams")]
    Task<ScoreCastResponse<List<TeamResult>>> GetTeamsAsync(string leagueName, CancellationToken ct);
}
