using Microsoft.AspNetCore.Components;

namespace ScoreCast.Web.Components.Shared;

public partial class LoginDisplay
{
    [Parameter] public string? Class { get; set; }
    [Inject] private NavigationManager Navigation { get; set; } = null!;

    private void BeginLogout() =>
        Navigation.NavigateTo("/logout", replace: true);
}
