using ScoreCast.ApiClient.V1.Apis;
using ScoreCast.Models.V1.Requests.MasterData;

namespace ScoreCast.Jobs;

public sealed class SyncMatchesJob(IScoreCastApiClient api, IConfiguration configuration, ILogger<SyncMatchesJob> logger)
{
    private const string AppName = "JOBS:SYNC-MATCHES";

    public async Task ExecuteAsync()
    {
        var competitions = configuration.GetSection("Competitions").Get<string[]>() ?? [];

        foreach (var code in competitions)
        {
            try
            {
                var result = await api.SyncMatchesAsync(
                    new SyncCompetitionRequest { CompetitionCode = code, AppName = AppName },
                    CancellationToken.None);

                if (result.Success)
                    logger.LogInformation("SyncMatches {Code}: {Message}", code, result.Message);
                else
                    logger.LogWarning("SyncMatches {Code} failed: {Message}", code, result.Message);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "SyncMatches {Code} error", code);
            }
        }
    }
}
