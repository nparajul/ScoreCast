using System.Net.Http.Json;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using ScoreCast.Models.V1.Requests.MasterData;
using ScoreCast.Shared.Constants;

namespace ScoreCast.Ws.Services;

public sealed class EnhanceLiveMatchesBackgroundService(
    IHttpClientFactory httpClientFactory,
    IConfiguration config,
    ILogger<EnhanceLiveMatchesBackgroundService> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await Task.Delay(TimeSpan.FromSeconds(15), stoppingToken);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                var client = httpClientFactory.CreateClient();
                var apiKey = config["ApiKeySettings:Clients:0:Key"];
                client.DefaultRequestHeaders.Add(ApiKeyAuth.HeaderName, apiKey);

                var baseUrl = config["Kestrel:Endpoints:Http:Url"]
                              ?? config["ASPNETCORE_URLS"]
                              ?? "http://localhost:5105";

                var response = await client.PostAsJsonAsync(
                    $"{baseUrl}/api/v1/master-data/enhance-live",
                    new EnhanceLiveMatchesRequest { AppName = nameof(EnhanceLiveMatchesBackgroundService) },
                    stoppingToken);

                if (response.IsSuccessStatusCode)
                    logger.LogInformation("EnhanceLive background: completed successfully");
                else
                    logger.LogError("EnhanceLive background: HTTP {StatusCode}", response.StatusCode);
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                logger.LogError(ex, "EnhanceLive background failed");
            }

            await Task.Delay(TimeSpan.FromSeconds(30), stoppingToken);
        }
    }
}
