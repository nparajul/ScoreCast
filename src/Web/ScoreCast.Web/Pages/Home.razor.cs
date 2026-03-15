namespace ScoreCast.Web.Pages;

public partial class Home
{
    [CascadingParameter]
    private Task<AuthenticationState> AuthState { get; set; } = default!;

    [Inject] private NavigationManager Nav { get; set; } = default!;

    protected override async Task OnInitializedAsync()
    {
        var state = await AuthState;
        if (state.User.Identity?.IsAuthenticated == true)
            Nav.NavigateTo("/leagues", replace: true);
    }
}
