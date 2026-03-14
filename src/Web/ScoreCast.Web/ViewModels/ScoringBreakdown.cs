using ScoreCast.Models.V1.Responses.Prediction;

namespace ScoreCast.Web.ViewModels;

public record ScoringBreakdown(string Label, int Points, int Count, int Total, string CssClass)
{
    public static List<ScoringBreakdown> Calculate(
        List<PredictionMatchViewModel> matches,
        List<ScoringRuleResult> rules)
    {
        var predicted = matches.Where(m => m.HasPrediction && m.Outcome is not null).ToList();

        return rules.Select(rule =>
        {
            var count = predicted.Count(m => m.Outcome == rule.Outcome);
            return new ScoringBreakdown(rule.Description, rule.Points, count, count * rule.Points, GetCssClass(rule.Outcome));
        }).ToList();
    }

    private static string GetCssClass(string outcome) => outcome switch
    {
        "ExactScore" => "predict-exact",
        "CorrectResultAndGoalDifference" => "predict-result-gd",
        "CorrectResult" => "predict-correct",
        "CorrectGoalDifference" => "predict-gd",
        "Incorrect" => "predict-wrong",
        _ => ""
    };
}
