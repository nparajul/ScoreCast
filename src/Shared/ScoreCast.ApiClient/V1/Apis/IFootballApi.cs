using Refit;
using ScoreCast.Models.V1.Responses;
using ScoreCast.Models.V1.Responses.Football;

namespace ScoreCast.ApiClient.V1.Apis;

public partial interface IScoreCastApiClient
{
    [Get("/api/v1/football/competitions")]
    Task<ScoreCastResponse<List<CompetitionResult>>> GetCompetitionsAsync(CancellationToken ct);

    [Get("/api/v1/football/competitions/{competitionName}/teams")]
    Task<ScoreCastResponse<List<TeamResult>>> GetTeamsAsync(string competitionName, CancellationToken ct);

    [Get("/api/v1/football/competitions/{competitionCode}/seasons")]
    Task<ScoreCastResponse<List<SeasonResult>>> GetSeasonsAsync(string competitionCode, CancellationToken ct);

    [Get("/api/v1/football/seasons/{seasonId}/table")]
    Task<ScoreCastResponse<List<LeagueTableRow>>> GetLeagueTableAsync(long seasonId, CancellationToken ct);

    [Get("/api/v1/football/competitions/{competitionCode}/zones")]
    Task<ScoreCastResponse<List<CompetitionZoneResult>>> GetCompetitionZonesAsync(string competitionCode, CancellationToken ct);

    [Get("/api/v1/football/seasons/{seasonId}/gameweek/{gameweekNumber}")]
    Task<ScoreCastResponse<GameweekMatchesResult>> GetGameweekMatchesAsync(long seasonId, int gameweekNumber, CancellationToken ct);

    [Get("/api/v1/football/seasons/{seasonId}/player-stats")]
    Task<ScoreCastResponse<PlayerStatsResult>> GetPlayerStatsAsync(long seasonId, CancellationToken ct);
}
