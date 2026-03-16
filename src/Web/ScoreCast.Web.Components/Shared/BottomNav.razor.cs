using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Routing;
using MudBlazor;

namespace ScoreCast.Web.Components.Shared;

public partial class BottomNav : ComponentBase, IDisposable
{
    [Inject] private NavigationManager Nav { get; set; } = null!;

    private static readonly (string Icon, string Label, string Url)[] Tabs =
    [
        (Icons.Material.Filled.SportsSoccer, "Scores", "/scores"),
        (Icons.Material.Filled.Edit, "Predict", "/predict"),
        (Icons.Material.Filled.EmojiEvents, "Leagues", "/dashboard"),
        (Icons.Material.Filled.Leaderboard, "Tables", "/points-table"),
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

    private void Navigate(string url) => Nav.NavigateTo(url);

    private void OnLocationChanged(object? sender, LocationChangedEventArgs e)
    {
        UpdatePath();
        StateHasChanged();
    }

    private void UpdatePath() =>
        _currentPath = "/" + Nav.ToBaseRelativePath(Nav.Uri).TrimEnd('/');

    public void Dispose() => Nav.LocationChanged -= OnLocationChanged;
}
