using ScoreCast.Models.V1.Requests.UserManagement;
using ScoreCast.Models.V1.Responses;
using ScoreCast.Models.V1.Responses.UserManagement;
using ScoreCast.Ws.Application.V1.UserManagement.Queries;

namespace ScoreCast.Ws.Endpoints.V1.UserManagement;

public sealed class GetRolePagesEndpoint : Endpoint<GetRolePagesRequest, ScoreCastResponse<List<PageResult>>>
{
    public override void Configure()
    {
        Get("/roles/{roleId}/pages");
        Group<UserManagementGroup>();
        Summary(s =>
        {
            s.Summary = "Get Role Pages";
            s.Description = "Returns the pages assigned to a role";
        });
    }

    public override async Task HandleAsync(GetRolePagesRequest req, CancellationToken ct)
    {
        var result = await new GetRolePagesQuery(req.RoleId).ExecuteAsync(ct);
        await Send.OkAsync(result, ct);
    }
}
