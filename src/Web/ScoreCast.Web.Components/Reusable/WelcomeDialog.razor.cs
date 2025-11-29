using ScoreCast.ApiClient.V1.Apis;
using ScoreCast.Models.V1.Responses.League;

namespace ScoreCast.Web.Components.Reusable;

public partial class WelcomeDialog
{
    [CascadingParameter] public IMudDialogInstance Dialog { get; set; } = default!;
    [Parameter] public string? Username { get; set; }
    [Inject] private ILeagueApi LeagueApi { get; set; } = default!;

    private string? DisplayName { get; set; }
    private string? FavoriteTeam { get; set; }
    private List<TeamResult> _teams = [];

    protected override async Task OnInitializedAsync()
    {
        var response = await LeagueApi.GetTeamsAsync("Premier League", CancellationToken.None);
        if (response.Success && response.Data is not null)
            _teams = response.Data;
    }

    private void Skip() => Dialog.Close(DialogResult.Ok(new WelcomeDialogResult(null, null)));

    private void Save() => Dialog.Close(DialogResult.Ok(new WelcomeDialogResult(DisplayName?.Trim(), FavoriteTeam)));

    private Task<IEnumerable<string>> SearchTeams(string? value, CancellationToken ct)
    {
        var names = _teams.Select(t => t.Name);
        var results = string.IsNullOrWhiteSpace(value)
            ? names
            : names.Where(n => n.Contains(value, StringComparison.OrdinalIgnoreCase));
        return Task.FromResult(results);
    }
}

public record WelcomeDialogResult(string? DisplayName, string? FavoriteTeam);
