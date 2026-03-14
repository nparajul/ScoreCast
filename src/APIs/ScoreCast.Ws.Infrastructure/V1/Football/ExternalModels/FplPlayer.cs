using System.Text.Json.Serialization;

namespace ScoreCast.Ws.Infrastructure.V1.Football.ExternalModels;

internal sealed record FplPlayer(
    int Id,
    int Code,
    [property: JsonPropertyName("first_name")] string FirstName,
    [property: JsonPropertyName("second_name")] string SecondName,
    [property: JsonPropertyName("web_name")] string WebName,
    int Team,
    [property: JsonPropertyName("team_code")] int TeamCode);
