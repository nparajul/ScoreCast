using ScoreCast.Ws.Domain.V1.Entities.Common;
using ScoreCast.Ws.Domain.V1.Enums;

namespace ScoreCast.Ws.Domain.V1.Entities.Football;

public sealed record Match : ScoreCastEntity
{
    public long GameweekId { get; set; }
    public long HomeTeamId { get; set; }
    public long AwayTeamId { get; set; }
    public string? ExternalId { get; set; }
    public DateTime? KickoffTime { get; set; }
    public int? HomeScore { get; set; }
    public int? AwayScore { get; set; }
    public MatchStatus Status { get; set; } = MatchStatus.Scheduled;

    public Gameweek Gameweek { get; init; } = default!;
    public Team HomeTeam { get; init; } = default!;
    public Team AwayTeam { get; init; } = default!;
    public ICollection<Prediction> Predictions { get; init; } = [];
}
