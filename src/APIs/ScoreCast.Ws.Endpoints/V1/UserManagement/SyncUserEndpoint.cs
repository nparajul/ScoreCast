using ScoreCast.Models.V1.Requests.UserManagement;
using ScoreCast.Models.V1.Responses;
using ScoreCast.Models.V1.Responses.UserManagement;
using ScoreCast.Ws.Application.V1.Interfaces;
using ScoreCast.Ws.Application.V1.UserManagement.Commands;

namespace ScoreCast.Ws.Endpoints.V1.UserManagement;

public sealed class SyncUserEndpoint : Endpoint<SyncUserRequest, ScoreCastResponse<SyncUserResult>>
{
    public override void Configure()
    {
        Post("/sync");
        Group<UserManagementGroup>();
        Summary(s =>
        {
            s.Summary = "Sync User";
            s.Description = "Creates or updates a user record from Firebase authentication claims";
        });
    }

    public override async Task HandleAsync(SyncUserRequest req, CancellationToken ct)
    {
        var result = await new SyncUserCommand(req).ExecuteAsync(ct);

        if (result is { Success: true, Data.IsNewUser: true })
        {
            var email = Resolve<IEmailService>();
            _ = email.SendWelcomeEmailAsync(result.Data.Email, result.Data.DisplayName ?? result.Data.UserId, ct);
        }

        await Send.OkAsync(result, ct);
    }
}
