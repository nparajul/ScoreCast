using System.Net.Http.Headers;
using Microsoft.Extensions.Http.Resilience;
using ScoreCast.Shared.Constants;
using ScoreCast.Shared.Enums;

namespace ScoreCast.Ws.Extensions;

public static class ExternalHttpClientExtensions
{
    public static void AddScoreCastExternalHttpClients(this WebApplicationBuilder builder)
    {
        builder.Services.AddHttpClient(nameof(ScoreCastHttpClient.FootballDataClient), (sp, client) =>
        {
            var config = sp.GetRequiredService<IConfiguration>();
            var baseUrl = config[FootballDataApi.BaseUrlKey];
            var apiKey = config[FootballDataApi.ApiKeyKey];
            ArgumentException.ThrowIfNullOrWhiteSpace(baseUrl);
            ArgumentException.ThrowIfNullOrWhiteSpace(apiKey);
            client.BaseAddress = new Uri(baseUrl, UriKind.RelativeOrAbsolute);
            client.DefaultRequestHeaders.Add(FootballDataApi.AuthHeader, apiKey);
            client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
        })
        .AddStandardResilienceHandler();

        builder.Services.AddHttpClient(nameof(ScoreCastHttpClient.FplClient), (sp, client) =>
        {
            var config = sp.GetRequiredService<IConfiguration>();
            var baseUrl = config[FplApi.BaseUrlKey];
            ArgumentException.ThrowIfNullOrWhiteSpace(baseUrl);
            client.BaseAddress = new Uri(baseUrl);
            client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
            client.DefaultRequestHeaders.UserAgent.ParseAdd("ScoreCast/1.0");
        })
        .AddStandardResilienceHandler();

        builder.Services.AddHttpClient(nameof(ScoreCastHttpClient.PulseClient), (sp, client) =>
        {
            var config = sp.GetRequiredService<IConfiguration>();
            var baseUrl = config["ApiSettings:PulseApi:BaseUrl"];
            ArgumentException.ThrowIfNullOrWhiteSpace(baseUrl);
            client.BaseAddress = new Uri(baseUrl);
            client.DefaultRequestHeaders.Add("Origin", "https://www.premierleague.com");
            client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
        })
        .AddStandardResilienceHandler();
    }
}
