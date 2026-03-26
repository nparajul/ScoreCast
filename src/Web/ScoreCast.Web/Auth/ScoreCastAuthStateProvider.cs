using System.Security.Claims;
using System.Text.Json;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.JSInterop;

namespace ScoreCast.Web.Auth;

public sealed class ScoreCastAuthStateProvider : AuthenticationStateProvider, IAsyncDisposable
{
    private readonly IJSRuntime _js;
    private readonly TaskCompletionSource<bool> _initialized = new();
    private DotNetObjectReference<ScoreCastAuthStateProvider>? _dotNetRef;
    private ClaimsPrincipal _currentUser = new(new ClaimsIdentity());
    private FirebaseUser? _firebaseUser;

    public ScoreCastAuthStateProvider(IJSRuntime js) => _js = js;

    public async Task InitializeAsync(string configJson)
    {
        _dotNetRef = DotNetObjectReference.Create(this);
        var config = JsonSerializer.Deserialize<JsonElement>(configJson);
        await _js.InvokeVoidAsync("firebaseAuth.init", config, _dotNetRef);
    }

    [JSInvokable]
    public void OnAuthStateChanged(FirebaseUser? user)
    {
        _firebaseUser = user;
        _currentUser = user is not null
            ? BuildPrincipal(user)
            : new ClaimsPrincipal(new ClaimsIdentity());

        if (!_initialized.Task.IsCompleted)
            _initialized.SetResult(true);

        NotifyAuthenticationStateChanged(Task.FromResult(new AuthenticationState(_currentUser)));
    }

    public override async Task<AuthenticationState> GetAuthenticationStateAsync()
    {
        await _initialized.Task;
        return new AuthenticationState(_currentUser);
    }

    public async Task<AuthResult> LoginAsync(string email, string password)
    {
        var result = await _js.InvokeAsync<FirebaseAuthResult>("firebaseAuth.signInWithEmail", email, password);
        return result.Success ? new AuthResult(true) : new AuthResult(false, result.Error);
    }

    public async Task<AuthResult> RegisterAsync(string email, string password, string? displayName = null)
    {
        var result = await _js.InvokeAsync<FirebaseAuthResult>("firebaseAuth.registerWithEmail", email, password, displayName);
        return result.Success ? new AuthResult(true) : new AuthResult(false, result.Error);
    }

    public async Task<AuthResult> SignInWithGoogleAsync()
    {
        var result = await _js.InvokeAsync<FirebaseAuthResult>("firebaseAuth.signInWithGoogle");
        return result.Success ? new AuthResult(true) : new AuthResult(false, result.Error);
    }

    public async Task LogoutAsync() => await _js.InvokeVoidAsync("firebaseAuth.signOut");

    public bool EmailVerified => _firebaseUser?.EmailVerified ?? false;

    public bool IsGoogleUser => _firebaseUser?.IsGoogleUser ?? false;

    public async Task<AuthResult> ResendVerificationAsync()
    {
        var result = await _js.InvokeAsync<FirebaseAuthResult>("firebaseAuth.resendVerification");
        return result.Success ? new AuthResult(true) : new AuthResult(false, result.Error);
    }

    public async Task<bool> ReloadUserAsync() =>
        await _js.InvokeAsync<bool>("firebaseAuth.reloadUser");

    public async Task<string?> GetIdTokenAsync() =>
        await _js.InvokeAsync<string?>("firebaseAuth.getIdToken");

    public async Task<AuthResult> ResetPasswordAsync(string email)
    {
        var result = await _js.InvokeAsync<FirebaseAuthResult>("firebaseAuth.resetPassword", email);
        return result.Success ? new AuthResult(true) : new AuthResult(false, result.Error);
    }

    private static ClaimsPrincipal BuildPrincipal(FirebaseUser user)
    {
        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, user.Uid),
            new("sub", user.Uid),
            new("email_verified", user.EmailVerified.ToString().ToLowerInvariant()),
            new("is_google_user", user.IsGoogleUser.ToString().ToLowerInvariant())
        };

        if (user.Email is not null)
            claims.Add(new Claim(ClaimTypes.Email, user.Email));
        if (user.DisplayName is not null)
            claims.Add(new Claim(ClaimTypes.Name, user.DisplayName));

        return new ClaimsPrincipal(new ClaimsIdentity(claims, "firebase"));
    }

    public ValueTask DisposeAsync()
    {
        _dotNetRef?.Dispose();
        return ValueTask.CompletedTask;
    }
}

public sealed record FirebaseUser(string Uid, string? Email, string? DisplayName, bool EmailVerified = false, bool IsGoogleUser = false);
public sealed record FirebaseAuthResult(bool Success, string? Uid = null, string? Error = null, bool EmailVerified = false);
public sealed record AuthResult(bool Success, string? Error = null);
