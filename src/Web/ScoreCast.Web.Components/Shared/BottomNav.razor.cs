using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Routing;
using MudBlazor;

namespace ScoreCast.Web.Components.Shared;

public partial class BottomNav : ComponentBase, IDisposable
{
    [Inject] private NavigationManager Nav { get; set; } = null!;

    private static readonly (string? Icon, string? Emoji, string Label, string Url, string Color)[] Tabs =
    [
        (Icons.Material.Filled.SportsSoccer, null, "Scores", "/scores", "#4CAF50"),
        (null, "🏆", "Predict", "/dashboard", ""),
        (Icons.Material.Filled.Leaderboard, null, "Tables", "/points-table", "#42A5F5"),
        (null, "🛡️", "Teams", "/teams", ""),
        (Icons.Material.Filled.MoreHoriz, null, "More", "/settings", "#BDBDBD"),
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

    private void OnLocationChanged(object? sender, LocationChangedEventArgs e)
    {
        UpdatePath();
        StateHasChanged();
    }

    private void UpdatePath() =>
        _currentPath = "/" + Nav.ToBaseRelativePath(Nav.Uri).TrimEnd('/');

    public void Dispose() => Nav.LocationChanged -= OnLocationChanged;
}
