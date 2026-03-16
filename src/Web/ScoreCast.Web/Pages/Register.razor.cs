using ScoreCast.Web.Auth;

namespace ScoreCast.Web.Pages;

public partial class Register
{
    [Inject] private ScoreCastAuthStateProvider Auth { get; set; } = null!;
    [Inject] private NavigationManager Nav { get; set; } = null!;

    private readonly RegisterModel _model = new();
    private string? _error;
    private bool _loading;

    protected override async Task OnInitializedAsync()
    {
        var state = await Auth.GetAuthenticationStateAsync();
        if (state.User.Identity?.IsAuthenticated == true)
            Nav.NavigateTo("/dashboard", replace: true);
    }

    private async Task HandleRegister()
    {
        _error = null;
        _loading = true;

        var (success, error) = await Auth.RegisterAsync(_model.Email, _model.Username, _model.Password);

        if (success)
            Nav.NavigateTo("/dashboard", replace: true);
        else
            _error = error;

        _loading = false;
    }

    private sealed class RegisterModel
    {
        public string Email { get; set; } = "";
        public string Username { get; set; } = "";
        public string Password { get; set; } = "";
    }
}
