namespace ScoreCast.Web.Components.Reusable;

public partial class WelcomeDialog
{
    [CascadingParameter] public IMudDialogInstance Dialog { get; set; } = default!;
    [Parameter] public string? Username { get; set; }

    private ScannableTextField<string?> _displayNameRef = null!;

    private string? DisplayName { get; set; }
    private string? FavoriteTeam { get; set; }

    private void Skip() => Dialog.Close(DialogResult.Ok(new WelcomeDialogResult(null, null)));

    private void Save() => Dialog.Close(DialogResult.Ok(new WelcomeDialogResult(DisplayName?.Trim(), FavoriteTeam)));

    private Task<bool> HandleDisplayName()
    {
        try
        {
            if (string.IsNullOrWhiteSpace(DisplayName))
                return Task.FromResult(false);
            DisplayName = DisplayName.Trim();
            return Task.FromResult(true);
        }
        catch (Exception exception)
        {
            return Task.FromException<bool>(exception);
        }
    }

    private Task<IEnumerable<string>> SearchTeams(string? value, CancellationToken ct)
    {
        var results = string.IsNullOrWhiteSpace(value)
            ? _teams
            : _teams.Where(t => t.Contains(value, StringComparison.OrdinalIgnoreCase));
        return Task.FromResult(results);
    }

    private async Task FocusNext()
    {
        await Task.Delay(100);
        if (string.IsNullOrWhiteSpace(DisplayName))
            await _displayNameRef.FocusAsync();
    }

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (firstRender)
        {
            await FocusNext();
        }

        await base.OnAfterRenderAsync(firstRender);
    }

    private static readonly string[] _teams =
    [
        "Arsenal", "Aston Villa", "AFC Bournemouth", "Brentford", "Brighton & Hove Albion",
        "Chelsea", "Crystal Palace", "Everton", "Fulham", "Ipswich Town",
        "Leicester City", "Liverpool", "Manchester City", "Manchester United", "Newcastle United",
        "Nottingham Forest", "Southampton", "Tottenham Hotspur", "West Ham United", "Wolverhampton Wanderers"
    ];
}

public record WelcomeDialogResult(string? DisplayName, string? FavoriteTeam);
