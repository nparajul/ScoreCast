using ScoreCast.Shared.Enums;
using ScoreCast.Ws.Domain.V1.Entities.Common;
using ScoreCast.Ws.Domain.V1.Entities.UserManagement;

namespace ScoreCast.Ws.Domain.V1.Entities.Football;

public sealed record PredictionLeagueMember : ScoreCastEntity
{
    public long PredictionLeagueId { get; set; }
    public long UserId { get; set; }
    public PredictionLeagueMemberRole Role { get; set; } = PredictionLeagueMemberRole.Member;

    public PredictionLeague PredictionLeague { get; init; } = default!;
    public UserMaster User { get; init; } = default!;
}
