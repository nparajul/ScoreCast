using ScoreCast.Shared.Types;

namespace ScoreCast.Models.V1.Responses.Football;

public record TeamDetailResult(
    long Id, string Name, string? ShortName, string? LogoUrl,
    string? Venue, int? Founded, string? ClubColors, string? Website,
    TeamNextMatch? NextMatch, List<TeamFormMatch> RecentForm,
    List<TeamCompetitionSeason> Competitions);

public record TeamNextMatch(
    long MatchId, ScoreCastDateTime KickoffTime, string DateLabel, string DayOfWeek,
    string OpponentName, string? OpponentLogo, bool IsHome,
    string CompetitionName, string? CompetitionLogo);

public record TeamFormMatch(
    long MatchId, ScoreCastDateTime KickoffTime,
    string OpponentName, string? OpponentLogo, bool IsHome,
    int? HomeScore, int? AwayScore, string Result,
    string CompetitionName, string? CompetitionLogo);

public record TeamCompetitionSeason(
    long SeasonId, string CompetitionName, string CompetitionCode,
    string? CompetitionLogo, string SeasonName, bool IsCurrent);
