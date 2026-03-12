using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;

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
                    RoleClaimType = "realm_access.roles"
                };
            });

        builder.Services.AddAuthorization();
    }
}
