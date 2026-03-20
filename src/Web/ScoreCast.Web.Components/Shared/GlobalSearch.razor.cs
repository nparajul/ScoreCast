using ScoreCast.Models.V1.Responses.Football;

namespace ScoreCast.Web.Components.Shared;

public partial class GlobalSearch
{
    [Inject] private IScoreCastApiClient Api { get; set; } = null!;
    [Inject] private NavigationManager Nav { get; set; } = null!;

    [Parameter] public bool Visible { get; set; }
    [Parameter] public EventCallback OnClose { get; set; }

    private string? _query;
    private List<SearchItem> _allItems = [];
    private List<SearchItem> _filtered = [];
    private bool _dataLoaded;
    private bool _loading;

    private async Task OnSearchChanged(string? value)
    {
        _query = value;

        if (string.IsNullOrWhiteSpace(value))
        {
            _filtered = [];
            return;
        }

        if (!_dataLoaded)
        {
            _loading = true;
            StateHasChanged();
            await LoadDataAsync();
            _loading = false;
        }

        _filtered = _allItems
            .Where(i => i.Name.Contains(value, StringComparison.OrdinalIgnoreCase))
            .OrderBy(i => i.Category)
            .ThenBy(i => i.Name)
            .Take(15)
            .ToList();
    }

    private async Task LoadDataAsync()
    {
        if (_dataLoaded) return;

        var comps = await Api.GetCompetitionsAsync(CancellationToken.None);
        if (comps is { Success: true, Data: not null })
        {
            foreach (var c in comps.Data)
                _allItems.Add(new("🏆", c.Name, c.LogoUrl, "Competition", $"/scores"));

            foreach (var comp in comps.Data)
            {
                var teams = await Api.GetTeamsAsync(comp.Name, CancellationToken.None);
                if (teams is { Success: true, Data: not null })
                {
                    foreach (var t in teams.Data)
                    {
                        if (_allItems.All(i => i.Url != $"/teams/{t.Id}"))
                            _allItems.Add(new("🛡️", t.Name, t.LogoUrl, "Team", $"/teams/{t.Id}"));
                    }
                }
            }
        }

        _dataLoaded = true;
    }

    private async Task Navigate(SearchItem item)
    {
        _query = null;
        _filtered = [];
        await OnClose.InvokeAsync();
        Nav.NavigateTo(item.Url);
    }

    private async Task Close()
    {
        _query = null;
        _filtered = [];
        await OnClose.InvokeAsync();
    }

    private record SearchItem(string Emoji, string Name, string? ImageUrl, string Category, string Url);
}
