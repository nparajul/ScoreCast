using Microsoft.JSInterop;
using ScoreCast.Web.Components.Helpers;

namespace ScoreCast.Web.Components;

public abstract class ScoreCastComponentBase : ComponentBase
{
    [CascadingParameter(Name = "IsMobile")] public bool IsMobile { get; set; }
    [Inject] private PageStateService PageState { get; set; } = null!;
    [Inject] private IJSRuntime Js { get; set; } = null!;
    [Inject] private NavigationManager Nav { get; set; } = null!;

    protected virtual string PageKey => GetType().Name;

    protected void SaveState(string key, object value) => PageState.Set(PageKey, key, value);

    protected T? RestoreState<T>(string key, T? defaultValue = default) => PageState.Get(PageKey, key, defaultValue);

    protected async Task RestoreScrollAsync()
    {
        try
        {
            var path = "/" + Nav.ToBaseRelativePath(Nav.Uri).TrimEnd('/');
            await Task.Delay(50);
            await Js.InvokeVoidAsync("scrollState.restore", path);
        }
        catch { /* JS not loaded yet */ }
    }
}
