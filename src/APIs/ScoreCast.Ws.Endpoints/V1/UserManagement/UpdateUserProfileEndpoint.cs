using ScoreCast.Models.V1.Requests.UserManagement;
using ScoreCast.Models.V1.Responses;
using ScoreCast.Models.V1.Responses.UserManagement;
using ScoreCast.Ws.Application.V1.UserManagement.Commands;
using ScoreCast.Ws.Endpoints.Extensions;

namespace ScoreCast.Ws.Endpoints.V1.UserManagement;

public sealed class UpdateUserProfileEndpoint : Endpoint<UpdateUserProfileRequest, ScoreCastResponse<UserProfileResult>>
{
    public override void Configure()
    {
        Put("/me");
        Group<UserManagementGroup>();
        Summary(s =>
        {
            s.Summary = "Update My Profile";
            s.Description = "Updates the authenticated user's profile";
        });
    }

    public override async Task HandleAsync(UpdateUserProfileRequest req, CancellationToken ct)
    {
        var result = await new UpdateUserProfileCommand(HttpContext.GetKeycloakUserId(), req).ExecuteAsync(ct);
        await Send.OkAsync(result, ct);
    }
}
