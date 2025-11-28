using Microsoft.AspNetCore.Components.WebAssembly.Hosting;

namespace ScoreCast.Web.Layout;

public partial class MainLayout
{
    private MudThemeProvider _themeProvider = default!;
    private bool _isDarkMode = false;

    private string? SelectedRole { get; set; }
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

    private void ToggleDarkMode() => _isDarkMode = !_isDarkMode;

    public void AlertChanged() => StateHasChanged();
}
