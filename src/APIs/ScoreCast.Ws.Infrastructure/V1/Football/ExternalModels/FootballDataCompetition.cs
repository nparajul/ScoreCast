namespace ScoreCast.Ws.Infrastructure.V1.Football.ExternalModels;

internal sealed record FootballDataCompetition(
    FootballDataArea Area, int Id, string Name, string Code, string Type,
    string? Emblem, FootballDataSeason? CurrentSeason, List<FootballDataSeason> Seasons);
