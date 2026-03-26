using ScoreCast.Web.Auth;
using ScoreCast.Web.Components.Helpers;
using ScoreCast.Web.Validation;
using ScoreCast.Web.Validation.Auth;
using ScoreCast.Web.ViewModels.Auth;

namespace ScoreCast.Web.Pages;

public partial class Login
{
    [Inject] private ScoreCastAuthStateProvider Auth { get; set; } = null!;
    [Inject] private NavigationManager Nav { get; set; } = null!;
    [Inject] private ILoadingService Loading { get; set; } = null!;

    private readonly LoginViewModel _model = new();
    private readonly Func<object, string, Task<IEnumerable<string>>> _validation = new LoginViewModelValidator().ToMudValidation();
    private MudForm _form = null!;
    private string? _error;
    private string? _success;

    protected override async Task OnInitializedAsync()
    {
        var state = await Auth.GetAuthenticationStateAsync();
        if (state.User.Identity?.IsAuthenticated == true)
            Nav.NavigateTo("/dashboard", replace: true);
    }

    private async Task HandleLogin()
    {
        await _form.ValidateAsync();
        if (!_form.IsValid) return;

        _error = null;
        _success = null;
        AuthResult result = default!;
        await Loading.While(async () =>
            result = await Auth.LoginAsync(_model.Email, _model.Password));

        if (result.Success)
            Nav.NavigateTo(Auth.EmailVerified ? "/dashboard" : "/verify-email", replace: true);
        else
            _error = result.Error;
    }

    private async Task HandleGoogleSignIn()
    {
        _error = null;
        _success = null;
        AuthResult result = default!;
        await Loading.While(async () =>
            result = await Auth.SignInWithGoogleAsync());

        if (result.Success)
            Nav.NavigateTo("/dashboard", replace: true);
        else if (result.Error != "Sign-in cancelled")
            _error = result.Error;
    }

    private async Task HandleForgotPassword()
    {
        _error = null;
        _success = null;

        if (string.IsNullOrWhiteSpace(_model.Email))
        {
            _error = "Enter your email address above, then click Forgot password";
            return;
        }

        var result = await Auth.ResetPasswordAsync(_model.Email);
        if (result.Success)
            _success = "Password reset email sent — check your inbox";
        else
            _error = result.Error;
    }
}
