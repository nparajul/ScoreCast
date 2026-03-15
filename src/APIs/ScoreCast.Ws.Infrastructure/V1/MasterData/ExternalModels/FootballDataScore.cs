using ScoreCast.Shared.Constants;

namespace ScoreCast.Ws.Infrastructure.V1.MasterData.ExternalModels;

internal sealed record FootballDataScore(
    string? Winner, string? Duration,
    FootballDataScoreDetail? FullTime, FootballDataScoreDetail? HalfTime,
    FootballDataScoreDetail? RegularTime);
