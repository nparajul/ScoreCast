using ScoreCast.Ws.Domain.V1.Entities.Common;

namespace ScoreCast.Ws.Domain.V1.Entities.UserManagement;

public sealed record RoleMaster : ScoreCastEntity
{
    public required string Name { get; set; }
    public string? Description { get; set; }
    public int SortOrder { get; set; }
    public bool IsActive { get; set; } = true;

    public ICollection<UserRole> UserRoles { get; init; } = [];
    public ICollection<RolePage> RolePages { get; init; } = [];
}
