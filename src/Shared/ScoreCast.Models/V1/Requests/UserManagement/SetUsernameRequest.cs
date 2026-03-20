namespace ScoreCast.Models.V1.Requests.UserManagement;

public record SetUsernameRequest : ScoreCastRequest
{
    public required string Username { get; set; }
}
