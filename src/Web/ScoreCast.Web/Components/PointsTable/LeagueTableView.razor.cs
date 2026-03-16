using ScoreCast.Models.V1.Responses.Football;

namespace ScoreCast.Web.Components.PointsTable;

public partial class LeagueTableView
{
    [Parameter, EditorRequired] public List<PointsTableRow> Rows { get; set; } = [];
    [Parameter] public List<CompetitionZoneResult> Zones { get; set; } = [];
    [Parameter] public string? HighlightTeamName { get; set; }

    private string _mobileTab = "Short";

    private string RowStyleFunc(PointsTableRow row, int _)
    {
        if (IsHighlighted(row))
            return "background:rgba(var(--mud-palette-primary-rgb),0.12);border-left:3px solid var(--mud-palette-primary);";
        var zone = Zones.FirstOrDefault(z => row.Position >= z.StartPosition && row.Position <= z.EndPosition);
        if (zone is null) return string.Empty;
        return $"background:{zone.Color}15;border-left:3px solid {zone.Color};";
    }

    private bool IsHighlighted(PointsTableRow row) =>
        HighlightTeamName is not null && (row.TeamName == HighlightTeamName || row.TeamShortName == HighlightTeamName);
}
