using ScoreCast.Web.Auth;

namespace ScoreCast.Web.Pages;

public partial class Logout
{
    [Inject] private ScoreCastAuthStateProvider Auth { get; set; } = null!;
    [Inject] private NavigationManager Nav { get; set; } = null!;

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (!firstRender) return;
        await Auth.LogoutAsync();
        Nav.NavigateTo("/", replace: true);
    }
}
