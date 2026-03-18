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

        builder.Services.AddTransient<BaseAddressAuthorizationMessageHandler>();

        builder.Services
            .AddRefitClient<IScoreCastApiClient>(RefitSettings)
            .ConfigureHttpClient(c => c.BaseAddress = new Uri(apiBaseUrl))
            .AddHttpMessageHandler<BaseAddressAuthorizationMessageHandler>();

        builder.Services
            .AddRefitClient<IAuthApi>(RefitSettings)
            .ConfigureHttpClient(c => c.BaseAddress = new Uri(apiBaseUrl));
    }
}
