using FastEndpoints;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using ScoreCast.Models.V1.Requests.MasterData;
using ScoreCast.Ws.Application.V1.MasterData.Commands;

namespace ScoreCast.Ws.Services;

public sealed class EnhanceLiveMatchesBackgroundService(
    IServiceScopeFactory scopeFactory,
    ILogger<EnhanceLiveMatchesBackgroundService> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await Task.Delay(TimeSpan.FromSeconds(10), stoppingToken);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                using var scope = scopeFactory.CreateScope();
                var command = new EnhanceLiveMatchesCommand(new EnhanceLiveMatchesRequest
                {
                    AppName = nameof(EnhanceLiveMatchesBackgroundService),
                });
                var handler = scope.ServiceProvider.GetRequiredService<ICommandHandler<EnhanceLiveMatchesCommand, ScoreCast.Models.V1.Responses.ScoreCastResponse>>();
                var result = await handler.ExecuteAsync(command, stoppingToken);

                if (!result.Success)
                    logger.LogError("Failed Response EnhanceLive background: {Message}", result.Message);
                else
                    logger.LogInformation("Success Response EnhanceLive background: {Message}", result.Message);
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                logger.LogError(ex, "EnhanceLive background failed");
            }

            await Task.Delay(TimeSpan.FromSeconds(30), stoppingToken);
        }
    }
}
