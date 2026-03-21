using Microsoft.AspNetCore.Components;

namespace ScoreCast.Web.Components.Shared;

public partial class SideDrawer : ComponentBase
{
    [Inject] private NavigationManager Nav { get; set; } = null!;

    [Parameter] public bool IsOpen { get; set; }
    [Parameter] public EventCallback OnClose { get; set; }

    private async Task Close() => await OnClose.InvokeAsync();

    private async Task Navigate(string url)
    {
        await Close();
        Nav.NavigateTo(url);
    }
}
