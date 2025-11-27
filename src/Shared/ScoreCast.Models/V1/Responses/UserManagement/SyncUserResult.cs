namespace ScoreCast.Models.V1.Responses.UserManagement;

public record SyncUserResult(long Id, string UserId, string Email, string? DisplayName, bool IsNewUser);
