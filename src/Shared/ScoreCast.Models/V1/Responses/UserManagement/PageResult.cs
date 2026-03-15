namespace ScoreCast.Models.V1.Responses.UserManagement;

public record PageResult(long Id, string PageCode, string PageName, string? PageUrl, long? ParentPageId, int DisplayOrder);
