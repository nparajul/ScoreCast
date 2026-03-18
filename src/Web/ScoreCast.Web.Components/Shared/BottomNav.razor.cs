using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Routing;
using Microsoft.JSInterop;
using MudBlazor;

namespace ScoreCast.Web.Components.Shared;

public partial class BottomNav : ComponentBase, IDisposable
{
    [Inject] private NavigationManager Nav { get; set; } = null!;
    [Inject] private IJSRuntime Js { get; set; } = null!;

    private ElementReference _scrollContainer;

    private static readonly (string Icon, string? Emoji, string Label, string Url)[] Tabs =
    [
        (Icons.Material.Filled.SportsSoccer, null, "Scores", "/scores"),
        (null!, "🏆", "Predict", "/dashboard"),
        (Icons.Material.Filled.Leaderboard, null, "Tables", "/points-table"),
        (Icons.Material.Filled.AutoAwesome, null, "Insights", "/insights"),
        (null!, "🛡️", "Teams", "/teams"),
        (Icons.Material.Filled.People, null, "Players", "/player-stats"),
        (Icons.Material.Filled.Settings, null, "Settings", "/settings"),
    ];

    private string _currentPath = "";

    protected override void OnInitialized()
    {
        UpdatePath();
        Nav.LocationChanged += OnLocationChanged;
    }

    private string TabClass(string url) =>
        _currentPath.StartsWith(url, StringComparison.OrdinalIgnoreCase)
            ? "bottom-nav-tab active"
            : "bottom-nav-tab";

    private async Task OnTabClick(string url, int index)
    {
        Nav.NavigateTo(url);
        await Js.InvokeVoidAsync("document.getElementById", $"bnav-{index}");
        await Js.InvokeVoidAsync("eval",
            $"document.getElementById('bnav-{index}')?.scrollIntoView({{behavior:'smooth',inline:'center',block:'nearest'}})");
    }

    private void OnLocationChanged(object? sender, LocationChangedEventArgs e)
    {
        UpdatePath();
        StateHasChanged();
    }

    private void UpdatePath() =>
        _currentPath = "/" + Nav.ToBaseRelativePath(Nav.Uri).TrimEnd('/');

    public void Dispose() => Nav.LocationChanged -= OnLocationChanged;
}
