namespace ScoreCast.Ws.Infrastructure.V1.Football.ExternalModels;

internal sealed record FootballDataMatch(
    int Id, string UtcDate, string Status, int? Matchday,
    string? Stage, string? Group,
    FootballDataMatchTeam HomeTeam, FootballDataMatchTeam AwayTeam,
    FootballDataScore Score,
    List<FootballDataReferee>? Referees = null);

internal sealed record FootballDataReferee(int Id, string Name, string? Type);
