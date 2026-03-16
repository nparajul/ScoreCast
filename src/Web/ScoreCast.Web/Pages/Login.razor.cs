using ScoreCast.Web.Auth;

namespace ScoreCast.Web.Pages;

public partial class Login
{
    [Inject] private ScoreCastAuthStateProvider Auth { get; set; } = null!;
    [Inject] private NavigationManager Nav { get; set; } = null!;

    private readonly LoginModel _model = new();
    private string? _error;
    private bool _loading;

    protected override async Task OnInitializedAsync()
    {
        var state = await Auth.GetAuthenticationStateAsync();
        if (state.User.Identity?.IsAuthenticated == true)
            Nav.NavigateTo("/dashboard", replace: true);
    }

    private async Task HandleLogin()
    {
        _error = null;
        _loading = true;

        var result = await Auth.LoginAsync(_model.Username, _model.Password);

        if (result.Success)
            Nav.NavigateTo("/dashboard", replace: true);
        else
            _error = result.Error;

        _loading = false;
    }

    private void GoToRegister()
    {
        var returnUrl = Nav.BaseUri + "login";
        var url = Auth.GetRegistrationUrl(returnUrl);
        Nav.NavigateTo(url, forceLoad: true);
    }

    private sealed class LoginModel
    {
        public string Username { get; set; } = "";
        public string Password { get; set; } = "";
    }
}
