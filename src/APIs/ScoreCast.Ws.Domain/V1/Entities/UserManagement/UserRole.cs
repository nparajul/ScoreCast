using ScoreCast.Ws.Domain.V1.Entities.Common;

namespace ScoreCast.Ws.Domain.V1.Entities.UserManagement;

public sealed record UserRole : ScoreCastEntity
{
    public required long UserId { get; set; }
    public required long RoleId { get; set; }

    public UserMaster User { get; init; } = default!;
    public RoleMaster Role { get; init; } = default!;
}
