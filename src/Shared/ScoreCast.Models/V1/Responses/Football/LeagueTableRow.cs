namespace ScoreCast.Models.V1.Responses.Football;

public record LeagueTableRow(
    int Position, long TeamId, string TeamName, string? TeamLogo,
    int Played, int Won, int Drawn, int Lost,
    int GoalsFor, int GoalsAgainst, int GoalDifference, int Points);
