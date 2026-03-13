using System.Net.Http.Json;
using FastEndpoints;
using Microsoft.EntityFrameworkCore;
using ScoreCast.Models.V1.Responses;
using ScoreCast.Shared.Enums;
using ScoreCast.Ws.Application.Interfaces;
using ScoreCast.Ws.Application.V1.Football.Commands;
using ScoreCast.Ws.Domain.V1.Entities.Football;
using ScoreCast.Ws.Domain.V1.Enums;
using ScoreCast.Ws.Infrastructure.V1.Football.ExternalModels;

namespace ScoreCast.Ws.Infrastructure.V1.Football.CommandHandlers;

internal sealed record SyncCompetitionCommandHandler(
    IScoreCastDbContext DbContext,
    IUnitOfWork UnitOfWork,
    IHttpClientFactory HttpClientFactory) : ICommandHandler<SyncCompetitionCommand, ScoreCastResponse>
{
    public async Task<ScoreCastResponse> ExecuteAsync(SyncCompetitionCommand command, CancellationToken ct)
    {
        var client = HttpClientFactory.CreateClient(nameof(ScoreCastHttpClient.FootballDataClient));
        var apiResponse = await client.GetFromJsonAsync<FootballDataCompetition>(
            $"competitions/{command.Request.CompetitionCode}", ct);

        if (apiResponse is null)
            return ScoreCastResponse.Error($"No data returned for competition {command.Request.CompetitionCode}");

        var country = await UpsertCountryAsync(apiResponse.Area, ct);
        var competition = await UpsertCompetitionAsync(apiResponse, country.Id, ct);
        await UpsertSeasonsAsync(apiResponse, competition.Id, country.Id, ct);

        await UnitOfWork.SaveChangesAsync(nameof(SyncCompetitionCommand), ct);
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
        FootballDataCompetition api, long countryId, CancellationToken ct)
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
                CountryId = countryId,
                LogoUrl = api.Emblem,
                ExternalId = externalId,
                Type = api.Type.Equals("CUP", StringComparison.OrdinalIgnoreCase) ? LeagueType.Cup : LeagueType.League
            };
            DbContext.Competitions.Add(competition);
        }
        else
        {
            competition.Name = api.Name;
            competition.Code = api.Code;
            competition.CountryId = countryId;
            competition.LogoUrl = api.Emblem;
            competition.Type = api.Type.Equals("CUP", StringComparison.OrdinalIgnoreCase) ? LeagueType.Cup : LeagueType.League;
        }

        return competition;
    }

    private async Task UpsertSeasonsAsync(
        FootballDataCompetition api, long competitionId, long countryId, CancellationToken ct)
    {
        var currentSeasonExternalId = api.CurrentSeason?.Id.ToString();

        var existingSeasons = await DbContext.Seasons
            .Where(s => s.CompetitionId == competitionId)
            .ToDictionaryAsync(s => s.ExternalId!, ct);

        foreach (var apiSeason in api.Seasons)
        {
            var externalId = apiSeason.Id.ToString();
            var startDate = DateOnly.Parse(apiSeason.StartDate);
            var endDate = DateOnly.Parse(apiSeason.EndDate);
            var name = $"{startDate.Year}/{endDate.Year % 100:D2}";

            long? winnerTeamId = null;
            if (apiSeason.Winner is not null)
            {
                var winnerTeam = await UpsertTeamAsync(apiSeason.Winner, countryId, ct);
                winnerTeamId = winnerTeam.Id;
            }

            if (existingSeasons.TryGetValue(externalId, out var season))
            {
                season.Name = name;
                season.StartDate = startDate;
                season.EndDate = endDate;
                season.CurrentMatchday = apiSeason.CurrentMatchday;
                season.WinnerTeamId = winnerTeamId;
                season.IsCurrent = externalId == currentSeasonExternalId;
            }
            else
            {
                DbContext.Seasons.Add(new Season
                {
                    Name = name,
                    CompetitionId = competitionId,
                    ExternalId = externalId,
                    StartDate = startDate,
                    EndDate = endDate,
                    CurrentMatchday = apiSeason.CurrentMatchday,
                    WinnerTeamId = winnerTeamId,
                    IsCurrent = externalId == currentSeasonExternalId
                });
            }
        }
    }

    private async Task<Team> UpsertTeamAsync(FootballDataTeam api, long countryId, CancellationToken ct)
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
                CountryId = countryId,
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
