using ScoreCast.Ws.Domain.V1.Entities.Common;
using ScoreCast.Ws.Domain.V1.Entities.UserManagement;

namespace ScoreCast.Ws.Domain.V1.Entities.Football;

public sealed record PredictionLeague : ScoreCastEntity
{
    public string Name { get; set; } = string.Empty;
    public string InviteCode { get; set; } = string.Empty;
    public long CompetitionId { get; set; }
    public long SeasonId { get; set; }
    public long CreatedByUserId { get; set; }

    public Competition Competition { get; init; } = null!;
    public Season Season { get; init; } = null!;
    public UserMaster CreatedByUser { get; init; } = null!;
    public ICollection<PredictionLeagueMember> Members { get; init; } = [];
}
