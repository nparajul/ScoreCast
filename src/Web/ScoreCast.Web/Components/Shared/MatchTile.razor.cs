namespace ScoreCast.Web.Components.Shared;

public partial class MatchTile
{
    [Parameter, EditorRequired] public MatchTileModel Match { get; set; } = null!;
    [Parameter] public bool IsExpanded { get; set; }
    [Parameter] public EventCallback OnToggle { get; set; }
    [Parameter] public long? ExcludeTeamId { get; set; }
}
