namespace ScoreCast.Web.Components.Shared;

public partial class UserSync
{
    private bool _checked;

    private async Task EnsureUserSynced(AuthenticationState state)
    {
        if (_checked) return;
        _checked = true;

        try
        {
            var profile = await UserApi.GetMyProfileAsync();
            if (profile.Success) return;

            var user = state.User;
            await UserApi.SyncUserAsync(new Models.V1.Requests.UserManagement.SyncUserRequest
            {
                ChosenUsername = user.Identity?.Name ?? "",
                Email = user.FindFirst("email")?.Value ?? ""
            });
        }
        catch
        {

        }
    }
}
