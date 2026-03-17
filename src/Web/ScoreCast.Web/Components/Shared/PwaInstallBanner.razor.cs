using Microsoft.JSInterop;

namespace ScoreCast.Web.Components.Shared;

public partial class PwaInstallBanner : IAsyncDisposable
{
    [Inject] private IJSRuntime Js { get; set; } = null!;

    private bool _showBanner;
    private bool _canInstall;
    private bool _isIos;
    private bool _isStandalone;
    private DotNetObjectReference<PwaInstallBanner>? _ref;

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (!firstRender) return;

        _isStandalone = await Js.InvokeAsync<bool>("pwaInstall.isStandalone");
        if (_isStandalone) return;

        var dismissed = await Js.InvokeAsync<string?>("localStorage.getItem", "pwa-install-dismissed");
        if (dismissed is not null && DateTimeOffset.TryParse(dismissed, out var d) && DateTimeOffset.UtcNow - d < TimeSpan.FromDays(14))
            return;

        _isIos = await Js.InvokeAsync<bool>("pwaInstall.isIos");
        _ref = DotNetObjectReference.Create(this);
        await Js.InvokeVoidAsync("pwaInstall.init", _ref);

        // iOS: show instructions immediately. Others: wait for beforeinstallprompt callback.
        if (_isIos) { _showBanner = true; StateHasChanged(); }
    }

    [JSInvokable]
    public void OnInstallAvailable()
    {
        _canInstall = true;
        _showBanner = true;
        InvokeAsync(StateHasChanged);
    }

    [JSInvokable]
    public void OnInstalled()
    {
        _showBanner = false;
        InvokeAsync(StateHasChanged);
    }

    private async Task Install() => await Js.InvokeAsync<bool>("pwaInstall.prompt");

    private async Task Dismiss()
    {
        _showBanner = false;
        await Js.InvokeVoidAsync("localStorage.setItem", "pwa-install-dismissed", DateTimeOffset.UtcNow.ToString("o"));
    }

    public ValueTask DisposeAsync()
    {
        _ref?.Dispose();
        return ValueTask.CompletedTask;
    }
}
