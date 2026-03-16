using System.Text.RegularExpressions;
using ScoreCast.Web.Auth;
using ScoreCast.Web.Components.Helpers;

namespace ScoreCast.Web.Pages;

public partial class Register
{
    [Inject] private ScoreCastAuthStateProvider Auth { get; set; } = null!;
    [Inject] private NavigationManager Nav { get; set; } = null!;
    [Inject] private ILoadingService Loading { get; set; } = null!;

    private readonly RegisterModel _model = new();
    private string? _error;

    protected override async Task OnInitializedAsync()
    {
        var state = await Auth.GetAuthenticationStateAsync();
        if (state.User.Identity?.IsAuthenticated == true)
            Nav.NavigateTo("/dashboard", replace: true);
    }

    private async Task HandleRegister()
    {
        _error = Validate();
        if (_error is not null) return;

        (bool success, string? error) result = default;
        await Loading.While(async () =>
            result = await Auth.RegisterAsync(_model.Email, _model.Username, _model.Password));

        if (result.success)
            Nav.NavigateTo("/dashboard", replace: true);
        else
            _error = result.error;
    }

    private string? Validate()
    {
        if (_model.Password != _model.ConfirmPassword)
            return "Passwords do not match";
        if (_model.Password.Length < 8)
            return "Password must be at least 8 characters";
        if (_model.Password.Length > 128)
            return "Password must be 128 characters or less";
        if (!Regex.IsMatch(_model.Password, "[A-Z]"))
            return "Password must contain at least 1 uppercase letter";
        if (!Regex.IsMatch(_model.Password, "[a-z]"))
            return "Password must contain at least 1 lowercase letter";
        if (!Regex.IsMatch(_model.Password, "[0-9]"))
            return "Password must contain at least 1 digit";
        if (!Regex.IsMatch(_model.Password, @"[^a-zA-Z0-9]"))
            return "Password must contain at least 1 special character";
        if (_model.Password.Contains(_model.Username, StringComparison.OrdinalIgnoreCase))
            return "Password cannot contain your username";
        if (_model.Password.Contains(_model.Email, StringComparison.OrdinalIgnoreCase))
            return "Password cannot contain your email";
        return null;
    }

    private sealed class RegisterModel
    {
        public string Email { get; set; } = "";
        public string Username { get; set; } = "";
        public string Password { get; set; } = "";
        public string ConfirmPassword { get; set; } = "";
    }
}
