namespace ScoreCast.Models.V1.Requests.Community;

public record GetGlobalLeaderboardRequest : ScoreCastRequest
{
    public string? Competition { get; set; }
}
