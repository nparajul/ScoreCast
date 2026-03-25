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
    int BestGameweek,
    int CompletedGameweeks,
    bool IsActive,
    ScoreCastDateTime MemberSince,
    bool HasCompletedOnboarding = true)
{
    public string AvgPerGameweek => CompletedGameweeks > 0
        ? ((double)TotalPoints / CompletedGameweeks).ToString("F2")
        : "0.00";
}
