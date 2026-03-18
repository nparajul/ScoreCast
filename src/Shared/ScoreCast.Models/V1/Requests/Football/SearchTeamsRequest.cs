namespace ScoreCast.Models.V1.Requests.Football;

public sealed class SearchTeamsRequest
{
    public string? Q { get; set; }
    public int Skip { get; set; }
    public int Take { get; set; } = 50;
}
