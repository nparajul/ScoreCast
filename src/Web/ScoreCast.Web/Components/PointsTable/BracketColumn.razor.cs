using Microsoft.AspNetCore.Components;
using ScoreCast.Models.V1.Responses.Football;

namespace ScoreCast.Web.Components.PointsTable;

public partial class BracketColumn
{
    [Parameter] public List<BracketSlot>? Slots { get; set; }
    [Parameter] public int PairSize { get; set; } = 1;
}
