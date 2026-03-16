using ScoreCast.ApiClient.V1.Apis;
using ScoreCast.Models.V1.Requests.MasterData;

namespace ScoreCast.Jobs;

public sealed class SyncMatchesJob(IScoreCastApiClient api, ILogger<SyncMatchesJob> logger)
{
    private const string AppName = "JOBS:SYNC-MATCHES";

    public async Task ExecuteAsync()
    {
        var response = await api.GetCompetitionsAsync(CancellationToken.None);
        if (response is not { Success: true, Data: not null })
        {
            logger.LogWarning("SyncMatches: failed to fetch competitions: {Message}", response.Message);
            return;
        }

        foreach (var competition in response.Data)
        {
            try
            {
                var result = await api.SyncMatchesAsync(
                    new SyncCompetitionRequest { CompetitionCode = competition.Code, AppName = AppName },
                    CancellationToken.None);

                if (result.Success)
                    logger.LogInformation("SyncMatches {Code}: {Message}", competition.Code, result.Message);
                else
                    logger.LogWarning("SyncMatches {Code} failed: {Message}", competition.Code, result.Message);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "SyncMatches {Code} error", competition.Code);
            }
        }
    }
}
