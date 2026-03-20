using ScoreCast.Web.Components.Helpers;

namespace ScoreCast.Web.Components.Shared;

public partial class MatchTile
{
    [Parameter, EditorRequired] public MatchTileModel Match { get; set; } = null!;
    [Parameter] public bool IsExpanded { get; set; }
    [Parameter] public EventCallback OnToggle { get; set; }
    [Parameter] public long? ExcludeTeamId { get; set; }
    [Inject] private IClientTimeProvider ClientTime { get; set; } = null!;
    [Inject] private NavigationManager Nav { get; set; } = null!;

    private string FormatLocal(DateTime utc, string format) => ClientTime.ToLocal(utc).ToString(format);

    private void GoToMatch() => Nav.NavigateTo($"/matches/{Match.MatchId}");
}
