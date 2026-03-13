namespace ScoreCast.Ws.Infrastructure.V1.Football.ExternalModels;

internal sealed record FootballDataScore(
    string? Winner, string? Duration,
    FootballDataScoreDetail? FullTime, FootballDataScoreDetail? HalfTime);
