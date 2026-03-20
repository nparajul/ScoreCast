using FastEndpoints;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using ScoreCast.Models.V1.Requests.MasterData;
using ScoreCast.Ws.Application.V1.MasterData.Commands;

namespace ScoreCast.Ws.Services;

public sealed class EnhanceLiveMatchesBackgroundService(ILogger<EnhanceLiveMatchesBackgroundService> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        // Wait for app startup
        await Task.Delay(TimeSpan.FromSeconds(10), stoppingToken);

        using var timer = new PeriodicTimer(TimeSpan.FromMinutes(2));
        while (await timer.WaitForNextTickAsync(stoppingToken))
        {
            try
            {
                var result = await new EnhanceLiveMatchesCommand(new EnhanceLiveMatchesRequest()
                {
                    AppName = nameof(EnhanceLiveMatchesBackgroundService),
                }).ExecuteAsync(stoppingToken);

                if (!result.Success)
                    logger.LogError("Failed Response EnhanceLive background: {Message}", result.Message);

                logger.LogInformation("Success Response EnhanceLive background: {Message}", result.Message);
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                logger.LogError(ex, "EnhanceLive background failed");
            }
        }
    }
}
