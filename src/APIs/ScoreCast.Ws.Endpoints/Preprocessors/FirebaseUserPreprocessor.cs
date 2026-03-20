using System.Security.Claims;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using ScoreCast.Models.V1.Requests;
using ScoreCast.Models.V1.Requests.UserManagement;
using ScoreCast.Shared.Constants;
using ScoreCast.Ws.Application.V1.Interfaces;

namespace ScoreCast.Ws.Endpoints.Preprocessors;

public sealed class FirebaseUserPreprocessor : IGlobalPreProcessor
{
    public const string FirebaseUserIdKey = "FirebaseUserId";
    public const string ScoreCastUserIdKey = "ScoreCastUserId";

    public async Task PreProcessAsync(IPreProcessorContext ctx, CancellationToken ct)
    {
        if (ctx.HttpContext.User.Identity is not { IsAuthenticated: true, AuthenticationType: var authType })
            return;

        if (authType == ApiKeyAuth.SchemeName)
        {
            if (ctx.Request is ScoreCastRequest apiKeyRequest)
                apiKeyRequest.UserId = ctx.HttpContext.User.FindFirstValue(ClaimTypes.Name);
            return;
        }

        var firebaseUserId = ctx.HttpContext.User.FindFirstValue(ClaimTypes.NameIdentifier)
                             ?? ctx.HttpContext.User.FindFirstValue("user_id");

        if (string.IsNullOrWhiteSpace(firebaseUserId))
        {
            await ctx.HttpContext.Response.SendUnauthorizedAsync(ct);
            return;
        }

        ctx.HttpContext.Items[FirebaseUserIdKey] = firebaseUserId;

        if (ctx.Request is ScoreCastRequest request)
        {
            if (request is SyncUserRequest syncRequest)
                syncRequest.FirebaseUid = firebaseUserId;

            var dbContext = ctx.HttpContext.RequestServices.GetRequiredService<IScoreCastDbContext>();
            var userId = await dbContext.UserMasters
                .AsNoTracking()
                .Where(u => u.FirebaseUid == firebaseUserId)
                .Select(u => u.UserId)
                .FirstOrDefaultAsync(ct);

            request.UserId = userId;

            if (!string.IsNullOrEmpty(userId))
                ctx.HttpContext.Items[ScoreCastUserIdKey] = userId;
        }
    }
}
