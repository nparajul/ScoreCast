using ScoreCast.Models.V1.Responses.Football;

namespace ScoreCast.Web.Components.PointsTable;

public partial class GroupCard
{
    [Parameter, EditorRequired] public PointsTableGroup Group { get; set; } = null!;
    [Parameter, EditorRequired] public List<CompetitionZoneResult> Zones { get; set; } = [];
}
