namespace ScoreCast.Models.V1.Requests.MasterData;

public record SyncPulseEventsRequest : ScoreCastRequest
{
    public required string CompetitionCode { get; set; }
    public int BatchSize { get; set; } = 20;
}
