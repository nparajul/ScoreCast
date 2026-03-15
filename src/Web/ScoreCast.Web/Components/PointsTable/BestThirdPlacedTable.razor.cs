using ScoreCast.Models.V1.Responses.Football;

namespace ScoreCast.Web.Components.PointsTable;

public partial class BestThirdPlacedTable
{
    [Parameter, EditorRequired] public List<PointsTableRow> Rows { get; set; } = [];
}
