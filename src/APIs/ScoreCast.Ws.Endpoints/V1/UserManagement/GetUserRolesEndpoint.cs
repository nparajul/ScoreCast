using ScoreCast.Models.V1.Responses;
using ScoreCast.Models.V1.Responses.UserManagement;
using ScoreCast.Ws.Application.V1.UserManagement.Queries;
using ScoreCast.Ws.Endpoints.Extensions;

namespace ScoreCast.Ws.Endpoints.V1.UserManagement;

public sealed class GetUserRolesEndpoint : EndpointWithoutRequest<ScoreCastResponse<List<RoleResult>>>
{
    public override void Configure()
    {
        Get("/me/roles");
        Group<UserManagementGroup>();
        Summary(s =>
        {
            s.Summary = "Get My Roles";
            s.Description = "Returns the authenticated user's assigned roles";
        });
    }

    public override async Task HandleAsync(CancellationToken ct)
    {
        var result = await new GetUserRolesQuery(HttpContext.GetFirebaseUserId()).ExecuteAsync(ct);
        await Send.OkAsync(result, ct);
    }
}
