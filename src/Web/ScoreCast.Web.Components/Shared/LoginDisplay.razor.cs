using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.WebAssembly.Authentication;

namespace ScoreCast.Web.Components.Shared;

public partial class LoginDisplay
{
    [Inject] private NavigationManager Navigation { get; set; } = null!;

    private void BeginLogout() => Navigation.NavigateToLogout("authentication/logout");
}
