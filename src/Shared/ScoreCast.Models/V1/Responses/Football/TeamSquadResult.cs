namespace ScoreCast.Models.V1.Responses.Football;

public record TeamSquadResult(List<SquadPlayer> Players);

public record SquadPlayer(
    long PlayerId, string Name, string? Position, string? PhotoUrl,
    string? Nationality, DateOnly? DateOfBirth);
