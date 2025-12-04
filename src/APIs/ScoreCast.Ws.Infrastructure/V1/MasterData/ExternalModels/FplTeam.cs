using System.Text.Json.Serialization;

namespace ScoreCast.Ws.Infrastructure.V1.MasterData.ExternalModels;

internal sealed record FplTeam(
    int Id,
    int Code,
    string Name,
    [property: JsonPropertyName("short_name")] string ShortName);
