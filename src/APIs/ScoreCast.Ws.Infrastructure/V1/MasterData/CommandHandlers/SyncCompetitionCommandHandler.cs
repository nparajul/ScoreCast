using System.Net.Http.Json;
using FastEndpoints;
using Microsoft.EntityFrameworkCore;
using ScoreCast.Models.V1.Responses;
using ScoreCast.Shared.Constants;
using ScoreCast.Shared.Exceptions;
using ScoreCast.Ws.Application.V1.MasterData.Commands;
using ScoreCast.Ws.Domain.V1.Entities.Football;
using ScoreCast.Shared.Enums;
using ScoreCast.Ws.Application;
using ScoreCast.Ws.Application.V1.Interfaces;
using ScoreCast.Ws.Infrastructure.V1.MasterData.ExternalModels;

namespace ScoreCast.Ws.Infrastructure.V1.MasterData.CommandHandlers;

internal sealed record SyncCompetitionCommandHandler(
    IScoreCastDbContext DbContext,
    IUnitOfWork UnitOfWork,
    IHttpClientFactory HttpClientFactory) : ICommandHandler<SyncCompetitionCommand, ScoreCastResponse>
{
    public async Task<ScoreCastResponse> ExecuteAsync(SyncCompetitionCommand command, CancellationToken ct)
    {
        try
        {
            return await ExecuteCoreAsync(command, ct);
        }
        catch (ScoreCastException ex)
        {
            return ScoreCastResponse.Error(ex.Message);
        }
    }

    private async Task<ScoreCastResponse> ExecuteCoreAsync(SyncCompetitionCommand command, CancellationToken ct)
    {
        var client = HttpClientFactory.CreateClient(nameof(ScoreCastHttpClient.FootballDataClient));

        FootballDataCompetition? apiResponse;
        try
        {
            apiResponse = await client.GetFromJsonAsync<FootballDataCompetition>(
                string.Format(FootballDataApi.Routes.Competition, command.Request.CompetitionCode), ct);
        }
        catch (Exception ex)
        {
            throw new ScoreCastException($"Football-data.org competition API failed for {command.Request.CompetitionCode}", ex);
        }

        if (apiResponse is null)
            return ScoreCastResponse.Error($"No data returned for competition {command.Request.CompetitionCode}");

        var country = await UpsertCountryAsync(apiResponse.Area, ct);
        var competition = await UpsertCompetitionAsync(apiResponse, country, ct);
        await UpsertSeasonsAsync(apiResponse, competition, country, ct);

        await UnitOfWork.SaveChangesAsync(command.Request.AppName ?? nameof(SyncCompetitionCommand), ct);
        return ScoreCastResponse.Ok($"Synced {apiResponse.Name} with {apiResponse.Seasons.Count} seasons");
    }

    private async Task<Country> UpsertCountryAsync(FootballDataArea area, CancellationToken ct)
    {
        var externalId = area.Id.ToString();
        var country = await DbContext.Countries
            .FirstOrDefaultAsync(c => c.ExternalId == externalId, ct);

        if (country is null)
        {
            country = new Country
            {
                Name = area.Name,
                Code = area.Code,
                ExternalId = externalId,
                FlagUrl = area.Flag
            };
            DbContext.Countries.Add(country);
        }
        else
        {
            country.Name = area.Name;
            country.Code = area.Code;
            country.FlagUrl = area.Flag;
        }

        return country;
    }

    private async Task<Competition> UpsertCompetitionAsync(
        FootballDataCompetition api, Country country, CancellationToken ct)
    {
        var externalId = api.Id.ToString();
        var competition = await DbContext.Competitions
            .FirstOrDefaultAsync(c => c.ExternalId == externalId, ct);

        if (competition is null)
        {
            competition = new Competition
            {
                Name = api.Name,
                Code = api.Code,
                Country = country,
                LogoUrl = api.Emblem,
                ExternalId = externalId
            };
            DbContext.Competitions.Add(competition);
        }
        else
        {
            competition.Name = api.Name;
            competition.Code = api.Code;
            competition.CountryId = country.Id;
            competition.LogoUrl = api.Emblem;
        }

        return competition;
    }

    private async Task UpsertSeasonsAsync(
        FootballDataCompetition api, Competition competition, Country country, CancellationToken ct)
    {
        var currentSeasonExternalId = api.CurrentSeason?.Id.ToString();

        var existingSeasons = await DbContext.Seasons
            .Where(s => s.CompetitionId == competition.Id)
            .ToDictionaryAsync(s => s.ExternalId!, ct);

        var teamCache = new Dictionary<string, Team>();

        foreach (var apiSeason in api.Seasons)
        {
            var externalId = apiSeason.Id.ToString();
            var startDate = DateOnly.Parse(apiSeason.StartDate);
            var endDate = DateOnly.Parse(apiSeason.EndDate);
            var name = startDate.Year == endDate.Year
                ? $"{startDate.Year}"
                : $"{startDate.Year}/{endDate.Year % 100:D2}";

            Team? winnerTeam = null;
            if (apiSeason.Winner is not null)
            {
                var teamExtId = apiSeason.Winner.Id.ToString();
                if (!teamCache.TryGetValue(teamExtId, out winnerTeam))
                {
                    winnerTeam = await UpsertTeamAsync(apiSeason.Winner, country, ct);
                    teamCache[teamExtId] = winnerTeam;
                }
            }

            if (existingSeasons.TryGetValue(externalId, out var season))
            {
                season.Name = name;
                season.StartDate = startDate;
                season.EndDate = endDate;
                season.CurrentMatchday = apiSeason.CurrentMatchday;
                season.WinnerTeam = winnerTeam;
                season.IsCurrent = externalId == currentSeasonExternalId;
            }
            else
            {
                DbContext.Seasons.Add(new Season
                {
                    Name = name,
                    Competition = competition,
                    ExternalId = externalId,
                    StartDate = startDate,
                    EndDate = endDate,
                    CurrentMatchday = apiSeason.CurrentMatchday,
                    WinnerTeam = winnerTeam,
                    IsCurrent = externalId == currentSeasonExternalId
                });
            }
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

}
