namespace ScoreCast.Ws.Infrastructure.V1.Football.ExternalModels;

internal sealed record FootballDataMatchesResponse(
    FootballDataResultSet? ResultSet,
    List<FootballDataMatch> Matches);
