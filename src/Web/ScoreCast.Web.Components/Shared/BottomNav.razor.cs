using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Routing;
using MudBlazor;

namespace ScoreCast.Web.Components.Shared;

public partial class BottomNav : ComponentBase, IDisposable
{
    [Inject] private NavigationManager Nav { get; set; } = null!;

    private static readonly (string? Icon, string? Emoji, string Label, string Url, string Color, bool IsDrawer)[] Tabs =
    [
        (null, "🏆", "Predict", "/dashboard", "", false),
        (Icons.Material.Filled.Public, null, "Global", "/global", "#FF6B35", false),
        (Icons.Material.Filled.SportsSoccer, null, "Scores", "/scores", "#4CAF50", false),
        (Icons.Material.Filled.Leaderboard, null, "Tables", "/points-table", "#42A5F5", false),
        (Icons.Material.Filled.Menu, null, "More", "", "#BDBDBD", true),
    ];

    private string _currentPath = "";
    private bool _menuOpen;

    protected override void OnInitialized()
    {
        UpdatePath();
        Nav.LocationChanged += OnLocationChanged;
    }

    private string TabClass(string url) =>
        !string.IsNullOrEmpty(url) && _currentPath.StartsWith(url, StringComparison.OrdinalIgnoreCase)
            ? "bottom-nav-tab active"
            : "bottom-nav-tab";

    private void OnTabClick((string? Icon, string? Emoji, string Label, string Url, string Color, bool IsDrawer) tab)
    {
        if (tab.IsDrawer)
        {
            _menuOpen = !_menuOpen;
        }
        else
        {
            _menuOpen = false;
            Nav.NavigateTo(tab.Url);
        }
    }

    private void Navigate(string url)
    {
        _menuOpen = false;
        Nav.NavigateTo(url);
    }

    private void OnLocationChanged(object? sender, LocationChangedEventArgs e)
    {
        _menuOpen = false;
        UpdatePath();
        StateHasChanged();
    }

    private void UpdatePath() =>
        _currentPath = "/" + Nav.ToBaseRelativePath(Nav.Uri).TrimEnd('/');

    public void Dispose() => Nav.LocationChanged -= OnLocationChanged;
}
