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
        if (RoleNav.SelectedRole is null) return;

        var uri = new Uri(Navigation.Uri);
        var path = uri.AbsolutePath.TrimEnd('/');
        _authorized = RoleNav.Pages.Any(p => MatchesRoute(p.PageUrl, path));
        _checked = true;
        InvokeAsync(StateHasChanged);
    }

    private static bool MatchesRoute(string? pattern, string path)
    {
        if (string.IsNullOrEmpty(pattern)) return false;

        var patternSegments = pattern.TrimEnd('/').Split('/');
        var pathSegments = path.Split('/');

        if (patternSegments.Length != pathSegments.Length) return false;

        for (var i = 0; i < patternSegments.Length; i++)
        {
            if (patternSegments[i].StartsWith('{')) continue;
            if (!string.Equals(patternSegments[i], pathSegments[i], StringComparison.OrdinalIgnoreCase)) return false;
        }

        return true;
    }

    public void Dispose() => RoleNav.OnChanged -= CheckAccess;
}
