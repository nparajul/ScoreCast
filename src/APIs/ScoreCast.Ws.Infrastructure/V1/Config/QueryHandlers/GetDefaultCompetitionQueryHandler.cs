using Microsoft.EntityFrameworkCore;
using ScoreCast.Models.V1.Responses;
using ScoreCast.Models.V1.Responses.Football;
using ScoreCast.Shared.Constants;
using ScoreCast.Shared.Enums;
using ScoreCast.Ws.Application.V1.Config.Queries;
using ScoreCast.Ws.Application.V1.Interfaces;

namespace ScoreCast.Ws.Infrastructure.V1.Config.QueryHandlers;

internal sealed record GetDefaultCompetitionQueryHandler(
    IScoreCastDbContext DbContext) : ICommandHandler<GetDefaultCompetitionQuery, ScoreCastResponse<CompetitionResult>>
{
    public async Task<ScoreCastResponse<CompetitionResult>> ExecuteAsync(GetDefaultCompetitionQuery query, CancellationToken ct)
    {
        var code = CompetitionCodes.PremierLeague;

        var config = await new GetAppConfigQuery(AppConfigKeys.DefaultCompetition).ExecuteAsync(ct);
        if (config is not null)
        {
            var parsed = config.RootElement.GetProperty("competitionCode").GetString();
            if (!string.IsNullOrEmpty(parsed))
                code = parsed;
        }

        var competition = await DbContext.Competitions
            .AsNoTracking()
            .Where(c => c.Code == code && c.IsActive)
            .Select(c => new CompetitionResult(
                c.Id, c.Name, c.Code, c.LogoUrl, c.Country.Name, c.Country.FlagUrl,
                DbContext.ExternalMappings
                    .Where(m => m.EntityType == EntityType.Competition && m.EntityId == c.Id)
                    .Select(m => m.Source.ToString())
                    .ToList()))
            .FirstOrDefaultAsync(ct);

        return competition is null
            ? ScoreCastResponse<CompetitionResult>.Error($"Competition '{code}' not found")
            : ScoreCastResponse<CompetitionResult>.Ok(competition);
    }
}
