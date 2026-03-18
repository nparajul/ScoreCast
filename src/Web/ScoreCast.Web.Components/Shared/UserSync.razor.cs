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
        if (state.User.Identity?.IsAuthenticated == true)
            await EnsureUserSynced(state);
    }

    private async void OnAuthStateChanged(Task<AuthenticationState> task)
    {
        var state = await task;
        if (state.User.Identity?.IsAuthenticated == true)
        {
            _synced = false;
            await InvokeAsync(async () => await EnsureUserSynced(state));
        }
    }

    private async Task EnsureUserSynced(AuthenticationState state)
    {
        if (_synced) return;
        _synced = true;

        try
        {
            ScoreCastResponse<UserProfileResult>? profile = null;
            await Loading.While(async () => profile = await Api.GetMyProfileAsync(CancellationToken.None));

            if (profile is { Success: true })
            {
                await RoleNav.LoadRolesAsync();
                return;
            }

            var user = state.User;
            await Api.SyncUserAsync(new SyncUserRequest
            {
                ChosenUsername = user.Identity?.Name ?? user.FindFirst(System.Security.Claims.ClaimTypes.Email)?.Value ?? "",
                Email = user.FindFirst(System.Security.Claims.ClaimTypes.Email)?.Value ?? "",
                AppName = _appName
            }, CancellationToken.None);

            await RoleNav.LoadRolesAsync();
            await ShowWelcomeDialog(user.Identity?.Name ?? "");
        }
        catch (Exception ex)
        {
            await Alert.ShowDialogForException(ex, Severity.Error);
        }
    }

    private async Task ShowWelcomeDialog(string username)
    {
        var parameters = new DialogParameters<WelcomeDialog> { { x => x.Username, username } };
        var options = new DialogOptions { CloseOnEscapeKey = false, BackdropClick = false, MaxWidth = MaxWidth.ExtraSmall, FullWidth = true };
        var dialog = await DialogService.ShowAsync<WelcomeDialog>("Welcome", parameters, options);
        var result = await dialog.Result;

        if (result is { Canceled: false, Data: WelcomeDialogResult data }
            && !string.IsNullOrWhiteSpace(data.FavoriteTeam))
        {
            await Loading.While(async () => await Api.UpdateMyProfileAsync(
                new UpdateUserProfileRequest { FavoriteTeam = data.FavoriteTeam },
                CancellationToken.None));
        }
    }

    public void Dispose() => AuthStateProvider.AuthenticationStateChanged -= OnAuthStateChanged;
}
