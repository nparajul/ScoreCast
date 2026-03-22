using ScoreCast.Models.V1.Requests.UserManagement;
using ScoreCast.Models.V1.Responses.Football;
using ScoreCast.Models.V1.Responses.UserManagement;
using ScoreCast.Web.Components.Helpers;
using ScoreCast.Web.Validation;
using ScoreCast.Web.Validation.Settings;
using ScoreCast.Web.ViewModels.Settings;
using MudBlazor;

namespace ScoreCast.Web.Pages;

public partial class Settings
{
    [Inject] private IScoreCastApiClient Api { get; set; } = null!;
    [Inject] private ILoadingService Loading { get; set; } = null!;
    [Inject] private IAlertService Alert { get; set; } = null!;
    [Inject] private IDialogService Dialog { get; set; } = null!;
    [Inject] private NavigationManager Nav { get; set; } = null!;

    private UserProfileResult? _profile;
    private readonly SettingsViewModel _model = new();
    private readonly SettingsViewModelValidator _validator = new();
    private MudForm _form = null!;
    private List<TeamResult> _allTeams = [];
    private TeamResult? _selectedTeam;
    private string? _usernameHelper;
    private string? _usernameError;

    private bool HasChanges => _model.DisplayName != _profile?.DisplayName
                            || _model.Username != _profile?.UserId
                            || _selectedTeam?.Name != (_profile?.FavoriteTeam ?? "");

    protected override async Task OnInitializedAsync()
    {
        await Loading.While(async () =>
        {
            var profileTask = Api.GetMyProfileAsync(CancellationToken.None);
            var compsTask = Api.GetCompetitionsAsync(CancellationToken.None);
            await Task.WhenAll(profileTask, compsTask);

            var comps = compsTask.Result;
            if (comps is { Success: true, Data: not null })
            {
                foreach (var comp in comps.Data)
                {
                    var teams = await Api.GetTeamsAsync(comp.Name, CancellationToken.None);
                    if (teams is { Success: true, Data: not null })
                        _allTeams.AddRange(teams.Data);
                }
                _allTeams = _allTeams.DistinctBy(t => t.Id).OrderBy(t => t.Name).ToList();
            }

            var response = profileTask.Result;
            if (response is { Success: true, Data: not null })
            {
                _profile = response.Data;
                _model.DisplayName = _profile.DisplayName ?? "";
                _model.Username = _profile.UserId;
                _usernameHelper = $"Current: @{_profile.UserId}";
                _selectedTeam = _allTeams.FirstOrDefault(t =>
                    t.Name.Equals(_profile.FavoriteTeam, StringComparison.OrdinalIgnoreCase));
            }
        });
    }

    private Task<IEnumerable<TeamResult>> SearchTeams(string? value, CancellationToken ct)
    {
        var results = string.IsNullOrWhiteSpace(value)
            ? _allTeams.AsEnumerable()
            : _allTeams.Where(t => t.Name.Contains(value, StringComparison.OrdinalIgnoreCase));
        return Task.FromResult(results);
    }

    private async Task SaveProfile()
    {
        await _form.ValidateAsync();
        if (!_form.IsValid) return;

        _usernameError = null;

        await Loading.While(async () =>
        {
            // Update username if changed
            var usernameChanged = _model.Username.Trim().ToLowerInvariant() != _profile!.UserId;
            if (usernameChanged)
            {
                var usernameResult = await Api.SetUsernameAsync(
                    new SetUsernameRequest { Username = _model.Username.Trim() },
                    CancellationToken.None);

                if (!usernameResult.Success)
                {
                    _usernameError = usernameResult.Message ?? "Username not available";
                    return;
                }
            }

            var response = await Api.UpdateMyProfileAsync(
                new UpdateUserProfileRequest { DisplayName = _model.DisplayName.Trim(), FavoriteTeam = _selectedTeam?.Name },
                CancellationToken.None);

            if (response is { Success: true, Data: not null })
            {
                _profile = response.Data;
                _model.DisplayName = _profile.DisplayName ?? "";
                _model.Username = _profile.UserId;
                _usernameHelper = $"Current: @{_profile.UserId}";
                _selectedTeam = _allTeams.FirstOrDefault(t =>
                    t.Name.Equals(_profile.FavoriteTeam, StringComparison.OrdinalIgnoreCase));
                Alert.Add("Profile updated", Severity.Success);
            }
        });
    }

    private async Task Logout()
    {
        var result = await Dialog.ShowMessageBoxAsync(
            "Log Out",
            "Are you sure you want to log out?",
            yesText: "Log Out",
            cancelText: "Cancel");

        if (result == true)
            Nav.NavigateTo("/logout", replace: true);
    }
}
