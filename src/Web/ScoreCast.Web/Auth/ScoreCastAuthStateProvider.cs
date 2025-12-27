using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text.Json;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.JSInterop;

using ScoreCast.Shared.Types;
using ScoreCast.Web.Components.Helpers;

namespace ScoreCast.Web.Auth;

public sealed class ScoreCastAuthStateProvider(
    IAuthApi authApi,
    IJSRuntime js,
    IConfiguration config) : AuthenticationStateProvider, IAuthService
{
    private const string AccessTokenKey = "sc_access_token";
    private const string RefreshTokenKey = "sc_refresh_token";

    private ClaimsPrincipal _currentUser = new(new ClaimsIdentity());
    private string? _accessToken;
    private DateTimeOffset _tokenExpiry;

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
        var response = await PostTokenRequest(new
        {
            grantType = "password",
            username,
            password
        });

        if (!response.Success)
            return new AuthResult(false, response.Message ?? "Invalid username or password");

        await ApplyTokenData(response.TokenJson);
        NotifyAuthenticationStateChanged(GetAuthenticationStateAsync());
        return new AuthResult(true);
    }

    public async Task LogoutAsync()
    {
        var refreshToken = await GetFromStorage(RefreshTokenKey);
        if (refreshToken is not null)
        {
            try
            {
                await PostTokenRequest(new
                {
                    grantType = "refresh_token",
                    refreshToken
                });
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
        var clientId = config["Keycloak:ClientId"]!;
        return $"{authority}/protocol/openid-connect/registrations" +
               $"?client_id={clientId}" +
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
        var response = await PostTokenRequest(new
        {
            grantType = "refresh_token",
            refreshToken
        });

        if (!response.Success) return false;

        await ApplyTokenData(response.TokenJson);
        return true;
    }

    private async Task<TokenApiResponse> PostTokenRequest(object body)
    {
        var result = await authApi.TokenAsync(body);
        return new TokenApiResponse(result.Success, result.Message, result.Data?.TokenJson);
    }

    private async Task ApplyTokenData(string? tokenJson)
    {
        if (tokenJson is null) return;

        using var doc = JsonDocument.Parse(tokenJson);
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

    private sealed record TokenApiResponse(bool Success, string? Message, string? TokenJson);
}
