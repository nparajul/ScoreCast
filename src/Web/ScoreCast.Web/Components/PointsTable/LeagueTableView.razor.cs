using ScoreCast.Models.V1.Responses.Football;

namespace ScoreCast.Web.Components.PointsTable;

public partial class LeagueTableView
{
    [Parameter, EditorRequired] public List<PointsTableRow> Rows { get; set; } = [];
    [Parameter, EditorRequired] public List<CompetitionZoneResult> Zones { get; set; } = [];

    private string RowStyleFunc(PointsTableRow row, int _)
    {
        var zone = Zones.FirstOrDefault(z => row.Position >= z.StartPosition && row.Position <= z.EndPosition);
        if (zone is null) return string.Empty;
        return $"background:{zone.Color}15;border-left:3px solid {zone.Color};";
    }
}
