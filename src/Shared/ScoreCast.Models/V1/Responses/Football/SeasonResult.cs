namespace ScoreCast.Models.V1.Responses.Football;

public record SeasonResult(long Id, string Name, DateOnly StartDate, DateOnly EndDate, bool IsCurrent);
