namespace ScoreCast.Models.V1.Requests.UserManagement;

public record SyncUserRequest : ScoreCastRequest
{
    public string? KeycloakUserId { get; set; }
    public required string ChosenUsername { get; set; }
    public required string Email { get; set; }
    public string? DisplayName { get; set; }
}
