namespace ScoreCast.Ws.Infrastructure.V1.Football.ExternalModels;

internal sealed record FplBootstrapResponse(
    List<FplTeam> Teams,
    List<FplPlayer> Elements);
