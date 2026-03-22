using ScoreCast.Web.Auth;

namespace ScoreCast.Web.Pages;

public partial class VerifyEmail : IDisposable
{
    [Inject] private ScoreCastAuthStateProvider Auth { get; set; } = null!;
    [Inject] private NavigationManager Nav { get; set; } = null!;

    private string? _message;
    private Severity _severity;
    private int _cooldown;
    private Timer? _timer;

    protected override async Task OnInitializedAsync()
    {
        var state = await Auth.GetAuthenticationStateAsync();
        if (state.User.Identity?.IsAuthenticated != true)
            Nav.NavigateTo("/login", replace: true);
        else if (Auth.EmailVerified)
            Nav.NavigateTo("/dashboard", replace: true);
    }

    private async Task CheckVerification()
    {
        var verified = await Auth.ReloadUserAsync();
        if (verified)
            Nav.NavigateTo("/dashboard", replace: true);
        else
        {
            _message = "Email not verified yet. Please check your inbox and spam folder.";
            _severity = Severity.Warning;
        }
    }

    private async Task ResendEmail()
    {
        var result = await Auth.ResendVerificationAsync();
        if (result.Success)
        {
            _message = "Verification email sent!";
            _severity = Severity.Success;
            _cooldown = 60;
            _timer = new Timer(_ =>
            {
                _cooldown--;
                if (_cooldown <= 0) _timer?.Dispose();
                InvokeAsync(StateHasChanged);
            }, null, 1000, 1000);
        }
        else
        {
            _message = result.Error ?? "Failed to send email. Try again later.";
            _severity = Severity.Error;
        }
    }

    private async Task SignOut()
    {
        await Auth.LogoutAsync();
        Nav.NavigateTo("/", replace: true);
    }

    public void Dispose() => _timer?.Dispose();
}
