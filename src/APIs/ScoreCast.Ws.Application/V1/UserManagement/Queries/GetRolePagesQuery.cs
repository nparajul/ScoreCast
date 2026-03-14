using FastEndpoints;
using ScoreCast.Models.V1.Responses;
using ScoreCast.Models.V1.Responses.UserManagement;

namespace ScoreCast.Ws.Application.V1.UserManagement.Queries;

public record GetRolePagesQuery(long RoleId) : ICommand<ScoreCastResponse<List<PageResult>>>;
