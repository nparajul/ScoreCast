using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text.Json;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Components.WebAssembly.Http;
using Microsoft.JSInterop;

using ScoreCast.Shared.Types;
using ScoreCast.Web.Components.Helpers;

namespace ScoreCast.Web.Auth;

public sealed class ScoreCastAuthStateProvider(
    HttpClient http,
    IJSRuntime js,
    IConfiguration config) : AuthenticationStateProvider, IAuthService
{
    private const string AccessTokenKey = "sc_access_token";
    private const string RefreshTokenKey = "sc_refresh_token";

    private ClaimsPrincipal _currentUser = new(new ClaimsIdentity());
    private string? _accessToken;
    private DateTimeOffset _tokenExpiry;

    private string TokenEndpoint => $"{config["Keycloak:Authority"]}/protocol/openid-connect/token";
    private string ClientId => config["Keycloak:ClientId"]!;

    public override async Task<AuthenticationState> GetAuthenticationStateAsync()
    {
        if (_accessToken is null)
        {
            _accessToken = await GetFromStorage(AccessTokenKey);

            if (_accessToken is not null && !IsTokenValid(_accessToken))
            {
                var refreshToken = await GetFromStorage(RefreshTokenKey);
                if (refreshToken is null || !await RefreshTokenAsync(refreshToken))
                {
                    await ClearTokens();
                    return new AuthenticationState(_currentUser);
                }
            }

            if (_accessToken is not null)
                _currentUser = CreateClaimsPrincipal(_accessToken);
        }

        return new AuthenticationState(_currentUser);
    }

    public async Task<AuthResult> LoginAsync(string username, string password)
    {
        var content = new FormUrlEncodedContent(new Dictionary<string, string>
        {
            ["grant_type"] = "password",
            ["client_id"] = ClientId,
            ["username"] = username,
            ["password"] = password,
            ["scope"] = "openid profile email offline_access"
        });

        var request = new HttpRequestMessage(HttpMethod.Post, TokenEndpoint) { Content = content };
        request.SetBrowserRequestMode(BrowserRequestMode.Cors);
        request.SetBrowserRequestCredentials(BrowserRequestCredentials.Omit);

        var response = await http.SendAsync(request);
        if (!response.IsSuccessStatusCode)
        {
            var error = await response.Content.ReadAsStringAsync();
            var errorDesc = "Invalid username or password";
            try
            {
                using var doc = JsonDocument.Parse(error);
                if (doc.RootElement.TryGetProperty("error_description", out var desc))
                    errorDesc = desc.GetString() ?? errorDesc;
            }
            catch { /* use default */ }

            return new AuthResult(false, errorDesc);
        }

        await ApplyTokenResponse(response);
        NotifyAuthenticationStateChanged(GetAuthenticationStateAsync());
        return new AuthResult(true);
    }

    public async Task LogoutAsync()
    {
        var refreshToken = await GetFromStorage(RefreshTokenKey);
        if (refreshToken is not null)
        {
            var logoutEndpoint = $"{config["Keycloak:Authority"]}/protocol/openid-connect/logout";
            var content = new FormUrlEncodedContent(new Dictionary<string, string>
            {
                ["client_id"] = ClientId,
                ["refresh_token"] = refreshToken
            });
            try
            {
                var request = new HttpRequestMessage(HttpMethod.Post, logoutEndpoint) { Content = content };
                request.SetBrowserRequestMode(BrowserRequestMode.Cors);
                request.SetBrowserRequestCredentials(BrowserRequestCredentials.Omit);
                await http.SendAsync(request);
            }
            catch { /* best effort */ }
        }

        await ClearTokens();
        _currentUser = new ClaimsPrincipal(new ClaimsIdentity());
        NotifyAuthenticationStateChanged(GetAuthenticationStateAsync());
    }

    public string GetRegistrationUrl(string returnUrl)
    {
        var authority = config["Keycloak:Authority"]!;
        return $"{authority}/protocol/openid-connect/registrations" +
               $"?client_id={ClientId}" +
               "&response_type=code" +
               "&scope=openid" +
               $"&redirect_uri={Uri.EscapeDataString(returnUrl)}";
    }

    public async Task<string?> GetAccessTokenAsync()
    {
        if (_accessToken is not null && _tokenExpiry > new DateTimeOffset(ScoreCastDateTime.Now.Value, TimeSpan.Zero).AddSeconds(30))
            return _accessToken;

        var refreshToken = await GetFromStorage(RefreshTokenKey);
        if (refreshToken is not null && await RefreshTokenAsync(refreshToken))
            return _accessToken;

        return null;
    }

    private async Task<bool> RefreshTokenAsync(string refreshToken)
    {
        var content = new FormUrlEncodedContent(new Dictionary<string, string>
        {
            ["grant_type"] = "refresh_token",
            ["client_id"] = ClientId,
            ["refresh_token"] = refreshToken
        });

        var request = new HttpRequestMessage(HttpMethod.Post, TokenEndpoint) { Content = content };
        request.SetBrowserRequestMode(BrowserRequestMode.Cors);
        request.SetBrowserRequestCredentials(BrowserRequestCredentials.Omit);

        var response = await http.SendAsync(request);
        if (!response.IsSuccessStatusCode) return false;

        await ApplyTokenResponse(response);
        return true;
    }

    private async Task ApplyTokenResponse(HttpResponseMessage response)
    {
        var json = await response.Content.ReadAsStringAsync();
        using var doc = JsonDocument.Parse(json);
        var accessToken = doc.RootElement.GetProperty("access_token").GetString()!;
        var refreshToken = doc.RootElement.GetProperty("refresh_token").GetString()!;

        _accessToken = accessToken;
        var handler = new JwtSecurityTokenHandler();
        _tokenExpiry = handler.ReadJwtToken(accessToken).ValidTo;
        _currentUser = CreateClaimsPrincipal(accessToken);

        await js.InvokeVoidAsync("localStorage.setItem", AccessTokenKey, accessToken);
        await js.InvokeVoidAsync("localStorage.setItem", RefreshTokenKey, refreshToken);
    }

    private static ClaimsPrincipal CreateClaimsPrincipal(string accessToken)
    {
        var handler = new JwtSecurityTokenHandler();
        var jwt = handler.ReadJwtToken(accessToken);
        var identity = new ClaimsIdentity(jwt.Claims, "jwt");
        return new ClaimsPrincipal(identity);
    }

    private bool IsTokenValid(string token)
    {
        try
        {
            var handler = new JwtSecurityTokenHandler();
            var jwt = handler.ReadJwtToken(token);
            _tokenExpiry = jwt.ValidTo;
            return jwt.ValidTo > ScoreCastDateTime.Now.Value.AddSeconds(30);
        }
        catch { return false; }
    }

    private async Task ClearTokens()
    {
        _accessToken = null;
        _tokenExpiry = default;
        await js.InvokeVoidAsync("localStorage.removeItem", AccessTokenKey);
        await js.InvokeVoidAsync("localStorage.removeItem", RefreshTokenKey);
    }

    private async Task<string?> GetFromStorage(string key)
    {
        try { return await js.InvokeAsync<string?>("localStorage.getItem", key); }
        catch { return null; }
    }
}
