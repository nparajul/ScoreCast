namespace ScoreCast.Models.V1.Requests.MasterData;

public record SyncCompetitionRequest : ScoreCastRequest
{
    public required string CompetitionCode { get; set; }
}
