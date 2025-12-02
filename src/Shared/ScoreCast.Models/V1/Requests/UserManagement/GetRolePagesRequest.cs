using ScoreCast.Models.V1.Requests;

namespace ScoreCast.Models.V1.Requests.UserManagement;

public record GetRolePagesRequest : ScoreCastRequest
{
    public required long RoleId { get; set; }
}
