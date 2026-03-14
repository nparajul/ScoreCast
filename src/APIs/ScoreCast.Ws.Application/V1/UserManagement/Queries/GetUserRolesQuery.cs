using ScoreCast.Ws.Application.V1.Interfaces;
using ScoreCast.Models.V1.Responses;
using ScoreCast.Models.V1.Responses.UserManagement;

namespace ScoreCast.Ws.Application.V1.UserManagement.Queries;

public record GetUserRolesQuery(string KeycloakUserId) : IQuery<ScoreCastResponse<List<RoleResult>>>;
