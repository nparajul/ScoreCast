using ScoreCast.Models.V1.Responses.Football;
using ScoreCast.Shared.Validation;

namespace ScoreCast.Web.Components.Reusable;

public partial class WelcomeDialog
{
    private const int MaxStep = 5;

    [CascadingParameter] public IMudDialogInstance Dialog { get; set; } = null!;
    [Parameter] public string? Username { get; set; }
    [Inject] private IScoreCastApiClient Api { get; set; } = null!;

    private int _step;
    private string _displayName = "";
    private string? _nameError;
    private TeamResult? _selectedTeam;
    private List<TeamResult> _allTeams = [];

    protected override async Task OnInitializedAsync()
    {
        _displayName = Username ?? "";

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

    private bool CanAdvance => _step switch
    {
        4 => _displayName.Trim().Length >= 2 && string.IsNullOrEmpty(_nameError),
        _ => true
    };

    private void Next()
    {
        if (_step == 4)
        {
            _nameError = ValidateDisplayName(_displayName);
            if (_nameError is not null) return;
        }
        if (_step < MaxStep) _step++;
    }

    private void Back() { if (_step > 0) _step--; }

    private void Finish() =>
        Dialog.Close(DialogResult.Ok(new WelcomeDialogResult(_selectedTeam?.Name, _displayName.Trim())));

    private Task<IEnumerable<TeamResult>> SearchTeams(string? value, CancellationToken ct)
    {
        var results = string.IsNullOrWhiteSpace(value)
            ? _allTeams.AsEnumerable()
            : _allTeams.Where(t => t.Name.Contains(value, StringComparison.OrdinalIgnoreCase));
        return Task.FromResult(results);
    }

    private static string? ValidateDisplayName(string name)
    {
        var trimmed = name.Trim();
        if (trimmed.Length < 2) return "Must be at least 2 characters";
        if (trimmed.Length > 30) return "Must be 30 characters or less";
        if (ProfanityFilter.ContainsProfanity(trimmed)) return "Please choose an appropriate display name";
        return null;
    }
}

public record WelcomeDialogResult(string? FavoriteTeam, string? DisplayName = null);
