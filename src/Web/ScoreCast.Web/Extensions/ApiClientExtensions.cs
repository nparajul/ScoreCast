using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Components.WebAssembly.Authentication;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using Refit;

namespace ScoreCast.Web.Extensions;

public static class ApiClientExtensions
{
    private static readonly RefitSettings RefitSettings = new()
    {
        ContentSerializer = new SystemTextJsonContentSerializer(new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            PropertyNameCaseInsensitive = true,
            IgnoreReadOnlyFields = true,
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
            Converters = { new JsonStringEnumConverter() }
        })
    };

    public static void AddScoreCastApiClients(this WebAssemblyHostBuilder builder)
    {
        var apiBaseUrl = builder.Configuration["Api:BaseUrl"]!;

        builder.Services.AddScoped<ScoreCastApiAuthHandler>(sp =>
            new ScoreCastApiAuthHandler(sp.GetRequiredService<IAccessTokenProvider>(),
                sp.GetRequiredService<NavigationManager>(), apiBaseUrl));

        builder.Services
            .AddRefitClient<IScoreCastApiClient>(RefitSettings)
            .ConfigureHttpClient(c => c.BaseAddress = new Uri(apiBaseUrl))
            .AddHttpMessageHandler<ScoreCastApiAuthHandler>();

        builder.Services
            .AddRefitClient<IAuthApi>(RefitSettings)
            .ConfigureHttpClient(c => c.BaseAddress = new Uri(apiBaseUrl));
    }
}

internal sealed class ScoreCastApiAuthHandler : AuthorizationMessageHandler
{
    public ScoreCastApiAuthHandler(IAccessTokenProvider provider, NavigationManager navigation, string apiBaseUrl)
        : base(provider, navigation)
    {
        ConfigureHandler(authorizedUrls: [apiBaseUrl]);
    }
}
