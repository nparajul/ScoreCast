using ScoreCast.Shared.Types;

namespace ScoreCast.Models.V1.Responses.UserManagement;

public record UserProfileResult(
    long Id,
    string UserId,
    string Email,
    string? DisplayName,
    string? AvatarUrl,
    string? FavoriteTeam,
    int TotalPoints,
    int CurrentStreak,
    int LongestStreak,
    bool IsActive,
    ScoreCastDateTime MemberSince);
