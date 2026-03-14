namespace ScoreCast.Models.V1.Responses.Football;

public record LeagueTableRow(
    int Position, long TeamId, string TeamName, string? TeamLogo,
    int Played, int Won, int Drawn, int Lost,
    int GoalsFor, int GoalsAgainst, int GoalDifference, int Points,
    List<RecentForm> RecentForm);

public record RecentForm(string Result, string Opponent, int HomeScore, int AwayScore, bool IsHome, List<FormGoal> Goals);

public record FormGoal(string PlayerName, int Count, bool IsOwnGoal, bool IsHome);
