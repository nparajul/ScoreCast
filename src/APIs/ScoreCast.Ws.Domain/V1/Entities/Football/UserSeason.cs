using ScoreCast.Ws.Domain.V1.Entities.Common;
using ScoreCast.Ws.Domain.V1.Entities.UserManagement;

namespace ScoreCast.Ws.Domain.V1.Entities.Football;

public sealed record UserSeason : ScoreCastEntity
{
    public long UserId { get; set; }
    public long SeasonId { get; set; }

    public UserMaster User { get; init; } = null!;
    public Season Season { get; init; } = null!;
}
