using System.Net.Http.Headers;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using Refit;
using ScoreCast.Web.Auth;

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

        builder.Services.AddScoped<FirebaseTokenHandler>();

        builder.Services
            .AddRefitClient<IScoreCastApiClient>(RefitSettings)
            .ConfigureHttpClient(c => c.BaseAddress = new Uri(apiBaseUrl))
            .AddHttpMessageHandler<FirebaseTokenHandler>();
    }
}

internal sealed class FirebaseTokenHandler(ScoreCastAuthStateProvider auth) : DelegatingHandler
{
    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken ct)
    {
        var token = await auth.GetIdTokenAsync();
        if (token is not null)
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);

        return await base.SendAsync(request, ct);
    }
}
