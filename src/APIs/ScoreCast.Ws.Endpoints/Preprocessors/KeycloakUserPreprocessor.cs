using System.Security.Claims;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using ScoreCast.Models.V1.Requests;
using ScoreCast.Models.V1.Requests.UserManagement;
using ScoreCast.Ws.Application.Interfaces;

namespace ScoreCast.Ws.Endpoints.Preprocessors;

public sealed class KeycloakUserPreprocessor : IGlobalPreProcessor
{
    public const string KeycloakUserIdKey = "KeycloakUserId";

    public async Task PreProcessAsync(IPreProcessorContext ctx, CancellationToken ct)
    {
        if (ctx.HttpContext.User.Identity is not { IsAuthenticated: true })
            return;

        var keycloakUserId = ctx.HttpContext.User.FindFirstValue(ClaimTypes.NameIdentifier);

        if (string.IsNullOrWhiteSpace(keycloakUserId))
        {
            await ctx.HttpContext.Response.SendUnauthorizedAsync(ct);
            return;
        }

        ctx.HttpContext.Items[KeycloakUserIdKey] = keycloakUserId;

        if (ctx.Request is ScoreCastRequest request)
        {
            if (request is SyncUserRequest syncRequest)
                syncRequest.KeycloakUserId = keycloakUserId;

            var dbContext = ctx.HttpContext.RequestServices.GetRequiredService<IScoreCastDbContext>();
            var userId = await dbContext.UserMasters
                .AsNoTracking()
                .Where(u => u.KeycloakUserId == keycloakUserId)
                .Select(u => u.UserId)
                .FirstOrDefaultAsync(ct);

            request.UserId = userId;
        }
    }
}
