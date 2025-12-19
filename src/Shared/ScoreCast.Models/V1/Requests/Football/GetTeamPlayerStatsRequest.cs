namespace ScoreCast.Models.V1.Requests.Football;

public sealed class GetTeamPlayerStatsRequest
{
    public long TeamId { get; set; }
    public long? SeasonId { get; set; }
}
