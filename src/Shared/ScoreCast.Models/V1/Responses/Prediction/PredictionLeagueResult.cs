namespace ScoreCast.Models.V1.Responses.Prediction;

public record PredictionLeagueResult(
    long Id, string Name, string InviteCode,
    long CompetitionId, string CompetitionName, string CompetitionCode, string? CompetitionLogoUrl,
    long SeasonId, string? SeasonName,
    int MemberCount, string OwnerDisplayName);
