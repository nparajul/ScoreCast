using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using ScoreCast.Models.V1.Requests.Auth;
using ScoreCast.Models.V1.Responses;
using ScoreCast.Models.V1.Responses.Auth;

namespace ScoreCast.Ws.Endpoints.V1.Auth;

public sealed class RegisterEndpoint(IConfiguration config, IHttpClientFactory httpClientFactory)
    : Endpoint<RegisterRequest, ScoreCastResponse<TokenProxyResult>>
{
    public override void Configure()
    {
        Post("/register");
        Group<AuthGroup>();
        Summary(s =>
        {
            s.Summary = "Register";
            s.Description = "Creates a user in Keycloak and returns tokens";
        });
    }

    public override async Task HandleAsync(RegisterRequest req, CancellationToken ct)
    {
        var authority = config["Keycloak:Authority"]!;
        var clientId = config["Keycloak:WebClientId"] ?? "scorecast-web";
        var http = httpClientFactory.CreateClient();

        // Step 1: Get admin token
        var adminToken = await GetAdminToken(http, authority, ct);
        if (adminToken is null)
        {
            await Send.OkAsync(ScoreCastResponse<TokenProxyResult>.Error("Registration service unavailable", "REG_FAILED"), ct);
            return;
        }

        // Step 2: Create user in Keycloak
        var createResult = await CreateKeycloakUser(http, authority, adminToken, req, ct);
        if (createResult is not null)
        {
            await Send.OkAsync(ScoreCastResponse<TokenProxyResult>.Error(createResult, "REG_FAILED"), ct);
            return;
        }

        // Step 3: Login with new credentials
        var tokenUrl = $"{authority}/protocol/openid-connect/token";
        var loginParams = new FormUrlEncodedContent(new Dictionary<string, string>
        {
            ["grant_type"] = "password",
            ["client_id"] = clientId,
            ["username"] = req.Username,
            ["password"] = req.Password,
            ["scope"] = "openid profile email offline_access"
        });

        var loginResponse = await http.PostAsync(tokenUrl, loginParams, ct);
        var body = await loginResponse.Content.ReadAsStringAsync(ct);

        if (!loginResponse.IsSuccessStatusCode)
        {
            await Send.OkAsync(ScoreCastResponse<TokenProxyResult>.Error("Account created but login failed. Please log in manually.", "REG_LOGIN_FAILED"), ct);
            return;
        }

        await Send.OkAsync(ScoreCastResponse<TokenProxyResult>.Ok(new TokenProxyResult(body), "Registration successful"), ct);
    }

    private async Task<string?> GetAdminToken(HttpClient http, string authority, CancellationToken ct)
    {
        var adminClientId = config["Keycloak:AdminClientId"] ?? "admin-cli";
        var adminUsername = config["Keycloak:AdminUsername"];
        var adminPassword = config["Keycloak:AdminPassword"];

        if (adminUsername is null || adminPassword is null) return null;

        var masterTokenUrl = authority + "/protocol/openid-connect/token";

        var response = await http.PostAsync(masterTokenUrl, new FormUrlEncodedContent(new Dictionary<string, string>
        {
            ["grant_type"] = "password",
            ["client_id"] = adminClientId,
            ["username"] = adminUsername,
            ["password"] = adminPassword
        }), ct);

        if (!response.IsSuccessStatusCode) return null;

        using var doc = JsonDocument.Parse(await response.Content.ReadAsStringAsync(ct));
        return doc.RootElement.GetProperty("access_token").GetString();
    }

    private static async Task<string?> CreateKeycloakUser(HttpClient http, string authority, string adminToken, RegisterRequest req, CancellationToken ct)
    {
        var usersUrl = authority.Replace("/realms/", "/admin/realms/") + "/users";

        var userPayload = JsonSerializer.Serialize(new
        {
            username = req.Username,
            email = req.Email,
            enabled = true,
            emailVerified = true,
            credentials = new[] { new { type = "password", value = req.Password, temporary = false } }
        });

        var request = new HttpRequestMessage(System.Net.Http.HttpMethod.Post, usersUrl);
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", adminToken);
        request.Content = new StringContent(userPayload, Encoding.UTF8, "application/json");

        var response = await http.SendAsync(request, ct);

        if (response.StatusCode == System.Net.HttpStatusCode.Conflict)
            return "A user with this username or email already exists";

        if (!response.IsSuccessStatusCode)
        {
            var error = await response.Content.ReadAsStringAsync(ct);
            try
            {
                using var doc = JsonDocument.Parse(error);
                return doc.RootElement.TryGetProperty("errorMessage", out var msg)
                    ? msg.GetString() : "Registration failed";
            }
            catch { return "Registration failed"; }
        }

        return null; // success
    }
}
