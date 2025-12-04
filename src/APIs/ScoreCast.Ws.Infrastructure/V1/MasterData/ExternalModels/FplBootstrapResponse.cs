namespace ScoreCast.Ws.Infrastructure.V1.MasterData.ExternalModels;

internal sealed record FplBootstrapResponse(
    List<FplTeam> Teams,
    List<FplPlayer> Elements);
