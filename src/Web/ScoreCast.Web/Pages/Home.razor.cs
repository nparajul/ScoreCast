namespace ScoreCast.Web.Pages;

public partial class Home
{
    [CascadingParameter]
    private Task<AuthenticationState> AuthState { get; set; } = null!;

    [Inject] private NavigationManager Nav { get; set; } = null!;

    protected override async Task OnInitializedAsync()
    {
        var state = await AuthState;
        if (state.User.Identity?.IsAuthenticated == true)
            Nav.NavigateTo("/dashboard", replace: true);
    }
}
