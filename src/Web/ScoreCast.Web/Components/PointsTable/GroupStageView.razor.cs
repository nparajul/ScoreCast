using ScoreCast.Models.V1.Responses.Football;

namespace ScoreCast.Web.Components.PointsTable;

public partial class GroupStageView
{
    [Parameter, EditorRequired] public List<PointsTableGroup> Groups { get; set; } = [];
    [Parameter, EditorRequired] public List<CompetitionZoneResult> Zones { get; set; } = [];
}
