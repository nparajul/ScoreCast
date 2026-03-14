using System.Text.Json.Serialization;

namespace ScoreCast.Ws.Infrastructure.V1.MasterData.ExternalModels;

internal sealed record FplFixture(
    [property: JsonPropertyName("id")] int Id,
    [property: JsonPropertyName("code")] int Code,
    [property: JsonPropertyName("event")] int? Event,
    [property: JsonPropertyName("team_h")] int TeamH,
    [property: JsonPropertyName("team_a")] int TeamA,
    [property: JsonPropertyName("team_h_score")] int? TeamHScore,
    [property: JsonPropertyName("team_a_score")] int? TeamAScore,
    [property: JsonPropertyName("kickoff_time")] string? KickoffTime,
    [property: JsonPropertyName("finished")] bool Finished,
    [property: JsonPropertyName("started")] bool? Started,
    [property: JsonPropertyName("pulse_id")] int? PulseId,
    [property: JsonPropertyName("stats")] List<FplFixtureStat> Stats);
