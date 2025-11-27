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

        builder.Services
            .AddRefitClient<IUserManagementApi>()
            .ConfigureHttpClient(c => c.BaseAddress = new Uri(apiBaseUrl))
            .AddHttpMessageHandler<BaseAddressAuthorizationMessageHandler>();

        builder.Services
            .AddRefitClient<IHealthApi>()
            .ConfigureHttpClient(c => c.BaseAddress = new Uri(apiBaseUrl));

        builder.Services.AddScoped<BaseAddressAuthorizationMessageHandler>();
    }
}
