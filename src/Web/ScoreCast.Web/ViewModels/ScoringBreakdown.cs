using ScoreCast.Models.V1.Responses.Prediction;

namespace ScoreCast.Web.ViewModels;

public record ScoringBreakdown(string Label, int Points, int Count, int Total, string CssClass, List<MatchBreakdownItem> Matches)
{
    public static List<ScoringBreakdown> Calculate(
        List<PredictionMatchViewModel> matches,
        List<ScoringRuleResult> rules)
    {
        var predicted = matches.Where(m => m.HasPrediction && m.Outcome is not null).ToList();

        return rules.Select(rule =>
        {
            var matching = predicted.Where(m => m.Outcome == rule.Outcome)
                .Select(m => new MatchBreakdownItem(
                    m.HomeTeamId, m.HomeTeamShortName ?? "?",
                    m.AwayTeamId, m.AwayTeamShortName ?? "?",
                    m.HomeTeamLogo, m.AwayTeamLogo,
                    m.PredictedHomeScore ?? 0, m.PredictedAwayScore ?? 0,
                    m.HomeScore ?? 0, m.AwayScore ?? 0, rule.Points))
                .ToList();
            return new ScoringBreakdown(rule.Description, rule.Points, matching.Count, matching.Count * rule.Points, GetCssClass(rule.Outcome), matching);
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

public record MatchBreakdownItem(
    long HomeTeamId, string Home, long AwayTeamId, string Away, string? HomeLogo, string? AwayLogo,
    int PredHome, int PredAway, int ActualHome, int ActualAway, int Points);
