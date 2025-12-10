using Microsoft.AspNetCore.Components.WebAssembly.Hosting;

namespace ScoreCast.Web.Layout;

public partial class MainLayout : IDisposable
{
    private bool _drawerOpen = false;
    private bool _isMobile = false;
    private long _selectedRoleId;

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

    public void AlertChanged() => StateHasChanged();

    public void Dispose() => RoleNav.OnChanged -= StateHasChanged;
}
