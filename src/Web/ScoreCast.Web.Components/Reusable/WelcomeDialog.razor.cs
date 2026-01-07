using ScoreCast.Models.V1.Responses.Football;

namespace ScoreCast.Web.Components.Reusable;

public partial class WelcomeDialog
{
    [CascadingParameter] public IMudDialogInstance Dialog { get; set; } = null!;
    [Parameter] public string? Username { get; set; }
    [Inject] private IScoreCastApiClient Api { get; set; } = null!;

    private string? DisplayName { get; set; }
    private TeamResult? _selectedTeam;
    private List<TeamResult> _allTeams = [];

    protected override async Task OnInitializedAsync()
    {
        var comps = await Api.GetCompetitionsAsync(CancellationToken.None);
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
    }

    private Task<IEnumerable<TeamResult>> SearchTeams(string? value, CancellationToken ct)
    {
        var results = string.IsNullOrWhiteSpace(value)
            ? _allTeams.AsEnumerable()
            : _allTeams.Where(t => t.Name.Contains(value, StringComparison.OrdinalIgnoreCase));
        return Task.FromResult(results);
    }

    private void Skip() => Dialog.Close(DialogResult.Ok(new WelcomeDialogResult(null, null)));

    private void Save() => Dialog.Close(DialogResult.Ok(new WelcomeDialogResult(DisplayName?.Trim(), _selectedTeam?.Name)));
}

public record WelcomeDialogResult(string? DisplayName, string? FavoriteTeam);
