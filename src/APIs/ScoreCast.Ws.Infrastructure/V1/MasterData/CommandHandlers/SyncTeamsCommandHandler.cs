using System.Net.Http.Json;
using FastEndpoints;
using Microsoft.EntityFrameworkCore;
using ScoreCast.Models.V1.Responses;
using ScoreCast.Shared.Constants;
using ScoreCast.Shared.Enums;
using ScoreCast.Ws.Application;
using ScoreCast.Ws.Application.V1.MasterData.Commands;
using ScoreCast.Ws.Application.V1.Interfaces;
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
        var client = HttpClientFactory.CreateClient(nameof(ScoreCastHttpClient.FootballDataClient));

        FootballDataTeamsResponse? apiResponse;
        try
        {
            apiResponse = await client.GetFromJsonAsync<FootballDataTeamsResponse>(
                string.Format(FootballDataApi.Routes.Teams, command.Request.CompetitionCode), ct);
        }
        catch (Exception ex)
        {
            return ScoreCastResponse.Error($"Failed to fetch teams for {command.Request.CompetitionCode}: {ex.Message}");
        }

        if (apiResponse?.Teams is null or { Count: 0 })
            return ScoreCastResponse.Error($"No teams returned for {command.Request.CompetitionCode}");

        var competition = await DbContext.Competitions
            .Include(c => c.Country)
            .FirstOrDefaultAsync(c => c.Code == command.Request.CompetitionCode, ct);

        if (competition is null)
            return ScoreCastResponse.Error($"Competition {command.Request.CompetitionCode} not found. Sync the competition first.");

        var currentSeason = await DbContext.Seasons
            .FirstOrDefaultAsync(s => s.CompetitionId == competition.Id && s.IsCurrent, ct);

        await using var transaction = await UnitOfWork.BeginTransactionAsync(ct);
        try
        {
            var upsertedTeams = new List<Team>();
            var playerCount = 0;
            foreach (var apiTeam in apiResponse.Teams)
            {
                var team = await UpsertTeamAsync(apiTeam, competition.Country, ct);
                upsertedTeams.Add(team);

                if (apiTeam.Squad is { Count: > 0 } && currentSeason is not null)
                    playerCount += await UpsertPlayersAsync(apiTeam.Squad, team, currentSeason, ct);
            }

            if (currentSeason is not null)
                await LinkTeamsToSeasonAsync(currentSeason, upsertedTeams, ct);

            await UnitOfWork.SaveChangesAsync(command.Request.AppName ?? nameof(SyncTeamsCommand), ct);
            await transaction.CommitAsync(ct);
            return ScoreCastResponse.Ok($"Synced {upsertedTeams.Count} teams and {playerCount} players for {competition.Name}");
        }
        catch (Exception ex)
        {
            await transaction.RollbackAsync(ct);
            return ScoreCastResponse.Error($"Failed to sync teams for {command.Request.CompetitionCode}: {ex.Message}");
        }
    }

    private async Task<Team> UpsertTeamAsync(FootballDataTeam api, Country country, CancellationToken ct)
    {
        var externalId = api.Id.ToString();
        var team = await DbContext.Teams
            .FirstOrDefaultAsync(t => t.ExternalId == externalId, ct);

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
                Website = api.Website
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
        }

        return team;
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

    private async Task<int> UpsertPlayersAsync(
        List<FootballDataPlayer> squad, Team team, Season season, CancellationToken ct)
    {
        var count = 0;
        foreach (var apiPlayer in squad)
        {
            var externalId = apiPlayer.Id.ToString();
            var player = await DbContext.Players
                .FirstOrDefaultAsync(p => p.ExternalId == externalId, ct);

            if (player is null)
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
            }
            else
            {
                player.Name = apiPlayer.Name;
                player.Position = apiPlayer.Position;
                player.DateOfBirth = DateOnly.TryParse(apiPlayer.DateOfBirth, out var dob) ? dob : null;
                player.Nationality = apiPlayer.Nationality;
            }

            var exists = await DbContext.TeamPlayers
                .AnyAsync(tp => tp.Player == player && tp.Team == team && tp.Season == season, ct);

            if (!exists)
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
