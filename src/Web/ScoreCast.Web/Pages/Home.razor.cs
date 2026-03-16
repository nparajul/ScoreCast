namespace ScoreCast.Web.Pages;

public partial class Home
{
    [CascadingParameter]
    private Task<AuthenticationState> AuthState { get; set; } = null!;

    [Inject] private NavigationManager Nav { get; set; } = null!;

    private bool _isAuthenticated;

    protected override async Task OnInitializedAsync()
    {
        var state = await AuthState;
        _isAuthenticated = state.User.Identity?.IsAuthenticated == true;
        if (_isAuthenticated)
            Nav.NavigateTo("/dashboard", replace: true);
    }
}
