using ScoreCast.Shared.Enums;
using ScoreCast.Ws.Domain.V1.Entities.Common;
using ScoreCast.Ws.Domain.V1.Entities.UserManagement;

namespace ScoreCast.Ws.Domain.V1.Entities.Football;

public sealed record RiskPlay : ScoreCastEntity
{
    public long SeasonId { get; set; }
    public long GameweekId { get; set; }
    public long UserId { get; set; }
    public long MatchId { get; set; }
    public RiskPlayType RiskType { get; set; }
    public string? Selection { get; set; } // e.g. "Over", "Under", team ID, etc.
    public int? BonusPoints { get; set; }
    public bool? IsResolved { get; set; }
    public bool? IsWon { get; set; }

    public Season Season { get; init; } = null!;
    public Gameweek Gameweek { get; init; } = null!;
    public UserMaster User { get; init; } = null!;
    public Match Match { get; init; } = null!;
}
