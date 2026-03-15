using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using ScoreCast.Ws.Application.V1.Config.Queries;
using ScoreCast.Ws.Application.V1.Interfaces;

namespace ScoreCast.Ws.Infrastructure.V1.Config.QueryHandlers;

internal sealed record GetAppConfigQueryHandler(
    IScoreCastDbContext DbContext) : ICommandHandler<GetAppConfigQuery, JsonDocument?>
{
    public async Task<JsonDocument?> ExecuteAsync(GetAppConfigQuery query, CancellationToken ct)
    {
        return await DbContext.AppConfigs
            .AsNoTracking()
            .Where(c => c.Key == query.Key)
            .Select(c => c.Value)
            .FirstOrDefaultAsync(ct);
    }
}
