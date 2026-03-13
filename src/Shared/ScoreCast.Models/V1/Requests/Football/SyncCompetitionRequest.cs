using ScoreCast.Models.V1.Requests;

namespace ScoreCast.Models.V1.Requests.Football;

public record SyncCompetitionRequest : ScoreCastRequest
{
    public required string CompetitionCode { get; set; }
}
