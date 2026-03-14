namespace ScoreCast.Models.V1.Requests.Football;

public sealed class GetGameweekMatchesRequest
{
    public long SeasonId { get; set; }
    public int GameweekNumber { get; set; }
}
