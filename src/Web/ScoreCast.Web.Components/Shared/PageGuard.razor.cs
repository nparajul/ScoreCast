using Microsoft.AspNetCore.Components;
using ScoreCast.Web.Components.Helpers;

namespace ScoreCast.Web.Components.Shared;

public partial class PageGuard : ComponentBase, IDisposable
{
    [Parameter] public required RenderFragment ChildContent { get; set; }

    private bool _authorized;
    private bool _checked;

    protected override void OnInitialized()
    {
        RoleNav.OnChanged += CheckAccess;
        CheckAccess();
    }

    private void CheckAccess()
    {
        var uri = new Uri(Navigation.Uri);
        var path = uri.AbsolutePath.TrimEnd('/');
        _authorized = RoleNav.Pages.Any(p =>
            string.Equals(p.PageUrl?.TrimEnd('/'), path, StringComparison.OrdinalIgnoreCase));
        _checked = true;
        InvokeAsync(StateHasChanged);
    }

    public void Dispose() => RoleNav.OnChanged -= CheckAccess;
}
