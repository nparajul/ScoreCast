using ScoreCast.Ws.Domain.V1.Entities.Common;

namespace ScoreCast.Ws.Domain.V1.Entities.UserManagement;

public sealed record UserMaster : ScoreCastEntity
{
    public required string FirebaseUid { get; set; }
    public required string UserId { get; set; }
    public required string Email { get; set; }
    public string? DisplayName { get; set; }
    public string? AvatarUrl { get; set; }
    public string? FavoriteTeam { get; set; }
    public int TotalPoints { get; set; }
    public int BestGameweek { get; set; }
    public bool IsActive { get; set; } = true;
    public bool HasCompletedOnboarding { get; set; }
    public DateTime? LastLoginDate { get; set; }

    public ICollection<UserRole> UserRoles { get; init; } = [];
}
