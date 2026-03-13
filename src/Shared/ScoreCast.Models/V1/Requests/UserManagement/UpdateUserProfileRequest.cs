using ScoreCast.Models.V1.Requests;

namespace ScoreCast.Models.V1.Requests.UserManagement;

public record UpdateUserProfileRequest : ScoreCastRequest
{
    public string? DisplayName { get; set; }
    public string? AvatarUrl { get; set; }
    public string? FavoriteTeam { get; set; }
}
