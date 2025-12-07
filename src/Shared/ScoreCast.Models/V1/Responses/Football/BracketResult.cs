namespace ScoreCast.Models.V1.Responses.Football;

public record BracketResult(List<BracketRound> Rounds);

public record BracketRound(string Name, List<BracketSlot> Slots);

public record BracketSlot(string Home, string Away, string? Date, string? HomeTeam, string? AwayTeam, int? HomeScore, int? AwayScore);
