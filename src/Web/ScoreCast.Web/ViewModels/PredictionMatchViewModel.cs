using ScoreCast.Models.V1.Responses.Football;
using ScoreCast.Shared.Enums;
using ScoreCast.Shared.Types;

namespace ScoreCast.Web.ViewModels;

public sealed class PredictionMatchViewModel
{
    public long MatchId { get; private init; }
    public string? HomeTeamShortName { get; private init; }
    public string? AwayTeamShortName { get; private init; }
    public string? HomeTeamLogo { get; private init; }
    public string? AwayTeamLogo { get; private init; }
    public string? Status { get; private init; }
    public DateTime? KickoffTime { get; private init; }
    public int? HomeScore { get; private init; }
    public int? AwayScore { get; private init; }
    public int? PredictedHomeScore { get; set; }
    public int? PredictedAwayScore { get; set; }
    public string? Outcome { get; set; }
    public bool HasPrediction => PredictedHomeScore.HasValue && PredictedAwayScore.HasValue;
    public bool HasSavedPrediction { get; set; }
    public bool IsLocked => Status == nameof(MatchStatus.Finished)
                            || (KickoffTime.HasValue && KickoffTime.Value <= ScoreCastDateTime.Now);

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

    public bool IsIncomplete => !IsLocked && (PredictedHomeScore.HasValue != PredictedAwayScore.HasValue);
}
