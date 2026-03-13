using System.Net.Http.Headers;
using ScoreCast.Shared.Enums;

namespace ScoreCast.Ws.Extensions;

public static class ExternalHttpClientExtensions
{
    public static void AddScoreCastExternalHttpClients(this WebApplicationBuilder builder)
    {
        builder.Services.AddHttpClient(nameof(ScoreCastHttpClient.FootballDataClient), (sp, client) =>
        {
            var config = sp.GetRequiredService<IConfiguration>();
            var baseUrl = config["ApiSettings:FootballDataApi:BaseUrl"];
            var apiKey = config["ApiSettings:FootballDataApi:ApiKey"];
            ArgumentException.ThrowIfNullOrWhiteSpace(baseUrl);
            ArgumentException.ThrowIfNullOrWhiteSpace(apiKey);
            client.BaseAddress = new Uri(baseUrl, UriKind.RelativeOrAbsolute);
            client.DefaultRequestHeaders.Add("X-Auth-Token", apiKey);
            client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
        });
    }
}
