using System.Text.Json;
using Microsoft.Extensions.Configuration;
using ScoreCast.Models.V1.Responses;

namespace ScoreCast.Ws.Endpoints.V1.Auth;

public sealed class TokenProxyEndpoint(IConfiguration config, IHttpClientFactory httpClientFactory)
    : Endpoint<TokenProxyRequest, ScoreCastResponse<JsonElement>>
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
        var clientId = config["Keycloak:Audience"]!;
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
                await Send.OkAsync(ScoreCastResponse<JsonElement>.Error("Unsupported grant type", "INVALID_GRANT"), ct);
                return;
        }

        var http = httpClientFactory.CreateClient();
        var response = await http.PostAsync(tokenUrl, new FormUrlEncodedContent(parameters), ct);
        var body = await response.Content.ReadAsStringAsync(ct);

        if (!response.IsSuccessStatusCode)
        {
            await Send.OkAsync(ScoreCastResponse<JsonElement>.Error(body, "AUTH_FAILED"), ct);
            return;
        }

        var tokenData = JsonDocument.Parse(body).RootElement;
        await Send.OkAsync(ScoreCastResponse<JsonElement>.Ok(tokenData, "Token issued"), ct);
    }
}
