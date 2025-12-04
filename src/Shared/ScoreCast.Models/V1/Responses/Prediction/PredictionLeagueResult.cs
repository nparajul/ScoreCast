namespace ScoreCast.Models.V1.Responses.Prediction;

public record PredictionLeagueResult(
    long Id, string Name, string InviteCode, long SeasonId,
    string? SeasonName, int MemberCount, string OwnerDisplayName);
