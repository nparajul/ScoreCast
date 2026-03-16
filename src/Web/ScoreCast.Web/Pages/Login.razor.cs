using ScoreCast.Web.Auth;
using ScoreCast.Web.Components.Helpers;

namespace ScoreCast.Web.Pages;

public partial class Login
{
    [Inject] private ScoreCastAuthStateProvider Auth { get; set; } = null!;
    [Inject] private NavigationManager Nav { get; set; } = null!;
    [Inject] private ILoadingService Loading { get; set; } = null!;

    private readonly LoginModel _model = new();
    private string? _error;

    protected override async Task OnInitializedAsync()
    {
        var state = await Auth.GetAuthenticationStateAsync();
        if (state.User.Identity?.IsAuthenticated == true)
            Nav.NavigateTo("/dashboard", replace: true);
    }

    private async Task HandleLogin()
    {
        _error = null;

        AuthResult result = default!;
        await Loading.While(async () =>
            result = await Auth.LoginAsync(_model.Username, _model.Password));

        if (result.Success)
            Nav.NavigateTo("/dashboard", replace: true);
        else
            _error = result.Error;
    }

    private sealed class LoginModel
    {
        public string Username { get; set; } = "";
        public string Password { get; set; } = "";
    }
}
