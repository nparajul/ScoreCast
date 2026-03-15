using Microsoft.AspNetCore.Components;

namespace ScoreCast.Web.Components.PointsTable;

public partial class ConnectorSvg
{
    [Parameter] public int Pairs { get; set; }
    [Parameter] public bool Mirror { get; set; }
}
