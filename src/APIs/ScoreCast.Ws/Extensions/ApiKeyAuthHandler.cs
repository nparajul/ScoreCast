using System.Security.Claims;
using System.Text.Encodings.Web;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Options;
using ScoreCast.Shared.Constants;

namespace ScoreCast.Ws.Extensions;

public sealed class ApiKeyAuthHandler(
    IOptionsMonitor<AuthenticationSchemeOptions> options,
    ILoggerFactory logger,
    UrlEncoder encoder,
    IConfiguration configuration)
    : AuthenticationHandler<AuthenticationSchemeOptions>(options, logger, encoder)
{
    protected override Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        if (!Request.Headers.TryGetValue(ApiKeyAuth.HeaderName, out var providedKey))
            return Task.FromResult(AuthenticateResult.NoResult());

        var clients = configuration.GetSection("ApiKeySettings:Clients").GetChildren();

        foreach (var client in clients)
        {
            var key = client["Key"];
            if (string.IsNullOrEmpty(key) || key != providedKey) continue;

            var name = client["Name"] ?? "Unknown";
            var identity = new ClaimsIdentity(
            [
                new Claim(ClaimTypes.Name, name),
                new Claim(ClaimTypes.Role, "sc_admin")
            ], Scheme.Name);

            return Task.FromResult(AuthenticateResult.Success(
                new AuthenticationTicket(new ClaimsPrincipal(identity), Scheme.Name)));
        }

        return Task.FromResult(AuthenticateResult.Fail("Invalid API key"));
    }
}
