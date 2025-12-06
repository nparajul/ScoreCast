using System.Text.Json.Serialization;

namespace ScoreCast.Ws.Infrastructure.V1.MasterData.ExternalModels;

internal sealed record PulseTeamResponse(
    [property: JsonPropertyName("id")] int Id,
    [property: JsonPropertyName("name")] string Name,
    [property: JsonPropertyName("shortName")] string? ShortName,
    [property: JsonPropertyName("club")] PulseClub? Club,
    [property: JsonPropertyName("grounds")] List<PulseGround>? Grounds);

internal sealed record PulseClub(
    [property: JsonPropertyName("id")] int Id,
    [property: JsonPropertyName("name")] string Name,
    [property: JsonPropertyName("shortName")] string? ShortName,
    [property: JsonPropertyName("abbr")] string? Abbr);
