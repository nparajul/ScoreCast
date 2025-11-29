using ScoreCast.Ws.Domain.V1.Entities.Common;

namespace ScoreCast.Ws.Domain.V1.Entities.UserManagement;

public sealed record PageMaster : ScoreCastEntity
{
    public required string PageCode { get; set; }
    public required string PageName { get; set; }
    public string? PageUrl { get; set; }
    public long? ParentPageId { get; set; }

    public PageMaster? ParentPage { get; set; }
    public ICollection<PageMaster> ChildPages { get; init; } = [];
    public ICollection<RolePage> RolePages { get; init; } = [];
}
