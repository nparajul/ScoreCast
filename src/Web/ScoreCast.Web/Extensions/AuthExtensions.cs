using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using ScoreCast.Web.Auth;
using ScoreCast.Web.Components.Helpers;

namespace ScoreCast.Web.Extensions;

public static class AuthExtensions
{
    public static void AddScoreCastAuth(this WebAssemblyHostBuilder builder)
    {
        var apiBaseUrl = builder.Configuration["Api:BaseUrl"]!;

        builder.Services.AddHttpClient("ScoreCastAuth", c => c.BaseAddress = new Uri(apiBaseUrl));

        builder.Services.AddScoped<ScoreCastAuthStateProvider>();
        builder.Services.AddScoped<AuthenticationStateProvider>(sp =>
            sp.GetRequiredService<ScoreCastAuthStateProvider>());
        builder.Services.AddScoped<IAuthService>(sp =>
            sp.GetRequiredService<ScoreCastAuthStateProvider>());
        builder.Services.AddAuthorizationCore();
    }
}
