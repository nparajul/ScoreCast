using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using ScoreCast.Shared.Constants;

namespace ScoreCast.Ws.Extensions;

public static class AuthenticationExtensions
{
    public static void AddScoreCastAuthentication(this WebApplicationBuilder builder)
    {
        var firebaseProjectId = builder.Configuration["Firebase:ProjectId"]!;

        builder.Services
            .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
            .AddJwtBearer(options =>
            {
                options.Authority = $"https://securetoken.google.com/{firebaseProjectId}";
                options.Audience = firebaseProjectId;

                options.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuer = true,
                    ValidateAudience = true,
                    ValidateLifetime = true,
                    ValidateIssuerSigningKey = true,
                    ValidIssuer = $"https://securetoken.google.com/{firebaseProjectId}",
                    ValidAudience = firebaseProjectId
                };
            })
            .AddScheme<AuthenticationSchemeOptions, ApiKeyAuthHandler>(
                ApiKeyAuth.SchemeName, null);

        builder.Services.AddAuthorization();
    }
}
