using ScoreCast.Ws.Domain.V1.Entities.Common;
using ScoreCast.Shared.Enums;

namespace ScoreCast.Ws.Domain.V1.Entities.Football;

public sealed record Gameweek : ScoreCastEntity
{
    public required long SeasonId { get; set; }
    public required int Number { get; set; }
    public DateOnly? StartDate { get; set; }
    public DateOnly? EndDate { get; set; }
    public GameweekStatus Status { get; set; } = GameweekStatus.Upcoming;

    public Season Season { get; init; } = default!;
    public ICollection<Match> Matches { get; init; } = [];
}
