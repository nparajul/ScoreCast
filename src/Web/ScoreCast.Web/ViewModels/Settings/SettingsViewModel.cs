namespace ScoreCast.Web.ViewModels.Settings;

public sealed class SettingsViewModel
{
    public string DisplayName { get; set; } = "";
    public string FavoriteTeam { get; set; } = "";
    public string? SelectedTeamName { get; set; }
}
