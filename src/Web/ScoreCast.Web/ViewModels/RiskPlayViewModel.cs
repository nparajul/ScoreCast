using ScoreCast.Shared.Enums;

namespace ScoreCast.Web.ViewModels;

public sealed class RiskPlayViewModel
{
    public RiskPlayType RiskType { get; init; }
    public long? MatchId { get; set; }
    public string? Selection { get; set; }
    public int? BonusPoints { get; set; }
    public bool? IsWon { get; set; }
    public bool? IsResolved { get; set; }
    public bool IsActive => MatchId.HasValue;

    public string Label => RiskType switch
    {
        RiskPlayType.DoubleDown => "Double Down",
        RiskPlayType.ExactScoreBoost => "Exact Score Boost",
        RiskPlayType.CleanSheetBet => "Clean Sheet Bet",
        RiskPlayType.FirstGoalTeam => "First Goal Team",
        RiskPlayType.OverUnderGoals => "Over/Under 2.5",
        _ => RiskType.ToString()
    };

    public string Description => RiskType switch
    {
        RiskPlayType.DoubleDown => "Double your points on one match. Wrong = -5",
        RiskPlayType.ExactScoreBoost => "Confident in exact score? +15 bonus, wrong = -5",
        RiskPlayType.CleanSheetBet => "Bet a team keeps a clean sheet. +5 / -3",
        RiskPlayType.FirstGoalTeam => "Pick which team scores first. +3 / -2",
        RiskPlayType.OverUnderGoals => "Over or under 2.5 total goals. +3 / -2",
        _ => ""
    };

    public string Icon => RiskType switch
    {
        RiskPlayType.DoubleDown => "⚡",
        RiskPlayType.ExactScoreBoost => "🎯",
        RiskPlayType.CleanSheetBet => "🧤",
        RiskPlayType.FirstGoalTeam => "⚽",
        RiskPlayType.OverUnderGoals => "📊",
        _ => "🎲"
    };
}
