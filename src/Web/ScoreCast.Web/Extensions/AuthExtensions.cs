using Microsoft.AspNetCore.Components.WebAssembly.Hosting;

namespace ScoreCast.Web.Extensions;

public static class AuthExtensions
{
    public static void AddScoreCastAuth(this WebAssemblyHostBuilder builder)
    {
        builder.Services.AddOidcAuthentication(options =>
        {
            var authority = builder.Configuration["Keycloak:Authority"]!;
            var clientId = builder.Configuration["Keycloak:ClientId"]!;

            options.ProviderOptions.Authority = authority;
            options.ProviderOptions.ClientId = clientId;
            options.ProviderOptions.ResponseType = "code";

            options.ProviderOptions.DefaultScopes.Clear();
            options.ProviderOptions.DefaultScopes.Add("openid");
            options.ProviderOptions.DefaultScopes.Add("profile");
            options.ProviderOptions.DefaultScopes.Add("email");
            options.ProviderOptions.DefaultScopes.Add("roles");

            options.UserOptions.RoleClaim = "roles";
            options.UserOptions.NameClaim = "preferred_username";
        });
    }
}
