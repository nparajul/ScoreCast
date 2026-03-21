using System.Net.Http.Json;
using FastEndpoints;
using Microsoft.EntityFrameworkCore;
using ScoreCast.Models.V1.Responses;
using ScoreCast.Shared.Constants;
using ScoreCast.Shared.Enums;
using ScoreCast.Shared.Exceptions;
using ScoreCast.Ws.Application;
using ScoreCast.Ws.Application.V1.MasterData.Commands;
using ScoreCast.Ws.Application.V1.Interfaces;
using ScoreCast.Ws.Domain.V1.Entities;
using ScoreCast.Ws.Domain.V1.Entities.Football;
using ScoreCast.Ws.Infrastructure.V1.MasterData.ExternalModels;

namespace ScoreCast.Ws.Infrastructure.V1.MasterData.CommandHandlers;

internal sealed record SyncTeamsCommandHandler(
    IScoreCastDbContext DbContext,
    IUnitOfWork UnitOfWork,
    IHttpClientFactory HttpClientFactory) : ICommandHandler<SyncTeamsCommand, ScoreCastResponse>
{
    public async Task<ScoreCastResponse> ExecuteAsync(SyncTeamsCommand command, CancellationToken ct)
    {
        var competition = await DbContext.Competitions
            .Include(c => c.Country)
            .FirstOrDefaultAsync(c => c.Code == command.Request.CompetitionCode, ct);

        if (competition is null)
            return ScoreCastResponse.Error($"Competition {command.Request.CompetitionCode} not found. Sync the competition first.");

        var currentSeason = await DbContext.Seasons
            .FirstOrDefaultAsync(s => s.CompetitionId == competition.Id && s.IsCurrent, ct);

        var isPremierLeague = command.Request.CompetitionCode == CompetitionCodes.PremierLeague;

        await using var transaction = await UnitOfWork.BeginTransactionAsync(ct);
        try
        {
            List<Team> upsertedTeams;
            var playerCount = 0;

            if (isPremierLeague)
                upsertedTeams = await SyncTeamsFromPulseAsync(competition.Country, currentSeason, ct);
            else
                upsertedTeams = [];

            // Fallback to football-data.org if Pulse returned nothing, or for non-PL
            if (upsertedTeams.Count == 0)
                (upsertedTeams, playerCount) = await SyncTeamsFromFootballDataAsync(command.Request.CompetitionCode, competition.Country, currentSeason, ct);

            if (currentSeason is not null)
                await LinkTeamsToSeasonAsync(currentSeason, upsertedTeams, ct);

            await UnitOfWork.SaveChangesAsync(command.Request.AppName ?? nameof(SyncTeamsCommand), ct);
            await transaction.CommitAsync(ct);
            return ScoreCastResponse.Ok($"Synced {upsertedTeams.Count} teams and {playerCount} players for {competition.Name}");
        }
        catch (ScoreCastException ex)
        {
            await transaction.RollbackAsync(ct);
            return ScoreCastResponse.Error(ex.Message);
        }
    }

    private async Task<List<Team>> SyncTeamsFromPulseAsync(Country country, Season? currentSeason, CancellationToken ct)
    {
        if (currentSeason is null) return [];

        var pulseCompSeasonId = await DbContext.ExternalMappings
            .Where(m => m.Source == ExternalSource.Pulse && m.EntityType == EntityType.Season
                        && m.EntityId == currentSeason.Id)
            .Select(m => m.ExternalCode)
            .FirstOrDefaultAsync(ct);

        if (pulseCompSeasonId is null) return [];

        var pulseClient = HttpClientFactory.CreateClient(nameof(ScoreCastHttpClient.PulseClient));

        List<PulseTeamResponse>? pulseTeams;
        try
        {
            pulseTeams = await pulseClient.GetFromJsonAsync<List<PulseTeamResponse>>(
                string.Format(PulseApi.Routes.TeamsByCompSeason, pulseCompSeasonId), ct);
        }
        catch (Exception ex)
        {
            throw new ScoreCastException($"Pulse teams API failed for compSeason {pulseCompSeasonId}", ex);
        }

        if (pulseTeams is not { Count: > 0 }) return [];

        var existingPulseMappings = await DbContext.ExternalMappings
            .Where(m => m.Source == ExternalSource.Pulse && m.EntityType == EntityType.Team)
            .ToDictionaryAsync(m => m.ExternalCode, m => m.EntityId, ct);

        var mappedTeamIds = existingPulseMappings.Values.ToHashSet();
        var teamsById = await DbContext.Teams
            .Where(t => mappedTeamIds.Contains(t.Id))
            .ToDictionaryAsync(t => t.Id, ct);
        var allTeams = await DbContext.Teams.ToListAsync(ct);
        var upsertedTeams = new List<Team>();

        foreach (var pt in pulseTeams)
        {
            var pulseId = pt.Id.ToString();
            Team team;

            if (existingPulseMappings.TryGetValue(pulseId, out var existingTeamId) && teamsById.TryGetValue(existingTeamId, out var existingTeam))
            {
                existingTeam.ShortName = pt.ShortName;
                existingTeam.Venue = pt.Grounds?.FirstOrDefault()?.Name;
                team = existingTeam;
            }
            else
            {
                // Match existing team by name (Pulse "Arsenal" → DB "Arsenal FC")
                var matched = allTeams.FirstOrDefault(t => t.Name.StartsWith(pt.Name, StringComparison.OrdinalIgnoreCase))
                    ?? allTeams.FirstOrDefault(t => t.ShortName != null && t.ShortName.Equals(pt.ShortName, StringComparison.OrdinalIgnoreCase));

                if (matched is not null)
                {
                    matched.ShortName = pt.ShortName;
                    matched.Venue = pt.Grounds?.FirstOrDefault()?.Name;
                    team = matched;
                }
                else
                {
                    team = new Team
                    {
                        Name = pt.Name,
                        ShortName = pt.ShortName,
                        Country = country,
                        Venue = pt.Grounds?.FirstOrDefault()?.Name
                    };
                    DbContext.Teams.Add(team);
                }

                if (!existingPulseMappings.ContainsKey(pulseId))
                {
                    DbContext.ExternalMappings.Add(new ExternalMapping
                    {
                        EntityType = EntityType.Team,
                        Source = ExternalSource.Pulse,
                        ExternalCode = pulseId,
                        EntityId = team.Id
                    });
                }
            }

            upsertedTeams.Add(team);
        }

        return upsertedTeams;
    }

    private async Task<(List<Team> Teams, int PlayerCount)> SyncTeamsFromFootballDataAsync(
        string competitionCode, Country country, Season? currentSeason, CancellationToken ct)
    {
        var client = HttpClientFactory.CreateClient(nameof(ScoreCastHttpClient.FootballDataClient));

        FootballDataTeamsResponse? apiResponse;
        try
        {
            apiResponse = await client.GetFromJsonAsync<FootballDataTeamsResponse>(
                string.Format(FootballDataApi.Routes.Teams, competitionCode), ct);
        }
        catch (Exception ex)
        {
            throw new ScoreCastException($"Football-data.org teams API failed for {competitionCode}", ex);
        }

        if (apiResponse?.Teams is null or { Count: 0 })
            return ([], 0);

        var upsertedTeams = new List<Team>();
        var playerCount = 0;

        var playerCache = await DbContext.Players
            .Where(p => p.ExternalId != null)
            .ToDictionaryAsync(p => p.ExternalId!, ct);

        var existingTeamPlayers = currentSeason is not null
            ? (await DbContext.TeamPlayers
                .Where(tp => tp.SeasonId == currentSeason.Id)
                .Select(tp => new { tp.TeamId, tp.PlayerId })
                .ToListAsync(ct))
                .Select(x => (x.TeamId, x.PlayerId))
                .ToHashSet()
            : [];

        foreach (var apiTeam in apiResponse.Teams)
        {
            var team = await UpsertFdTeamAsync(apiTeam, country, ct);
            upsertedTeams.Add(team);

            if (apiTeam.Squad is { Count: > 0 } && currentSeason is not null)
                playerCount += UpsertPlayers(apiTeam.Squad, team, currentSeason, playerCache, existingTeamPlayers);
        }

        return (upsertedTeams, playerCount);
    }

    private async Task LinkTeamsToSeasonAsync(Season season, List<Team> teams, CancellationToken ct)
    {
        var existingTeamIds = await DbContext.SeasonTeams
            .Where(st => st.SeasonId == season.Id)
            .Select(st => st.TeamId)
            .ToHashSetAsync(ct);

        foreach (var team in teams.Where(t => t.Id == 0 || !existingTeamIds.Contains(t.Id)))
        {
            DbContext.SeasonTeams.Add(new SeasonTeam
            {
                Season = season,
                Team = team
            });
        }
    }

    private async Task<Team> UpsertFdTeamAsync(FootballDataTeam api, Country country, CancellationToken ct)
    {
        var externalId = api.Id.ToString();
        var team = await DbContext.Teams
            .FirstOrDefaultAsync(t => t.ExternalId == externalId, ct);

        var coach = await UpsertCoachAsync(api.Coach, ct);

        if (team is null)
        {
            team = new Team
            {
                Name = api.Name,
                ShortName = api.ShortName,
                LogoUrl = api.Crest,
                ExternalId = externalId,
                Country = country,
                Founded = api.Founded,
                Venue = api.Venue,
                ClubColors = api.ClubColors,
                Website = api.Website,
                Coach = coach
            };
            DbContext.Teams.Add(team);
        }
        else
        {
            team.Name = api.Name;
            team.ShortName = api.ShortName;
            team.LogoUrl = api.Crest;
            team.Founded = api.Founded;
            team.Venue = api.Venue;
            team.ClubColors = api.ClubColors;
            team.Website = api.Website;
            team.Coach = coach;
        }

        return team;
    }

    private async Task<Coach?> UpsertCoachAsync(FootballDataCoach? api, CancellationToken ct)
    {
        if (api?.Id is null || api.Name is null) return null;
        var extId = api.Id.Value.ToString();
        var coach = await DbContext.Coaches.FirstOrDefaultAsync(c => c.ExternalId == extId, ct);
        if (coach is null)
        {
            coach = new Coach { Name = api.Name, ExternalId = extId };
            DbContext.Coaches.Add(coach);
        }
        coach.Name = api.Name;
        coach.Nationality = api.Nationality;
        coach.DateOfBirth = DateOnly.TryParse(api.DateOfBirth, out var dob) ? dob : null;
        return coach;
    }

    private int UpsertPlayers(
        List<FootballDataPlayer> squad, Team team, Season season,
        Dictionary<string, Player> playerCache, HashSet<(long TeamId, long PlayerId)> existingTeamPlayers)
    {
        var count = 0;
        foreach (var apiPlayer in squad)
        {
            var externalId = apiPlayer.Id.ToString();

            if (!playerCache.TryGetValue(externalId, out var player))
            {
                player = new Player
                {
                    Name = apiPlayer.Name,
                    Position = apiPlayer.Position,
                    DateOfBirth = DateOnly.TryParse(apiPlayer.DateOfBirth, out var dob) ? dob : null,
                    Nationality = apiPlayer.Nationality,
                    ExternalId = externalId
                };
                DbContext.Players.Add(player);
                playerCache[externalId] = player;
            }
            else
            {
                player.Name = apiPlayer.Name;
                player.Position = apiPlayer.Position;
                player.DateOfBirth = DateOnly.TryParse(apiPlayer.DateOfBirth, out var dob) ? dob : null;
                player.Nationality = apiPlayer.Nationality;
            }

            var key = (team.Id, player.Id);
            if (team.Id == 0 || player.Id == 0 || existingTeamPlayers.Add(key))
            {
                DbContext.TeamPlayers.Add(new TeamPlayer
                {
                    Team = team,
                    Player = player,
                    Season = season
                });
                count++;
            }
        }

        return count;
    }
}
