using System.Text.Json;
using ScoreCast.Ws.Domain.V1.Entities.Common;

namespace ScoreCast.Ws.Domain.V1.Entities;

public sealed record AppConfig : ScoreCastEntity
{
    public required string Key { get; set; }
    public required JsonDocument Value { get; set; }
}
