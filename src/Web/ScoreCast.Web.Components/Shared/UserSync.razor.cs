using ScoreCast.Models.V1.Requests.UserManagement;
using ScoreCast.Models.V1.Responses;
using ScoreCast.Models.V1.Responses.UserManagement;
using ScoreCast.Web.Components.Helpers;
using ScoreCast.Web.Components.Reusable;

namespace ScoreCast.Web.Components.Shared;

public partial class UserSync : IDisposable
{
    private bool _synced;
    private bool _subscribed;
    private const string _appName = "SIGN UP";
    [Inject] public required ILoadingService Loading { get; set; }
    [Inject] public required IAlertService Alert { get; set; }
    [Inject] public required IDialogService DialogService { get; set; }
    [Inject] public required IRoleNavigationService RoleNav { get; set; }
    [Inject] public required AuthenticationStateProvider AuthStateProvider { get; set; }
    [CascadingParameter] private Task<AuthenticationState>? AuthStateTask { get; set; }

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (!_subscribed)
        {
            _subscribed = true;
            AuthStateProvider.AuthenticationStateChanged += OnAuthStateChanged;
        }

        if (!firstRender || _synced || AuthStateTask is null) return;

        var state = await AuthStateTask;
        if (IsVerifiedUser(state))
            await EnsureUserSynced(state);
    }

    private async void OnAuthStateChanged(Task<AuthenticationState> task)
    {
        var state = await task;
        if (IsVerifiedUser(state))
        {
            _synced = false;
            await InvokeAsync(async () => await EnsureUserSynced(state));
        }
    }

    private static bool IsVerifiedUser(AuthenticationState state) =>
        state.User.Identity?.IsAuthenticated == true
        && state.User.FindFirst("email_verified")?.Value == "true";

    private async Task EnsureUserSynced(AuthenticationState state)
    {
        if (_synced) return;
        _synced = true;

        try
        {
            ScoreCastResponse<UserProfileResult>? profile = null;
            await Loading.While(async () =>
            {
                for (var i = 1; i <= 3; i++)
                {
                    try
                    {
                        profile = await Api.GetMyProfileAsync(CancellationToken.None);
                        return;
                    }
                    catch (HttpRequestException) when (i < 3)
                    {
                        await Task.Delay(2000);
                    }
                    catch (TaskCanceledException) when (i < 3)
                    {
                        await Task.Delay(2000);
                    }
                }
            }, "Connecting to server...");

            if (profile is { Success: true })
            {
                await RoleNav.LoadRolesAsync();
                return;
            }

            var user = state.User;
            var email = user.FindFirst(System.Security.Claims.ClaimTypes.Email)?.Value ?? "";
            var displayName = user.Identity?.Name;
            var isGoogle = user.FindFirst("is_google_user")?.Value == "true";

            var syncResult = await Api.SyncUserAsync(new SyncUserRequest
            {
                Email = email,
                DisplayName = isGoogle ? null : displayName,
                IsGoogleSignIn = isGoogle,
                AppName = _appName
            }, CancellationToken.None);

            if (syncResult is { Success: true, Data.IsNewUser: true })
            {
                await ShowWelcomeDialog(syncResult.Data.DisplayName ?? email);
            }

            await RoleNav.LoadRolesAsync();
        }
        catch (Exception ex)
        {
            await Alert.ShowDialogForException(ex, Severity.Error);
        }
    }

    private async Task ShowWelcomeDialog(string username)
    {
        var parameters = new DialogParameters<WelcomeDialog> { { x => x.Username, username } };
        var options = new DialogOptions { CloseOnEscapeKey = false, BackdropClick = false, NoHeader = true, MaxWidth = MaxWidth.ExtraSmall, FullWidth = true };
        var dialog = await DialogService.ShowAsync<WelcomeDialog>(string.Empty, parameters, options);
        var result = await dialog.Result;

        if (result is { Canceled: false, Data: WelcomeDialogResult data })
        {
            var update = new UpdateUserProfileRequest();
            if (!string.IsNullOrWhiteSpace(data.FavoriteTeam)) update.FavoriteTeam = data.FavoriteTeam;
            if (!string.IsNullOrWhiteSpace(data.DisplayName)) update.DisplayName = data.DisplayName;

            if (update.FavoriteTeam is not null || update.DisplayName is not null)
            {
                await Loading.While(async () => await Api.UpdateMyProfileAsync(update, CancellationToken.None));
            }
        }
    }

    public void Dispose() => AuthStateProvider.AuthenticationStateChanged -= OnAuthStateChanged;
}
