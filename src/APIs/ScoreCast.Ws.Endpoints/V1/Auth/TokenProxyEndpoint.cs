using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using ScoreCast.Models.V1.Responses;
using ScoreCast.Models.V1.Responses.Auth;

namespace ScoreCast.Ws.Endpoints.V1.Auth;

public sealed class TokenProxyEndpoint(IConfiguration config, IHttpClientFactory httpClientFactory)
    : Endpoint<TokenProxyRequest, ScoreCastResponse<TokenProxyResult>>
{
    public override void Configure()
    {
        Post("/token");
        Group<AuthGroup>();
        Summary(s =>
        {
            s.Summary = "Token Proxy";
            s.Description = "Proxies token requests to Keycloak (login, refresh)";
        });
    }

    public override async Task HandleAsync(TokenProxyRequest req, CancellationToken ct)
    {
        var authority = config["Keycloak:Authority"]!;
        var clientId = config["Keycloak:WebClientId"] ?? "scorecast-web";
        var tokenUrl = $"{authority}/protocol/openid-connect/token";

        var parameters = new Dictionary<string, string>
        {
            ["grant_type"] = req.GrantType,
            ["client_id"] = clientId
        };

        switch (req.GrantType)
        {
            case "password":
                parameters["username"] = req.Username ?? "";
                parameters["password"] = req.Password ?? "";
                parameters["scope"] = "openid profile email offline_access";
                break;
            case "refresh_token":
                parameters["refresh_token"] = req.RefreshToken ?? "";
                break;
            default:
                await Send.OkAsync(ScoreCastResponse<TokenProxyResult>.Error("Unsupported grant type", "INVALID_GRANT"), ct);
                return;
        }

        var http = httpClientFactory.CreateClient();
        var response = await http.PostAsync(tokenUrl, new FormUrlEncodedContent(parameters), ct);
        var body = await response.Content.ReadAsStringAsync(ct);

        if (!response.IsSuccessStatusCode)
        {
            var errorMsg = "Invalid username or password";
            try
            {
                using var doc = System.Text.Json.JsonDocument.Parse(body);
                if (doc.RootElement.TryGetProperty("error_description", out var desc))
                    errorMsg = desc.GetString() ?? errorMsg;
            }
            catch { /* use default */ }

            await Send.OkAsync(ScoreCastResponse<TokenProxyResult>.Error(errorMsg, "AUTH_FAILED"), ct);
            return;
        }

        await Send.OkAsync(ScoreCastResponse<TokenProxyResult>.Ok(new TokenProxyResult(body), "Token issued"), ct);
    }
}
