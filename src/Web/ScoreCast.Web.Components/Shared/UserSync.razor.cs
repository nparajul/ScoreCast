using ScoreCast.Models.V1.Requests.UserManagement;
using ScoreCast.Models.V1.Responses;
using ScoreCast.Models.V1.Responses.UserManagement;
using ScoreCast.Web.Components.Helpers;
using ScoreCast.Web.Components.Reusable;

namespace ScoreCast.Web.Components.Shared;

public partial class UserSync
{
    private bool _checked;
    [Inject] public required ILoadingService Loading { get; set; }
    [Inject] public required IAlertService Alert { get; set; }
    [Inject] public required IDialogService DialogService { get; set; }

    private async Task EnsureUserSynced(AuthenticationState state)
    {
        if (_checked) return;
        _checked = true;

        try
        {
            ScoreCastResponse<UserProfileResult>? profile = null;
            await Loading.While(async () => profile = await UserApi.GetMyProfileAsync(CancellationToken.None));
            if (profile is not null && profile.Success) return;

            var user = state.User;
            await Loading.While(async () => await UserApi.SyncUserAsync(new SyncUserRequest
            {
                ChosenUsername = user.Identity?.Name ?? "",
                Email = user.FindFirst("email")?.Value ?? ""
            }, CancellationToken.None));

            await ShowWelcomeDialog();
        }
        catch (Exception ex)
        {
            await Alert.ShowDialogForException(ex, Severity.Error);
        }
    }

    private async Task ShowWelcomeDialog()
    {
        var options = new DialogOptions { CloseOnEscapeKey = false, BackdropClick = false, MaxWidth = MaxWidth.Small, FullWidth = true };
        var dialog = await DialogService.ShowAsync<WelcomeDialog>("Welcome", options);
        var result = await dialog.Result;

        if (result is { Canceled: false, Data: string displayName } && !string.IsNullOrWhiteSpace(displayName))
        {
            await Loading.While(async () => await UserApi.UpdateMyProfileAsync(
                new UpdateUserProfileRequest { DisplayName = displayName }, CancellationToken.None));
        }
    }
}
