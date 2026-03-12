using ScoreCast.Shared.Types;

namespace ScoreCast.Ws.Domain.V1.Entities.Common;

public abstract record ScoreCastEntity : IAuditable
{
    public long Id { get; init; }
    public bool IsDeleted { get; set; } = false;
    public required string CreatedByApp { get; set; }
    public ScoreCastDateTime CreatedDate { get; set; } = ScoreCastDateTime.Now;
    public required string CreatedBy { get; set; }
    public required string ModifiedBy { get; set; }
    public ScoreCastDateTime ModifiedDate { get; set; } = ScoreCastDateTime.Now;
    public string? ModifiedByApp { get; set; }
}
