using System.Text.Json.Serialization;

namespace ScoreCast.Ws.Infrastructure.V1.MasterData.ExternalModels;

internal sealed record PulseFixtureResponse(
    List<PulseTeamList>? TeamLists,
    List<PulseEvent>? Events,
    PulseGround? Ground);

internal sealed record PulseTeamList(
    PulseTeamRef? Team,
    List<PulsePlayer>? Lineup,
    List<PulsePlayer>? Substitutes);

internal sealed record PulseTeamRef(int Id, string? Name);

internal sealed record PulsePlayer(
    int Id,
    PulsePlayerName? Name,
    PulsePlayerBirth? Birth);

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
    int Secs,
    string Label);

internal sealed record PulseGround(
    string? Name, string? City);
