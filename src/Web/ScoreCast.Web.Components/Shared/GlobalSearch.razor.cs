using ScoreCast.Models.V1.Responses.Football;

namespace ScoreCast.Web.Components.Shared;

public partial class GlobalSearch
{
    [CascadingParameter] public IMudDialogInstance Dialog { get; set; } = null!;
    [Inject] private IScoreCastApiClient Api { get; set; } = null!;
    [Inject] private NavigationManager Nav { get; set; } = null!;

    private string? _query;
    private List<SearchItem> _allItems = [];
    private List<SearchItem> _filtered = [];
    private bool _loaded;

    protected override async Task OnInitializedAsync()
    {
        var compsTask = Api.GetCompetitionsAsync(CancellationToken.None);
        var teamsTask = LoadAllTeamsAsync();
        await Task.WhenAll(compsTask, teamsTask);

        var comps = compsTask.Result;
        if (comps is { Success: true, Data: not null })
        {
            foreach (var c in comps.Data)
                _allItems.Add(new("🏆", c.Name, c.LogoUrl, "Competition", $"/scores?comp={c.Code}"));
        }

        _allItems.AddRange(teamsTask.Result);
        _loaded = true;
    }

    private async Task<List<SearchItem>> LoadAllTeamsAsync()
    {
        var items = new List<SearchItem>();
        var comps = await Api.GetCompetitionsAsync(CancellationToken.None);
        if (comps is not { Success: true, Data: not null }) return items;

        foreach (var comp in comps.Data)
        {
            var teams = await Api.GetTeamsAsync(comp.Name, CancellationToken.None);
            if (teams is { Success: true, Data: not null })
            {
                foreach (var t in teams.Data)
                {
                    if (items.All(i => i.Url != $"/teams/{t.Id}"))
                        items.Add(new("🛡️", t.Name, t.LogoUrl, "Team", $"/teams/{t.Id}"));
                }
            }
        }

        return items;
    }

    private void OnSearchChanged(string? value)
    {
        _query = value;
        _filtered = string.IsNullOrWhiteSpace(value)
            ? []
            : _allItems
                .Where(i => i.Name.Contains(value, StringComparison.OrdinalIgnoreCase))
                .OrderBy(i => i.Category)
                .ThenBy(i => i.Name)
                .Take(20)
                .ToList();
    }

    private void Navigate(SearchItem item)
    {
        Dialog.Close();
        Nav.NavigateTo(item.Url);
    }

    private record SearchItem(string Emoji, string Name, string? ImageUrl, string Category, string Url);
}
