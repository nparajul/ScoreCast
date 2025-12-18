namespace ScoreCast.Ws.Infrastructure.V1.MasterData.ExternalModels;

internal sealed record FootballDataMatchDetailResponse(
    FootballDataMatch Match);

internal sealed record FootballDataGoal(
    int? Minute, int? ExtraTime, string? Type,
    FootballDataPersonRef? Team,
    FootballDataPersonRef? Scorer,
    FootballDataPersonRef? Assist);

internal sealed record FootballDataBooking(
    int? Minute,
    FootballDataPersonRef? Team,
    FootballDataPersonRef? Player,
    string? Card);

internal sealed record FootballDataSubstitution(
    int? Minute,
    FootballDataPersonRef? Team,
    FootballDataPersonRef? PlayerOut,
    FootballDataPersonRef? PlayerIn);

internal sealed record FootballDataPersonRef(int? Id, string? Name);
