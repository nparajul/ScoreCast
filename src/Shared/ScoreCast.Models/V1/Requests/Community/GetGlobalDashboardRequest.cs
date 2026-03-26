namespace ScoreCast.Models.V1.Requests.Community;

public record GetGlobalDashboardRequest : ScoreCastRequest
{
    public string? Competition { get; set; }
}
