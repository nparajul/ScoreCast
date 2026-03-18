using Microsoft.AspNetCore.Components;

namespace ScoreCast.Web.Components.Shared;

public partial class RedirectToLogin
{
    [Inject] private NavigationManager Navigation { get; set; } = null!;

    protected override void OnInitialized() =>
        Navigation.NavigateTo("/login", replace: true);
}
