using ScoreCast.Shared.Enums;

namespace ScoreCast.Models.V1.Responses.Prediction;

public record RiskPlayResult(
    long Id, long MatchId, RiskPlayType RiskType, string? Selection,
    int? BonusPoints, bool? IsResolved, bool? IsWon);
