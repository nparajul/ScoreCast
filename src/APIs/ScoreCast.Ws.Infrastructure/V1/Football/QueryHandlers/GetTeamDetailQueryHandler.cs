using FastEndpoints;
using Microsoft.EntityFrameworkCore;
using ScoreCast.Models.V1.Responses;
using ScoreCast.Models.V1.Responses.Football;
using ScoreCast.Shared.Enums;
using ScoreCast.Shared.Types;
using ScoreCast.Ws.Application.V1.Football.Queries;
using ScoreCast.Ws.Application.V1.Interfaces;

namespace ScoreCast.Ws.Infrastructure.V1.Football.QueryHandlers;

internal sealed record GetTeamDetailQueryHandler(
    IScoreCastDbContext DbContext) : IQueryHandler<GetTeamDetailQuery, ScoreCastResponse<TeamDetailResult>>
{
    public async Task<ScoreCastResponse<TeamDetailResult>> ExecuteAsync(GetTeamDetailQuery query, CancellationToken ct)
    {
        var team = await DbContext.Teams
            .AsNoTracking()
            .Where(t => t.Id == query.TeamId && t.IsActive)
            .Select(t => new { t.Id, t.Name, t.ShortName, t.LogoUrl, t.Venue, t.Founded, t.ClubColors, t.Website })
            .FirstOrDefaultAsync(ct);

        if (team is null)
            return ScoreCastResponse<TeamDetailResult>.Error("Team not found");

        var now = ScoreCastDateTime.Now.Value;

        // Competitions this team plays in (current seasons)
        var competitions = await DbContext.SeasonTeams
            .AsNoTracking()
            .Where(st => st.TeamId == query.TeamId && st.Season.IsCurrent)
            .OrderBy(st => st.Season.Competition.Name)
            .Select(st => new TeamCompetitionSeason(
                st.SeasonId, st.Season.Competition.Name, st.Season.Competition.Code,
                st.Season.Competition.LogoUrl, st.Season.Name, st.Season.IsCurrent))
            .ToListAsync(ct);

        var currentSeasonIds = competitions.Select(c => c.SeasonId).ToList();

        // Next match
        var nextMatch = await DbContext.Matches
            .AsNoTracking()
            .Where(m => (m.HomeTeamId == query.TeamId || m.AwayTeamId == query.TeamId)
                && currentSeasonIds.Contains(m.Gameweek.SeasonId)
                && m.KickoffTime > now
                && m.Status != MatchStatus.Finished)
            .OrderBy(m => m.KickoffTime)
            .Select(m => new
            {
                m.Id, m.KickoffTime,
                IsHome = m.HomeTeamId == query.TeamId,
                OpponentName = m.HomeTeamId == query.TeamId ? m.AwayTeam.ShortName ?? m.AwayTeam.Name : m.HomeTeam.ShortName ?? m.HomeTeam.Name,
                OpponentLogo = m.HomeTeamId == query.TeamId ? m.AwayTeam.LogoUrl : m.HomeTeam.LogoUrl,
                CompName = m.Gameweek.Season.Competition.Name,
                CompLogo = m.Gameweek.Season.Competition.LogoUrl
            })
            .FirstOrDefaultAsync(ct);

        TeamNextMatch? nextMatchResult = null;
        if (nextMatch?.KickoffTime is not null)
        {
            var ko = new ScoreCastDateTime(nextMatch.KickoffTime.Value);
            var today = DateOnly.FromDateTime(now);
            var matchDate = DateOnly.FromDateTime(nextMatch.KickoffTime.Value);
            var dateLabel = matchDate == today.AddDays(1) ? "Tomorrow"
                : matchDate.ToString("MMMM d");
            var dayOfWeek = nextMatch.KickoffTime.Value.ToString("dddd");

            nextMatchResult = new TeamNextMatch(
                nextMatch.Id, ko, dateLabel, dayOfWeek,
                nextMatch.OpponentName, nextMatch.OpponentLogo, nextMatch.IsHome,
                nextMatch.CompName, nextMatch.CompLogo);
        }

        // Recent form — last 5 finished matches across all competitions
        var recentMatches = await DbContext.Matches
            .AsNoTracking()
            .Where(m => (m.HomeTeamId == query.TeamId || m.AwayTeamId == query.TeamId)
                && currentSeasonIds.Contains(m.Gameweek.SeasonId)
                && m.Status == MatchStatus.Finished)
            .OrderByDescending(m => m.KickoffTime)
            .Take(5)
            .Select(m => new
            {
                m.Id, m.KickoffTime, m.HomeScore, m.AwayScore,
                IsHome = m.HomeTeamId == query.TeamId,
                OpponentName = m.HomeTeamId == query.TeamId ? m.AwayTeam.ShortName ?? m.AwayTeam.Name : m.HomeTeam.ShortName ?? m.HomeTeam.Name,
                OpponentLogo = m.HomeTeamId == query.TeamId ? m.AwayTeam.LogoUrl : m.HomeTeam.LogoUrl,
                CompName = m.Gameweek.Season.Competition.Name,
                CompLogo = m.Gameweek.Season.Competition.LogoUrl
            })
            .ToListAsync(ct);

        var recentForm = recentMatches.Select(m =>
        {
            var teamScore = m.IsHome ? m.HomeScore : m.AwayScore;
            var oppScore = m.IsHome ? m.AwayScore : m.HomeScore;
            var result = teamScore > oppScore ? "W" : teamScore < oppScore ? "L" : "D";
            return new TeamFormMatch(
                m.Id, new ScoreCastDateTime(m.KickoffTime ?? DateTime.MinValue),
                m.OpponentName, m.OpponentLogo, m.IsHome,
                m.HomeScore, m.AwayScore, result,
                m.CompName, m.CompLogo);
        }).ToList();

        return ScoreCastResponse<TeamDetailResult>.Ok(new TeamDetailResult(
            team.Id, team.Name, team.ShortName, team.LogoUrl,
            team.Venue, team.Founded, team.ClubColors, team.Website,
            nextMatchResult, recentForm, competitions));
    }
}
