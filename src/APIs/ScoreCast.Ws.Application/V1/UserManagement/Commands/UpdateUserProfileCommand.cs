using FastEndpoints;
using ScoreCast.Models.V1.Requests.UserManagement;
using ScoreCast.Models.V1.Responses;
using ScoreCast.Models.V1.Responses.UserManagement;

namespace ScoreCast.Ws.Application.V1.UserManagement.Commands;

public record UpdateUserProfileCommand(string FirebaseUid, UpdateUserProfileRequest Request)
    : ICommand<ScoreCastResponse<UserProfileResult>>;
