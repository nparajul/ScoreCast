using ScoreCast.Models.V1.Requests.UserManagement;
using ScoreCast.Models.V1.Responses;
using ScoreCast.Models.V1.Responses.UserManagement;
using ScoreCast.Ws.Application.V1.UserManagement.Commands;
using ScoreCast.Ws.Endpoints.Extensions;

namespace ScoreCast.Ws.Endpoints.V1.UserManagement;

public sealed class SetUsernameEndpoint : Endpoint<SetUsernameRequest, ScoreCastResponse<UserProfileResult>>
{
    public override void Configure()
    {
        Put("/me/username");
        Group<UserManagementGroup>();
        Summary(s =>
        {
            s.Summary = "Set Username";
            s.Description = "Sets a unique username for the authenticated user";
        });
    }

    public override async Task HandleAsync(SetUsernameRequest req, CancellationToken ct)
    {
        var result = await new SetUsernameCommand(HttpContext.GetFirebaseUserId(), req).ExecuteAsync(ct);
        await Send.OkAsync(result, ct);
    }
}
