using Refit;
using ScoreCast.Models.V1.Requests.UserManagement;
using ScoreCast.Models.V1.Responses;
using ScoreCast.Models.V1.Responses.UserManagement;

namespace ScoreCast.ApiClient.V1.Apis;

public partial interface IUserManagementApi
{
    [Post("/api/v1/users/sync")]
    Task<ScoreCastResponse<SyncUserResult>> SyncUserAsync([Body] SyncUserRequest request);

    [Get("/api/v1/users/me")]
    Task<ScoreCastResponse<UserProfileResult>> GetMyProfileAsync();

    [Put("/api/v1/users/me")]
    Task<ScoreCastResponse<UserProfileResult>> UpdateMyProfileAsync([Body] UpdateUserProfileRequest request);
}
