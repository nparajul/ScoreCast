using System.Text.Json.Serialization;

namespace ScoreCast.Ws.Infrastructure.V1.MasterData.ExternalModels;

internal sealed record FplFixture(
    int Id,
    int Code,
    int? Event,
    [property: JsonPropertyName("team_h")] int TeamH,
    [property: JsonPropertyName("team_a")] int TeamA,
    [property: JsonPropertyName("team_h_score")] int? TeamHScore,
    [property: JsonPropertyName("team_a_score")] int? TeamAScore,
    [property: JsonPropertyName("kickoff_time")] string? KickoffTime,
    bool Finished,
    [property: JsonPropertyName("pulse_id")] int? PulseId,
    List<FplFixtureStat> Stats);
