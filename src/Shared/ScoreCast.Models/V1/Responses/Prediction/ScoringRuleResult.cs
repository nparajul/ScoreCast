namespace ScoreCast.Models.V1.Responses.Prediction;

public record ScoringRuleResult(string Outcome, int Points, string Description, int DisplayOrder);
