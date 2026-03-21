namespace ScoreCast.Models.V1.Requests.Football;

public sealed record GetMatchExtrasRequest : ScoreCastRequest
{
    public long MatchId { get; set; }
}
