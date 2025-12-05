using ScoreCast.Models.V1.Responses.Football;

namespace ScoreCast.Web.Components.Reusable;

public partial class WelcomeDialog
{
    [CascadingParameter] public IMudDialogInstance Dialog { get; set; } = default!;
    [Parameter] public string? Username { get; set; }
    [Inject] private IScoreCastApiClient Api { get; set; } = default!;

    private string? DisplayName { get; set; }
    private string? FavoriteTeam { get; set; }
    private List<CompetitionResult> _competitions = [];
    private List<TeamResult> _teams = [];

    private string? _selectedCompetition;
    private string? SelectedCompetition
    {
        get => _selectedCompetition;
        set
        {
            if (_selectedCompetition == value) return;
            _selectedCompetition = value;
            FavoriteTeam = null;
            _ = LoadTeamsAsync(value);
        }
    }

    protected override async Task OnInitializedAsync()
    {
        var response = await Api.GetCompetitionsAsync(CancellationToken.None);
        if (response.Success && response.Data is not null)
            _competitions = response.Data;
    }

    private async Task LoadTeamsAsync(string? competitionName)
    {
        _teams = [];
        if (string.IsNullOrWhiteSpace(competitionName)) return;

        var response = await Api.GetTeamsAsync(competitionName, CancellationToken.None);
        if (response.Success && response.Data is not null)
            _teams = response.Data;

        StateHasChanged();
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
