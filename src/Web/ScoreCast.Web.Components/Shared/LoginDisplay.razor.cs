using Microsoft.AspNetCore.Components;
using ScoreCast.Web.Components.Helpers;

namespace ScoreCast.Web.Components.Shared;

public partial class LoginDisplay
{
    [Parameter] public string? Class { get; set; }
    [Inject] private NavigationManager Navigation { get; set; } = null!;
    [Inject] private IAuthService Auth { get; set; } = null!;

    private async Task BeginLogout()
    {
        await Auth.LogoutAsync();
        Navigation.NavigateTo("/", replace: true);
    }
}
