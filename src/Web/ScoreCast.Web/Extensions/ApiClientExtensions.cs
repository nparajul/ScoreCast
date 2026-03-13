using Microsoft.AspNetCore.Components.WebAssembly.Authentication;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using Refit;
using ScoreCast.ApiClient.V1.Apis;

namespace ScoreCast.Web.Extensions;

public static class ApiClientExtensions
{
    public static void AddScoreCastApiClients(this WebAssemblyHostBuilder builder)
    {
        var apiBaseUrl = builder.Configuration["Api:BaseUrl"]!;

        builder.Services.AddTransient<ApiAuthHandler>();

        builder.Services
            .AddRefitClient<IUserManagementApi>()
            .ConfigureHttpClient(c => c.BaseAddress = new Uri(apiBaseUrl))
            .AddHttpMessageHandler<ApiAuthHandler>();

        builder.Services
            .AddRefitClient<IHealthApi>()
            .ConfigureHttpClient(c => c.BaseAddress = new Uri(apiBaseUrl));

        builder.Services
            .AddRefitClient<ILeagueApi>()
            .ConfigureHttpClient(c => c.BaseAddress = new Uri(apiBaseUrl));
    }
}

internal sealed class ApiAuthHandler(IAccessTokenProvider tokenProvider) : DelegatingHandler
{
    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        var result = await tokenProvider.RequestAccessToken();
        if (result.TryGetToken(out var token))
            request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token.Value);

        return await base.SendAsync(request, cancellationToken);
    }
}
