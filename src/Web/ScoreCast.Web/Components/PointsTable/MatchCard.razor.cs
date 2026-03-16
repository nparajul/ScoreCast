using ScoreCast.Models.V1.Responses.Football;

namespace ScoreCast.Web.Components.PointsTable;

public partial class MatchCard
{
    [Parameter, EditorRequired] public BracketSlot Slot { get; set; } = null!;
}
