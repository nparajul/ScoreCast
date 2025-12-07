using System.Text.Json;
using ScoreCast.Ws.Application.V1.Interfaces;

namespace ScoreCast.Ws.Application.V1.Config.Queries;

public record GetAppConfigQuery(string Key) : IQuery<JsonDocument?>;
