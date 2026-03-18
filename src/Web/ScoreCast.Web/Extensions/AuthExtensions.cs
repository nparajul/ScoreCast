using Microsoft.AspNetCore.Components.WebAssembly.Authentication;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;

namespace ScoreCast.Web.Extensions;

public static class AuthExtensions
{
    public static void AddScoreCastAuth(this WebAssemblyHostBuilder builder)
    {
        var authority = builder.Configuration["Keycloak:Authority"]!;
        var clientId = builder.Configuration["Keycloak:ClientId"] ?? "scorecast-web";

        builder.Services.AddOidcAuthentication(options =>
        {
            options.ProviderOptions.Authority = authority;
            options.ProviderOptions.ClientId = clientId;
            options.ProviderOptions.ResponseType = "code";
            options.ProviderOptions.DefaultScopes.Add("openid");
            options.ProviderOptions.DefaultScopes.Add("profile");
            options.ProviderOptions.DefaultScopes.Add("email");
        });
    }
}
