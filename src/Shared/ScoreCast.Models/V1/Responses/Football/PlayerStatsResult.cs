namespace ScoreCast.Models.V1.Responses.Football;

public record PlayerStatsResult(List<PlayerStatRow> Rows);

public record PlayerStatRow(
    long PlayerId,
    string PlayerName,
    string? PlayerImageUrl,
    string? TeamName,
    string? TeamLogo,
    int Goals,
    int PenaltyGoals,
    int OwnGoals,
    int Assists,
    int YellowCards,
    int RedCards);
