namespace ScoreCast.Models.V1.Requests.Football;

public sealed class GetTeamMatchesRequest
{
    public long TeamId { get; set; }
    public long? SeasonId { get; set; }
}
