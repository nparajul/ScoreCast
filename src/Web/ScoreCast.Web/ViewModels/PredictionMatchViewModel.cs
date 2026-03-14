using ScoreCast.Models.V1.Responses.Football;
using ScoreCast.Shared.Enums;

namespace ScoreCast.Web.ViewModels;

public sealed class PredictionMatchViewModel
{
    public long MatchId { get; set; }
    public string HomeTeamShortName { get; set; } = "";
    public string AwayTeamShortName { get; set; } = "";
    public string? HomeTeamLogo { get; set; }
    public string? AwayTeamLogo { get; set; }
    public string Status { get; set; } = "";
    public DateTime? KickoffTime { get; set; }
    public int? HomeScore { get; set; }
    public int? AwayScore { get; set; }
    public int? PredictedHomeScore { get; set; }
    public int? PredictedAwayScore { get; set; }
    public string? Outcome { get; set; }
    public bool HasPrediction => PredictedHomeScore.HasValue && PredictedAwayScore.HasValue;
    public bool IsLocked => Status == nameof(MatchStatus.Finished) || (KickoffTime.HasValue && KickoffTime.Value <= DateTime.UtcNow);

    public static PredictionMatchViewModel FromMatch(MatchDetail match) => new()
    {
        MatchId = match.MatchId,
        HomeTeamShortName = match.HomeTeamShortName,
        AwayTeamShortName = match.AwayTeamShortName,
        HomeTeamLogo = match.HomeTeamLogo,
        AwayTeamLogo = match.AwayTeamLogo,
        Status = match.Status,
        KickoffTime = match.KickoffTime,
        HomeScore = match.HomeScore,
        AwayScore = match.AwayScore
    };

    public string GetRowClass()
    {
        if (Status != nameof(MatchStatus.Finished) || HomeScore is null || AwayScore is null)
            return "";

        if (!HasPrediction)
            return "predict-none";

        return Outcome switch
        {
            "ExactScore" => "predict-exact",
            "CorrectResultAndGoalDifference" => "predict-result-gd",
            "CorrectResult" => "predict-correct",
            "CorrectGoalDifference" => "predict-gd",
            "Incorrect" => "predict-wrong",
            _ => ""
        };
    }

    public static string? Validate(List<PredictionMatchViewModel> matches)
    {
        var unlocked = matches.Where(m => !m.IsLocked).ToList();
        if (unlocked.Count == 0)
            return "No matches available to predict";

        var incomplete = unlocked.Where(m => m.PredictedHomeScore.HasValue != m.PredictedAwayScore.HasValue).ToList();
        if (incomplete.Count > 0)
            return $"Please enter both scores for: {string.Join(", ", incomplete.Select(m => $"{m.HomeTeamShortName} vs {m.AwayTeamShortName}"))}";

        var missing = unlocked.Where(m => !m.HasPrediction).ToList();
        if (missing.Count > 0)
            return $"Please enter predictions for all matches ({missing.Count} remaining)";

        return null;
    }
}
