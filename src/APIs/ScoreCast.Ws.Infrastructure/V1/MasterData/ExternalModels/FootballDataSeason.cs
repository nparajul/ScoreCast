namespace ScoreCast.Ws.Infrastructure.V1.MasterData.ExternalModels;

internal sealed record FootballDataSeason(
    int Id, string StartDate, string EndDate, int? CurrentMatchday, FootballDataTeam? Winner);
