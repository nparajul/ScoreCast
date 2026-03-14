namespace ScoreCast.Ws.Infrastructure.V1.MasterData.ExternalModels;

internal sealed record FootballDataMatchesResponse(
    FootballDataResultSet? ResultSet,
    List<FootballDataMatch> Matches);
