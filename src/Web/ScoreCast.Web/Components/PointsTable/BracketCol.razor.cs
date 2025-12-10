using Microsoft.AspNetCore.Components;
using ScoreCast.Models.V1.Responses.Football;

namespace ScoreCast.Web.Components.PointsTable;

public partial class BracketCol
{
    [Parameter] public int Index { get; set; }
    [Parameter] public List<BracketSlot>? Slots { get; set; }
    [Parameter] public int RowSpan { get; set; } = 1;
    [Parameter] public int PairGap { get; set; }
}
