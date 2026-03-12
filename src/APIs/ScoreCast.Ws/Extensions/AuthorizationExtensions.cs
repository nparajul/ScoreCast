namespace ScoreCast.Ws.Extensions;

public static class AuthorizationExtensions
{
    public static void AddScoreCastAuthorization(this WebApplicationBuilder builder)
    {
        builder.Services.AddAuthorizationBuilder()
            .AddPolicy("RequireAdmin", policy => policy.RequireRole("sc_admin"))
            .AddPolicy("RequireUser", policy => policy.RequireRole("sc_user"));
    }
}
