using System.Text.Json.Serialization;

namespace ScoreCast.Ws.Infrastructure.V1.MasterData.ExternalModels;

internal sealed record PulseFixtureResponse(
    List<PulseTeamList>? TeamLists,
    List<PulseEvent>? Events,
    PulseGround? Ground,
    PulseClock? Clock,
    string? Phase,
    List<PulseTeamScore>? Teams,
    string? Status,
    PulseHalfTimeScore? HalfTimeScore);

internal sealed record PulseTeamScore(
    PulseTeamRef? Team,
    double? Score);

internal sealed record PulseTeamList(
    PulseTeamRef? Team,
    List<PulsePlayer>? Lineup,
    List<PulsePlayer>? Substitutes,
    PulseFormation? Formation);

internal sealed record PulseTeamRef(double Id, string? Name);

internal sealed record PulsePlayer(
    int Id,
    PulsePlayerName? Name,
    PulsePlayerBirth? Birth,
    int? MatchShirtNumber,
    string? MatchPosition,
    bool? Captain);

internal sealed record PulsePlayerName(
    string? Display, string? First, string? Last);

internal sealed record PulsePlayerBirth(
    PulseDate? Date);

internal sealed record PulseDate(
    long? Millis, string? Label);

internal sealed record PulseEvent(
    int? Id,
    [property: JsonPropertyName("personId")] int? PersonId,
    [property: JsonPropertyName("teamId")] int? TeamId,
    [property: JsonPropertyName("assistId")] int? AssistId,
    PulseClock? Clock,
    string Type,
    string? Description);

internal sealed record PulseClock(
    double? Secs,
    string Label);

internal sealed record PulseGround(
    string? Name, string? City);

internal sealed record PulseFormation(
    string? Label, List<List<int>>? Players);

internal sealed record PulseHalfTimeScore(
    int? HomeScore, int? AwayScore);
