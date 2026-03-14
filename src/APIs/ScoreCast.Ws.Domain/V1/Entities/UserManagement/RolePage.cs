using ScoreCast.Ws.Domain.V1.Entities.Common;

namespace ScoreCast.Ws.Domain.V1.Entities.UserManagement;

public sealed record RolePage : ScoreCastEntity
{
    public required long RoleId { get; set; }
    public required long PageId { get; set; }

    public RoleMaster Role { get; init; } = null!;
    public PageMaster Page { get; init; } = null!;
}
