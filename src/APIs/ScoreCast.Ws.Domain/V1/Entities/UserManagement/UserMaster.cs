using ScoreCast.Ws.Domain.V1.Entities.Common;

namespace ScoreCast.Ws.Domain.V1.Entities.UserManagement;

public sealed record UserMaster : ScoreCastEntity
{
    public required string KeycloakUserId { get; set; }
    public required string UserId { get; set; }
    public required string Email { get; set; }
    public string? DisplayName { get; set; }
    public string? AvatarUrl { get; set; }
    public string? FavoriteTeam { get; set; }
    public int TotalPoints { get; set; }
    public int CurrentStreak { get; set; }
    public int LongestStreak { get; set; }
    public bool IsActive { get; set; } = true;
    public DateTime? LastLoginDate { get; set; }

    public ICollection<UserRole> UserRoles { get; init; } = [];
}
