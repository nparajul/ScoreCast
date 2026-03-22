using ScoreCast.Web.Auth;
using ScoreCast.Web.Components.Helpers;
using ScoreCast.Web.Validation;
using ScoreCast.Web.Validation.Auth;
using ScoreCast.Web.ViewModels.Auth;

namespace ScoreCast.Web.Pages;

public partial class Register
{
    [Inject] private ScoreCastAuthStateProvider Auth { get; set; } = null!;
    [Inject] private NavigationManager Nav { get; set; } = null!;
    [Inject] private ILoadingService Loading { get; set; } = null!;

    private readonly RegisterViewModel _model = new();
    private readonly Func<object, string, Task<IEnumerable<string>>> _validation = new RegisterViewModelValidator().ToMudValidation();
    private MudForm _form = null!;
    private string? _error;

    protected override async Task OnInitializedAsync()
    {
        var state = await Auth.GetAuthenticationStateAsync();
        if (state.User.Identity?.IsAuthenticated == true)
            Nav.NavigateTo("/dashboard", replace: true);
    }

    private async Task HandleRegister()
    {
        await _form.ValidateAsync();
        if (!_form.IsValid) return;

        _error = null;
        AuthResult result = default!;
        await Loading.While(async () =>
            result = await Auth.RegisterAsync(_model.Email, _model.Password, _model.Username));

        if (result.Success)
            Nav.NavigateTo("/verify-email", replace: true);
        else
            _error = result.Error;
    }

    private async Task HandleGoogleSignIn()
    {
        _error = null;
        AuthResult result = default!;
        await Loading.While(async () =>
            result = await Auth.SignInWithGoogleAsync());

        if (result.Success)
            Nav.NavigateTo("/dashboard", replace: true);
        else if (result.Error != "Sign-in cancelled")
            _error = result.Error;
    }
}
