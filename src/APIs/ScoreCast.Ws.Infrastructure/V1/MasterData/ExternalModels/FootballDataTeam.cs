namespace ScoreCast.Ws.Infrastructure.V1.MasterData.ExternalModels;

internal sealed record FootballDataTeam(
    int Id, string Name, string? ShortName, string? Crest,
    string? Website, int? Founded, string? ClubColors, string? Venue,
    FootballDataCoach? Coach = null,
    List<FootballDataPlayer>? Squad = null);

internal sealed record FootballDataCoach(int? Id, string? Name);

internal sealed record FootballDataPlayer(
    int Id, string Name, string? Position, string? DateOfBirth, string? Nationality);
