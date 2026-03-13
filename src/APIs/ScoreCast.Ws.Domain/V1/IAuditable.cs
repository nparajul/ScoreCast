using ScoreCast.Shared.Types;

namespace ScoreCast.Ws.Domain.V1;

public interface IAuditable
{
    bool IsDeleted { get; set; }
    string? CreatedByApp { get; set; }
    ScoreCastDateTime CreatedDate { get; set; }
    string? CreatedBy { get; set; }
    string? ModifiedBy { get; set; }
    ScoreCastDateTime ModifiedDate { get; set; }
    string? ModifiedByApp { get; set; }
}
