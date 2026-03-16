using System.Text.Json;
using System.Text.Json.Serialization;
using Refit;
using ScoreCast.ApiClient.V1.Apis;
using ScoreCast.Shared.Constants;

namespace ScoreCast.Jobs;

public static class ApiClientExtensions
{
    public static void AddScoreCastApiClient(this IServiceCollection services, IConfiguration configuration)
    {
        var baseUrl = configuration["ScoreCastApi:BaseUrl"]!;
        var apiKey = configuration["ScoreCastApi:ApiKey"]!;

        var refitSettings = new RefitSettings
        {
            ContentSerializer = new SystemTextJsonContentSerializer(new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                PropertyNameCaseInsensitive = true,
                DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
                Converters = { new JsonStringEnumConverter() }
            })
        };

        services.AddRefitClient<IScoreCastApiClient>(refitSettings)
            .ConfigureHttpClient(c =>
            {
                c.BaseAddress = new Uri(baseUrl);
                c.DefaultRequestHeaders.Add(ApiKeyAuth.HeaderName, apiKey);
            });
    }
}
