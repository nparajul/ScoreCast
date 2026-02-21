using Refit;
using ScoreCast.Models.V1.Responses;
using ScoreCast.Models.V1.Responses.Football;

namespace ScoreCast.ApiClient.V1.Apis;

public partial interface IScoreCastApiClient
{
    [Get("/api/v1/football/competitions")]
    Task<ScoreCastResponse<List<CompetitionResult>>> GetCompetitionsAsync(CancellationToken ct);

    [Get("/api/v1/football/competitions/default")]
    Task<ScoreCastResponse<CompetitionResult>> GetDefaultCompetitionAsync(CancellationToken ct);

    [Get("/api/v1/football/competitions/{competitionName}/teams")]
    Task<ScoreCastResponse<List<TeamResult>>> GetTeamsAsync(string competitionName, CancellationToken ct);

    [Get("/api/v1/football/competitions/{competitionCode}/seasons")]
    Task<ScoreCastResponse<List<SeasonResult>>> GetSeasonsAsync(string competitionCode, CancellationToken ct);

    [Get("/api/v1/football/seasons/{seasonId}/table")]
    Task<ScoreCastResponse<PointsTableResult>> GetPointsTableAsync(long seasonId, CancellationToken ct);

    [Get("/api/v1/football/seasons/{seasonId}/bracket")]
    Task<ScoreCastResponse<BracketResult>> GetBracketAsync(long seasonId, CancellationToken ct);

    [Get("/api/v1/football/competitions/{competitionCode}/zones")]
    Task<ScoreCastResponse<List<CompetitionZoneResult>>> GetCompetitionZonesAsync(string competitionCode, CancellationToken ct);

    [Get("/api/v1/football/seasons/{seasonId}/gameweek/{gameweekNumber}")]
    Task<ScoreCastResponse<GameweekMatchesResult>> GetGameweekMatchesAsync(long seasonId, int gameweekNumber, CancellationToken ct);

    [Get("/api/v1/football/seasons/{seasonId}/player-stats")]
    Task<ScoreCastResponse<PlayerStatsResult>> GetPlayerStatsAsync(long seasonId, CancellationToken ct);

    [Get("/api/v1/football/teams/{teamId}")]
    Task<ScoreCastResponse<TeamDetailResult>> GetTeamDetailAsync(long teamId, CancellationToken ct);

    [Get("/api/v1/football/teams/{teamId}/matches")]
    Task<ScoreCastResponse<TeamMatchesResult>> GetTeamMatchesAsync(long teamId, long? seasonId, CancellationToken ct);

    [Get("/api/v1/football/teams/{teamId}/squad")]
    Task<ScoreCastResponse<TeamSquadResult>> GetTeamSquadAsync(long teamId, long? seasonId, CancellationToken ct);

    [Get("/api/v1/football/teams/{teamId}/player-stats")]
    Task<ScoreCastResponse<PlayerStatsResult>> GetTeamPlayerStatsAsync(long teamId, long? seasonId, CancellationToken ct);

    [Get("/api/v1/football/teams/search")]
    Task<ScoreCastResponse<TeamSearchResult>> SearchTeamsAsync(string? q, int skip, int take, CancellationToken ct);

    [Get("/api/v1/football/matches/{matchId}")]
    Task<ScoreCastResponse<MatchPageResult>> GetMatchPageAsync(long matchId, CancellationToken ct);

    [Get("/api/v1/football/matches/{matchId}/extras")]
    Task<ScoreCastResponse<MatchExtrasResult>> GetMatchExtrasAsync(long matchId, CancellationToken ct);
}
