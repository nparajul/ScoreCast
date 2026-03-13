namespace ScoreCast.Web.Components.Reusable;

public partial class WelcomeDialog
{
    [CascadingParameter] public IMudDialogInstance Dialog { get; set; } = default!;
    [Parameter] public string? Username { get; set; }

    private string? DisplayName { get; set; }
    private string? FavoriteTeam { get; set; }

    private void Skip() => Dialog.Close(DialogResult.Ok(new WelcomeDialogResult(null, null)));

    private void Save() => Dialog.Close(DialogResult.Ok(new WelcomeDialogResult(DisplayName?.Trim(), FavoriteTeam)));

    private static readonly string[] Teams =
    [
        "Arsenal", "Aston Villa", "AFC Bournemouth", "Brentford", "Brighton & Hove Albion",
        "Chelsea", "Crystal Palace", "Everton", "Fulham", "Ipswich Town",
        "Leicester City", "Liverpool", "Manchester City", "Manchester United", "Newcastle United",
        "Nottingham Forest", "Southampton", "Tottenham Hotspur", "West Ham United", "Wolverhampton Wanderers"
    ];
}

public record WelcomeDialogResult(string? DisplayName, string? FavoriteTeam);
