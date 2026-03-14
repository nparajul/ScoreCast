using ScoreCast.Models.V1.Requests;

namespace ScoreCast.Models.V1.Requests.Football;

public record SyncPulseEventsRequest : ScoreCastRequest
{
    public required string CompetitionCode { get; set; }
    public int BatchSize { get; set; } = 20;
}
