using Refit;
using ScoreCast.Models.V1.Requests.UserManagement;
using ScoreCast.Models.V1.Responses;
using ScoreCast.Models.V1.Responses.UserManagement;

namespace ScoreCast.ApiClient.V1.Apis;

public partial interface IScoreCastApiClient
{
    [Post("/api/v1/users/sync")]
    Task<ScoreCastResponse<SyncUserResult>> SyncUserAsync([Body] SyncUserRequest request, CancellationToken ct);

    [Get("/api/v1/users/me")]
    Task<ScoreCastResponse<UserProfileResult>> GetMyProfileAsync(CancellationToken ct);

    [Put("/api/v1/users/me")]
    Task<ScoreCastResponse<UserProfileResult>> UpdateMyProfileAsync([Body] UpdateUserProfileRequest request, CancellationToken ct);

    [Put("/api/v1/users/me/username")]
    Task<ScoreCastResponse<UserProfileResult>> SetUsernameAsync([Body] SetUsernameRequest request, CancellationToken ct);

    [Get("/api/v1/users/me/roles")]
    Task<ScoreCastResponse<List<RoleResult>>> GetMyRolesAsync(CancellationToken ct);

    [Get("/api/v1/users/roles/{roleId}/pages")]
    Task<ScoreCastResponse<List<PageResult>>> GetRolePagesAsync(long roleId, CancellationToken ct);
}
