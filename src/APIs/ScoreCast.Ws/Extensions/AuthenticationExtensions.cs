using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using ScoreCast.Shared.Constants;

namespace ScoreCast.Ws.Extensions;

public static class AuthenticationExtensions
{
    public static void AddScoreCastAuthentication(this WebApplicationBuilder builder)
    {
        var keycloakSection = builder.Configuration.GetSection("Keycloak");

        builder.Services
            .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
            .AddJwtBearer(options =>
            {
                options.Authority = keycloakSection["Authority"];
                options.Audience = keycloakSection["Audience"];
                options.RequireHttpsMetadata = !builder.Environment.IsDevelopment()
                                               && !builder.Environment.IsEnvironment("Local");

                options.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuer = true,
                    ValidateAudience = true,
                    ValidateLifetime = true,
                    ValidateIssuerSigningKey = true,
                    ValidIssuer = keycloakSection["Authority"],
                    ValidAudience = keycloakSection["Audience"],
                    NameClaimType = "preferred_username",
                    RoleClaimType = "roles"
                };
            })
            .AddScheme<AuthenticationSchemeOptions, ApiKeyAuthHandler>(
                ApiKeyAuth.SchemeName, null);

        builder.Services.AddTransient<IClaimsTransformation, KeycloakRoleClaimTransformation>();
        builder.Services.AddAuthorization();
    }
}
