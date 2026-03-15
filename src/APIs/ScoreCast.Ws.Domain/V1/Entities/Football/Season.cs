using ScoreCast.Ws.Domain.V1.Entities.Common;

namespace ScoreCast.Ws.Domain.V1.Entities.Football;

public sealed record Season : ScoreCastEntity
{
    public required string Name { get; set; }
    public long CompetitionId { get; set; }
    public string? ExternalId { get; set; }
    public DateOnly StartDate { get; set; }
    public DateOnly EndDate { get; set; }
    public int? CurrentMatchday { get; set; }
    public long? WinnerTeamId { get; set; }
    public bool IsCurrent { get; set; }

    public Competition Competition { get; init; } = null!;
    public Team? WinnerTeam { get; set; }
    public ICollection<SeasonTeam> SeasonTeams { get; init; } = [];
    public ICollection<TeamPlayer> TeamPlayers { get; init; } = [];
    public ICollection<Stage> Stages { get; init; } = [];
    public ICollection<Gameweek> Gameweeks { get; init; } = [];
}
