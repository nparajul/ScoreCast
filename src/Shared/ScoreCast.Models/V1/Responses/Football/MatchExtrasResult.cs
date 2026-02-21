namespace ScoreCast.Models.V1.Responses.Football;

public record MatchExtrasResult(
    List<H2HMatch> HeadToHead,
    List<FormEntry> HomeForm,
    List<FormEntry> AwayForm,
    UserMatchPrediction? MyPrediction,
    CommunityPredictions Community,
    List<PlayerSeasonStat> HomePlayerStats,
    List<PlayerSeasonStat> AwayPlayerStats);

public record H2HMatch(
    DateTime KickoffTime, long MatchId,
    string HomeTeam, string AwayTeam,
    string? HomeLogo, string? AwayLogo,
    int HomeScore, int AwayScore);

public record FormEntry(
    DateTime KickoffTime, long MatchId,
    string Opponent, string? OpponentLogo,
    bool IsHome, int GoalsFor, int GoalsAgainst,
    string Result); // W, D, L

public record UserMatchPrediction(
    int PredictedHome, int PredictedAway,
    string? Outcome, int Points);

public record CommunityPredictions(
    int TotalPredictions,
    int HomeWinPct, int DrawPct, int AwayWinPct,
    string? MostPopularScore, int MostPopularScorePct);

public record PlayerSeasonStat(
    long PlayerId, string Name, string? PhotoUrl, string? Position,
    int Goals, int Assists, int YellowCards, int RedCards);
