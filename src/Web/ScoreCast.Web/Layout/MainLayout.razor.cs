using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using Microsoft.JSInterop;

namespace ScoreCast.Web.Layout;

public partial class MainLayout : IDisposable
{
    [Inject] private NavigationManager Nav { get; set; } = null!;
    [Inject] private IJSRuntime Js { get; set; } = null!;

    private bool _drawerOpen = false;
    private bool _isMobile = false;
    private bool _searchOpen;
    private long _selectedRoleId;
    private bool _showBackButton;

    private static readonly string[] RootPaths = ["/", "/leagues", "/dashboard", "/settings", ""];

    private string? WrapperClass { get; set; }

    private void HandleMenuOpen(bool isOpen)
    {
        WrapperClass = isOpen ? "scroll-lock" : "";
        StateHasChanged();
    }

    public string GetEnvClass()
    {
        if (HostEnvironment.IsProduction())
            return string.Empty;
        if (HostEnvironment.IsStaging())
            return "staging";
        return "development";
    }

    protected override async Task OnInitializedAsync()
    {
        Notify.Register(this);
        Loading.IsLoading = true;
        RoleNav.OnChanged += StateHasChanged;
        Nav.LocationChanged += OnLocationChanged;
        UpdateBackButton();
        await base.OnInitializedAsync();
    }

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (firstRender)
        {
            Loading.IsLoading = false;
            StateHasChanged();
        }

        await base.OnAfterRenderAsync(firstRender);
    }

    private async Task OnRoleChanged(long roleId)
    {
        _selectedRoleId = roleId;
        var role = RoleNav.Roles.FirstOrDefault(r => r.Id == roleId);
        if (role is not null)
            await RoleNav.SelectRoleAsync(role);
    }

    private async Task OnRoleChangedFromSelect(ChangeEventArgs e)
    {
        if (long.TryParse(e.Value?.ToString(), out var roleId))
            await OnRoleChanged(roleId);
    }

    private void ToggleDrawer() => _drawerOpen = !_drawerOpen;

    private void NavigateAndCloseDrawer(string? url)
    {
        _drawerOpen = false;
        if (url is not null) Nav.NavigateTo(url);
    }

    private async Task GoBack() => await Js.InvokeVoidAsync("history.back");

    private bool IsActive(string? url)
    {
        if (url is null) return false;
        var path = "/" + Nav.ToBaseRelativePath(Nav.Uri).TrimEnd('/');
        return path.StartsWith(url, StringComparison.OrdinalIgnoreCase);
    }

    private void OnLocationChanged(object? sender, Microsoft.AspNetCore.Components.Routing.LocationChangedEventArgs e)
    {
        UpdateBackButton();
        StateHasChanged();
    }

    private void UpdateBackButton()
    {
        var relativePath = Nav.ToBaseRelativePath(Nav.Uri).TrimEnd('/');
        _showBackButton = !RootPaths.Contains($"/{relativePath}", StringComparer.OrdinalIgnoreCase)
                          && !RootPaths.Contains(relativePath, StringComparer.OrdinalIgnoreCase);
    }

    public void AlertChanged() => StateHasChanged();

    public void Dispose()
    {
        RoleNav.OnChanged -= StateHasChanged;
        Nav.LocationChanged -= OnLocationChanged;
    }
}
