namespace ScoreCast.Models.V1.Responses.Prediction;

public record UserSeasonResult(
    long Id, long SeasonId, string SeasonName,
    long CompetitionId, string CompetitionName, string CompetitionCode, string? CompetitionLogoUrl);
