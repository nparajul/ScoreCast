using ScoreCast.Models.V1.Responses;
using ScoreCast.Models.V1.Responses.UserManagement;
using ScoreCast.Ws.Application.V1.UserManagement.Queries;
using ScoreCast.Ws.Endpoints.Extensions;

namespace ScoreCast.Ws.Endpoints.V1.UserManagement;

public sealed class GetUserProfileEndpoint : EndpointWithoutRequest<ScoreCastResponse<UserProfileResult>>
{
    public override void Configure()
    {
        Get("/me");
        Group<UserManagementGroup>();
        Summary(s =>
        {
            s.Summary = "Get My Profile";
            s.Description = "Returns the authenticated user's profile";
        });
    }

    public override async Task HandleAsync(CancellationToken ct)
    {
        var result = await new GetUserProfileQuery(HttpContext.GetFirebaseUserId()).ExecuteAsync(ct);
        await Send.OkAsync(result, ct);
    }
}
