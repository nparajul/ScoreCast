using ScoreCast.Ws.Domain.V1.Entities.Common;
using ScoreCast.Ws.Domain.V1.Entities.UserManagement;

namespace ScoreCast.Ws.Domain.V1.Entities.Football;

public sealed record PredictionLeague : ScoreCastEntity
{
    public string Name { get; set; } = string.Empty;
    public string InviteCode { get; set; } = string.Empty;
    public long SeasonId { get; set; }
    public long CreatedByUserId { get; set; }

    public Season Season { get; init; } = default!;
    public UserMaster CreatedByUser { get; init; } = default!;
    public ICollection<PredictionLeagueMember> Members { get; init; } = [];
    public ICollection<Prediction> Predictions { get; init; } = [];
}
