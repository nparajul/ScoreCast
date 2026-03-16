namespace ScoreCast.Ws.Infrastructure.V1.MasterData.ExternalModels;

internal sealed record FootballDataMatch(
    int Id, string UtcDate, string Status, int? Matchday,
    string? Stage, string? Group,
    FootballDataMatchTeam HomeTeam, FootballDataMatchTeam AwayTeam,
    FootballDataScore Score,
    FootballDataCompetitionRef? Competition = null,
    int? Minute = null,
    int? InjuryTime = null,
    List<FootballDataReferee>? Referees = null,
    List<FootballDataGoal>? Goals = null,
    List<FootballDataBooking>? Bookings = null,
    List<FootballDataSubstitution>? Substitutions = null);

internal sealed record FootballDataCompetitionRef(int Id, string? Name, string? Code);

internal sealed record FootballDataReferee(int Id, string Name, string? Type);
