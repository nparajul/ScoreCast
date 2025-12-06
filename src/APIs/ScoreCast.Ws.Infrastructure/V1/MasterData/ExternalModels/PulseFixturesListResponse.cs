using System.Text.Json.Serialization;

namespace ScoreCast.Ws.Infrastructure.V1.MasterData.ExternalModels;

internal sealed record PulseFixturesListResponse(
    [property: JsonPropertyName("content")] List<PulseFixtureListItem> Content,
    [property: JsonPropertyName("pageInfo")] PulsePageInfo? PageInfo);

internal sealed record PulsePageInfo(
    [property: JsonPropertyName("numEntries")] int NumEntries);

internal sealed record PulseFixtureListItem(
    [property: JsonPropertyName("id")] double Id,
    [property: JsonPropertyName("status")] string Status,
    [property: JsonPropertyName("phase")] string? Phase,
    [property: JsonPropertyName("clock")] PulseClock? Clock,
    [property: JsonPropertyName("goals")] List<PulseFixtureGoal>? Goals,
    [property: JsonPropertyName("ground")] PulseGround? Ground,
    [property: JsonPropertyName("teams")] List<PulseTeamScore>? Teams,
    [property: JsonPropertyName("kickoff")] PulseKickoff? Kickoff,
    [property: JsonPropertyName("gameweek")] PulseGameweekRef? Gameweek,
    [property: JsonPropertyName("matchOfficials")] List<PulseMatchOfficial>? MatchOfficials);

internal sealed record PulseFixtureGoal(
    [property: JsonPropertyName("personId")] int? PersonId,
    [property: JsonPropertyName("assistId")] int? AssistId,
    [property: JsonPropertyName("clock")] PulseClock? Clock,
    [property: JsonPropertyName("type")] string Type,
    [property: JsonPropertyName("description")] string? Description);

internal sealed record PulseKickoff(
    [property: JsonPropertyName("millis")] long? Millis,
    [property: JsonPropertyName("label")] string? Label);

internal sealed record PulseGameweekRef(
    [property: JsonPropertyName("gameweek")] int Gameweek,
    [property: JsonPropertyName("compSeason")] PulseCompSeasonRef? CompSeason);

internal sealed record PulseCompSeasonRef(
    [property: JsonPropertyName("id")] int Id,
    [property: JsonPropertyName("label")] string? Label);

internal sealed record PulseMatchOfficial(
    [property: JsonPropertyName("matchOfficialId")] int? MatchOfficialId,
    [property: JsonPropertyName("birth")] PulseDate? Birth,
    [property: JsonPropertyName("name")] PulsePlayerName? Name,
    [property: JsonPropertyName("role")] string? Role);
