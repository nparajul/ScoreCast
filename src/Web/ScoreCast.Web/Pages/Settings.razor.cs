using Microsoft.AspNetCore.Components.WebAssembly.Authentication;
using ScoreCast.Models.V1.Requests.UserManagement;
using ScoreCast.Models.V1.Responses.UserManagement;
using ScoreCast.Web.Components.Helpers;
using MudBlazor;

namespace ScoreCast.Web.Pages;

public partial class Settings
{
    [Inject] private IScoreCastApiClient Api { get; set; } = null!;
    [Inject] private ILoadingService Loading { get; set; } = null!;
    [Inject] private ISnackbar Snackbar { get; set; } = null!;
    [Inject] private NavigationManager Nav { get; set; } = null!;

    private UserProfileResult? _profile;
    private string? _displayName;
    private string? _favoriteTeam;

    private bool HasChanges => _displayName != _profile?.DisplayName || _favoriteTeam != _profile?.FavoriteTeam;

    protected override async Task OnInitializedAsync()
    {
        await Loading.While(async () =>
        {
            var response = await Api.GetMyProfileAsync(CancellationToken.None);
            if (response is { Success: true, Data: not null })
            {
                _profile = response.Data;
                _displayName = _profile.DisplayName;
                _favoriteTeam = _profile.FavoriteTeam;
            }
        });
    }

    private async Task SaveProfile()
    {
        await Loading.While(async () =>
        {
            var response = await Api.UpdateMyProfileAsync(
                new UpdateUserProfileRequest { DisplayName = _displayName?.Trim(), FavoriteTeam = _favoriteTeam?.Trim() },
                CancellationToken.None);

            if (response is { Success: true, Data: not null })
            {
                _profile = response.Data;
                _displayName = _profile.DisplayName;
                _favoriteTeam = _profile.FavoriteTeam;
                Snackbar.Add("Profile updated", Severity.Success);
            }
        });
    }

    private void Logout() =>
        Nav.NavigateTo("authentication/logout", replace: true);
}
