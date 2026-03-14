using ScoreCast.Models.V1.Requests;

namespace ScoreCast.Models.V1.Requests.Prediction;

public record CreatePredictionLeagueRequest : ScoreCastRequest
{
    public required string Name { get; set; }
    public long SeasonId { get; set; }
}
