namespace ScoreCast.Ws.Infrastructure.V1.Football.ExternalModels;

internal sealed record FplFixtureStat(
    string Identifier,
    List<FplStatValue> H,
    List<FplStatValue> A);

internal sealed record FplStatValue(int Value, int Element);
