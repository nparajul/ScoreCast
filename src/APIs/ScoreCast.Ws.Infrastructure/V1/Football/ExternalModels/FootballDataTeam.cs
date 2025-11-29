namespace ScoreCast.Ws.Infrastructure.V1.Football.ExternalModels;

internal sealed record FootballDataTeam(
    int Id, string Name, string? ShortName, string? Crest,
    string? Website, int? Founded, string? ClubColors, string? Venue);
