using ScoreCast.Models.V1.Requests;

namespace ScoreCast.Models.V1.Requests.UserManagement;

public record SyncUserRequest : ScoreCastRequest
{
    public required string KeycloakUserId { get; set; }
    public required string Email { get; set; }
    public string? DisplayName { get; set; }
}
