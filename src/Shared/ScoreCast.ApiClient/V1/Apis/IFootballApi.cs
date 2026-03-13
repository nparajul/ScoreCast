using Refit;
using ScoreCast.Models.V1.Responses;
using ScoreCast.Models.V1.Responses.League;

namespace ScoreCast.ApiClient.V1.Apis;

public interface IFootballApi
{
    [Get("/api/v1/football/competitions/{competitionName}/teams")]
    Task<ScoreCastResponse<List<TeamResult>>> GetTeamsAsync(string competitionName, CancellationToken ct);
}
