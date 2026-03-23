namespace ScoreCast.Models.V1.Requests.Football;

public sealed class GetAllHighlightsRequest
{
    public int Skip { get; set; }
    public int Take { get; set; } = 10;
}
